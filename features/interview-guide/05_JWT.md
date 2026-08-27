# 05 — JWT Authentication

> Verified against the actual LearnPath backend and frontend source code.

---

## 1. What Is It?

JWT (JSON Web Token) is a **signed token** the server gives to a client after login. The client
sends it with every API request as proof of identity. The token is a long string with **three
parts separated by dots**: `header.payload.signature`.

- **Header** — contains the algorithm (HMAC-SHA256 here).
- **Payload** — contains claims (data about the user: id, email, roles…).
- **Signature** — created by signing the header+payload with a secret key. Anyone can *read* the
  payload, but only the server that knows the secret can *sign* it, so the token can't be forged.

Simple answer: *"A JWT is a signed, self-contained token that proves who the user is on every request."*

---

## 2. Why Is It Used?

LearnPath has a **stateless API**. Instead of storing session data on the server, the API
issues a JWT after login. Every protected controller `[Authorize]` checks that token. Benefits:

- No server-side session storage to scale.
- Works naturally for a React SPA that talks to a separate API.
- Roles travel inside the token, so authorization decisions are fast.

---

## 3. How Is It Used in LearnPath?

### Backend files

| File | Role |
|---|---|
| `backend/Authentication/Jwt/JwtSettings.cs` | Settings: Secret, Issuer, Audience, ExpiryMinutes, RefreshTokenExpiryDays |
| `backend/Authentication/Jwt/JwtTokenGenerator.cs` | `GenerateAccessToken(user, roles)` + `GenerateRefreshToken()` |
| `backend/Services/Auth/AuthService.cs` | Login/register/refresh/revoke; issues and rotates tokens |
| `backend/Program.cs` | Registers JWT bearer auth + validation rules + role policies |
| `backend/Entities/RefreshToken.cs` | The persisted long-lived refresh token |

### Token generation — `JwtTokenGenerator.GenerateAccessToken`

```csharp
claims = [
  Sub       = user.Id,          // "sub"
  Email     = user.Email,
  Jti       = Guid.NewGuid(),   // unique token id
  firstName, lastName,
  Role claims for each role     // ClaimTypes.Role
]

signature key  = SymmetricSecurityKey(secret)   // from JwtSettings.Secret
signing        = HmacSha256
token = new JwtSecurityToken(
    issuer:   JwtSettings.Issuer        ("LearnPathAPI"),
    audience: JwtSettings.Audience      ("LearnPathClient"),
    claims:   claims,
    expires:  now + ExpiryMinutes       (60 minutes),
    signingCredentials: signing)

return JwtSecurityTokenHandler.WriteToken(token)
```

The refresh token is **not a JWT** — it's 64 random bytes, base64-encoded,
created with `RandomNumberGenerator.Create()`, and stored in the database.

### Settings (from `appsettings.json` / `.env`)

- Secret comes from the environment (`LEARNPATH_JWT_SECRET`) — never committed.
- `ExpiryMinutes = 60` for access tokens.
- `RefreshTokenExpiryDays = 7`.

### Bearer validation — `Program.cs`

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)…  
  with:
    ValidateIssuer = true,  ValidateAudience = true,
    ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = "LearnPathAPI", ValidAudience = "LearnPathClient",
    IssuerSigningKey = SymmetricSecurityKey(secret),
    ClockSkew = TimeSpan.Zero
```

Then two role policies:

```csharp
"AdminOnly"          → RequireRole("Admin")
"InstructorOrAdmin"  → RequireRole("Admin", "Instructor")
```

And the pipeline runs `app.UseAuthentication()` → `app.UseAuthorization()` → `MapControllers()`.

---

## 4. How It Works Internally

Full login → request flow:

```
LoginPage (React)
  → POST api/v1/auth/login { email, password }
  → AuthService.LoginAsync
      → FindByEmailAsync + CheckPasswordAsync (Identity hashing) + status checks
      → roles = GetRolesAsync(user)
      → accessToken  = JwtTokenGenerator.GenerateAccessToken(user, roles)
      → refreshToken = GenerateRefreshToken()
      → save RefreshToken row (UserId, Token, ExpiresAt = now+7d, IsRevoked=false)
      → returns AuthResponseDto { accessToken, refreshToken, userId, email, firstName, lastName, roles }

Frontend stores both tokens (localStorage).

Next request:
  apiClient request interceptor: headers.Authorization = `Bearer ${accessToken}`
  → every protected endpoint [Authorize] validates:
      signature (secret) → issuer → audience → expiry → (roles via policy)
  → token payload is turned into User claims (NameIdentifier=sub, Email, roles)
  → controller reads user id via User.FindFirstValue(ClaimTypes.NameIdentifier)
```

Token expiration → refresh (see frontend section).

---

## 5. Important Internal Logic

- **Symmetric signing:** one secret signs and verifies; the server never stores tokens.
- **Roles in claims:** roles are added as `ClaimTypes.Role`, which is exactly what
  `[Authorize(Roles="Admin")]` and `RequireRole` look at. Controllers don't re-query roles — they rely on claims.
- **Clock skew zero:** token must not be expired; a 1-second skew stops edge cases.
- **User id resolution:** `Sub` claim → `ClaimTypes.NameIdentifier` → used in every controller
  (`UserId` property) and by `AuditLogService.ResolveActor` for logging "who did it".
- **Refresh token rotation:** `AuthService.RefreshTokenAsync` marks the old refresh token
  `IsRevoked = true` before issuing a new pair. Logout (`RevokeTokenAsync`) revokes **all** active refresh tokens.
- **Refresh token is stored hashed?** No — it's stored as the raw base64 string in the
  `RefreshTokens` table (that's the current implementation; worth mentioning if asked about
  improvement).

---

## 6. Frontend Side

Files:

- `frontend/src/services/apiClient.ts` — axios instance (baseURL `VITE_API_BASE_URL`, `withCredentials: true`).
- `frontend/src/utils/tokenUtils.ts` — localStorage helpers + `isTokenExpired`.
- `frontend/src/redux/slices/authSlice.ts` — stores user + roles after login/register.
- `frontend/src/services/authService.ts` — login/register/refresh/logout calls.

Flow:

```
Login → authSlice.loginThunk → tokenUtils.setTokens(access, refresh)
  → localStorage: "lp_access_token" + "lp_refresh_token"
  → authSlice state: user { userId, email, names, roles }, isAuthenticated = true

Every request:
  apiClient request interceptor:
    if token → config.headers.Authorization = `Bearer <accessToken>`

Every 401 (not on login/register paths):
  apiClient response interceptor:
    if !original._retry:
        original._retry = true
        POST /auth/refresh { accessToken, refreshToken }   (a fresh axios call, NOT apiClient → avoids loop)
        tokenUtils.setTokens(new access, new refresh)
        retry the original request with the new token
    else → tokenUtils.clearTokens() → redirect to /login
```

- The refresh call uses a plain `axios.post` on purpose — using `apiClient` there would loop
  forever (401 → refresh → 401…).
- On logout: `authService.logout()` → `POST /auth/revoke` (backend revokes all refresh tokens) → `clearTokens()` → redirect.

---

## 7. Backend Side

- `AuthController` — `register`, `login`, `refresh` are `[AllowAnonymous]`; `revoke` is `[Authorize]` (user id taken from claims).
- `AuthService` + `JwtTokenGenerator` handle creation/rotation.
- `Program.cs` sets up the JWT bearer scheme and the `AdminOnly` / `InstructorOrAdmin` policies.
- Controllers use `[Authorize(Roles = "…")]` thorough policy checks (e.g. quiz CRUD = `Admin,Instructor`, delete = `Admin`).

---

## 8. Database Side

- Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`) hold users/roles.
- `RefreshTokens` table: `UserId`, `Token`, `ExpiresAt`, `IsRevoked`, `CreatedAt`. This is the only token data persisted — access tokens themselves are not stored.

---

## 9. Security / Authorization

- Access token: short-lived (60 min), stateless, validated on every request.
- Refresh token: long-lived (7 days), stored in DB, rotated on each use, revoked on logout.
- Account status (Inactive/Invalid/Deleted) is checked at login **and** again on refresh.
- Role-based access via claims + `[Authorize(Roles=…)]` + policies.
- Service-level ownership checks still run (e.g. "you do not own this quiz"), independent of authentication.

---

## 10. Simple Real Example

A student logs in. Backend validates the password, loads roles (`Student`), and returns an
access token (expires in 60 min) plus a refresh token stored in the DB. The frontend stores both.
When the student opens the quiz page, `quizService` sends `Authorization: Bearer <token>`;
`[Authorize]` validates it, `FindFirstValue(NameIdentifier)` gives the user id, and the quizzes
list is filtered to that user. After 60 minutes the token expires; the next call returns 401, the
interceptor silently refreshes with the stored refresh token and retries — the user never notices.

---

## 11. Interview Answer

> "In LearnPath I implemented JWT authentication. After a successful login, `JwtTokenGenerator`
> creates an access token signed with HMAC-SHA256 using a secret from the environment, and it
> includes claims like the user id, email, and roles. Access tokens expire in 60 minutes, and I
> also issue a refresh token — a random 64-byte value stored in the database — so users aren't
> logged out abruptly. On the React side, an axios request interceptor adds `Bearer` to every
> request, and a response interceptor automatically calls the refresh endpoint when it sees a 401
> and retries the original request. The backend validates issuer, audience, lifetime, and
> signature, and role claims feed into `[Authorize(Roles=…)]` and policies like
> `InstructorOrAdmin`."

(≈ 40–50 seconds)

---

## 12. Follow-Up Questions

**Q: Why JWT and not cookie sessions?**
The API is stateless and the frontend is a separate SPA. JWT avoids server-side session storage and scales horizontally without shared session stores. Cookies would make CORS/CSRF handling more complex here.

**Q: What's in the token?**
Header (alg), payload with claims: sub (user id), email, jti, firstName, lastName, and role claims. No password or secret data.

**Q: Why is the token sent as `Authorization: Bearer …`?**
Standard convention for bearer tokens; the `[Authorize]` middleware reads it from that header by default.

**Q: How do roles get into the token?**
`JwtTokenGenerator.GenerateAccessToken` adds `ClaimTypes.Role` for each role from `GetRolesAsync`. `[Authorize(Roles=…)]` and the `RequireRole` policies check exactly those claims.

**Q: What happens when the access token expires?**
The API returns 401; the axios response interceptor calls `POST /auth/refresh` with the stored refresh token, gets a new pair, and retries the original request automatically. If refresh fails, tokens are cleared and the user is sent to `/login`.

**Q: Is the refresh token a JWT?**
No. It's 64 random bytes encoded in base64, generated with `RandomNumberGenerator`. It's stored in the `RefreshTokens` table so it can be revoked.

**Q: What's token rotation?**
On every refresh, the old refresh token is marked `IsRevoked` and a new one is issued, so a stolen token can't be reused indefinitely. Logout revokes all of a user's active refresh tokens.

**Q: Any limitations?**
Tokens are stored in localStorage, which is vulnerable to XSS. A more secure pattern is HttpOnly cookies. Also, once an access token is issued you can't revoke it before expiry without a blacklist — short expiry + rotation is our mitigation.

**Q: How would you improve it?**
Use HttpOnly/Secure cookies for refresh tokens, add refresh-token hashing at rest, add refresh throttling/reuse detection, and shorten access-token lifetime further.

---

## 13. Functionality

Confirmed from source code:

- Access token generation with sub/email/jti/name/role claims, HS256 signing (`JwtTokenGenerator`).
- 60-minute access token expiry, 7-day refresh token expiry (`JwtSettings`).
- Refresh token as 64 random bytes, persisted with `IsRevoked` and `ExpiresAt` (`RefreshToken` entity, `AuthService.BuildAuthResponseAsync`).
- Login with account status checks (Deleted/Inactive/Invalid) and `INVALID CREDENTIALS` + audit `LOGIN_FAILED`.
- Refresh with rotation (old token revoked) and re-check of account health.
- Logout via `REVOKE` revoking all active refresh tokens + `LOGOUT` audit entry.
- Full JWT bearer validation: issuer, audience, lifetime, signing key, zero clock skew (`Program.cs`).
- Role policies `AdminOnly` and `InstructorOrAdmin`; role-based `[Authorize(Roles=…)]` across controllers.
- Frontend: localStorage token storage, request interceptor (Bearer), 401→refresh→retry response interceptor (`apiClient.ts`).