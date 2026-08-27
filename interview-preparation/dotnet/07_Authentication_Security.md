# Phase 7 — Authentication + Security (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

All LearnPath references verified from source: `backend/Program.cs`,
`backend/Authentication/Jwt/*`, `backend/Services/Auth/AuthService.cs`, controllers,
and `backend/Entities/RefreshToken.cs`.

---

## 1. Authentication 🔥

### What is it?
Proving WHO you are — the process of verifying identity using credentials
(username/password) or a token. Result: "I know who you are."

### Why is it used?
The entrance gate: only verified users may use the API.

### Interview Answer
"Authentication answers 'who are you?' — verifying identity. In LearnPath, a user
logs in with email + password through `AuthService.LoginAsync`: I look up the user,
check the hashed password with `UserManager.CheckPasswordAsync`, verify account
status, and if valid, issue a JWT access token. The client sends that token on later
requests and the middleware validates it."

### LearnPath Example
`backend/Services/Auth/AuthService.cs` — `LoginAsync`, `RegisterAsync`. JWT issued
after successful verification.

### Common Mistake
- Mixing it with authorization — auth (who) comes first, authz (what) second.

### Remember
- AuthN = identity check. Login = authentication. Token = proof.

---

## 2. Authorization 🔥

### What is it?
Deciding WHAT an authenticated user is allowed to do — their permissions via roles
or policies.

### Why is it used?
Identity alone isn't enough — a student must not access admin endpoints.

### Interview Answer
"Authorization answers 'what may you do?'. Once the user is authenticated, I check
roles and policies. `[Authorize]` allows any logged-in user, `[Authorize(Roles = "Admin")]`
restricts to admins. In my code, controllers like `AdminController` are locked to the
Admin role, while `LearningPathController` uses `Authorize(Roles = "Admin,Instructor")`
for create/update actions."

### LearnPath Example
`AdminController` — `[Authorize(Roles = "Admin")]`; `LearningPathController` —
`[Authorize(Roles = "Admin,Instructor")]`; policies `AdminOnly`/`InstructorOrAdmin`
in `Program.cs`.

### Remember
- AuthZ = permission. Roles/policies after identity.

---

## 3. JWT 🔥

### What is it?
JSON Web Token — a compact, signed, self-contained token format (`header.payload.signature`)
used to carry claims (user id, email, roles) between client and server.

### Why is it used?
Stateless authentication: the server doesn't store sessions; it just verifies the
token's signature on each request.

### Interview Answer
"JWT is a token standard containing claims about the user, signed so it can't be
tampered with. The server verifies the signature with its secret on every request —
no server-side session storage needed. In LearnPath, `JwtTokenGenerator` builds the
token with the user's id, email, name, and roles, and signs it with HMACSHA256 using
the configured secret."

### LearnPath Example
`backend/Authentication/Jwt/JwtTokenGenerator.cs` — `GenerateAccessToken`, called by
`AuthService.BuildAuthResponseAsync`.

### How it works
```text
Login → server creates token (claims + expiry, signed) → client stores it →
client sends `Authorization: Bearer <token>` → server validates signature + expiry →
trusts the claims
```

### Common Mistake
- Thinking a JWT encrypts data — it signs (base64 readable). Don't put secrets in it.

### Remember
- JWT = signed, stateless token. Signature is the security, not encryption.

---

## 4. JWT structure 🔥

### What is it?
Three base64url parts separated by dots:
1. **Header** — algorithm + type (`{"alg":"HS256","typ":"JWT"}`)
2. **Payload** — claims (sub, email, roles, exp)
3. **Signature** — hash of `header.payload` signed with the secret

### Why is it used?
The payload carries identity data; the signature proves integrity + authenticity.

### Interview Answer
"A JWT has three dot-separated parts. The header says which algorithm was used —
mine uses HS256. The payload holds the claims — user id, email, roles, expiry. The
signature signs the first two parts with the secret, so if anyone changes the payload,
the signature no longer matches and the server rejects it."

### Example
```text
eyJhbGciOiJIUzI1NiJ9 . eyJzdWIiOiJ1c2VyLTEyMyIsInJvbGVzIjpbIlN0dWRlbnQiXX0 . signature
      header                payload (readable!)              signature
```

### LearnPath Example
`JwtTokenGenerator` — claims list (Sub, Email, Jti, firstName, lastName, roles),
HS256 via `SecurityAlgorithms.HmacSha256`, `WriteToken` produces the three parts.

### Common Mistake
- Decoding/reuse of payload values without checking the signature — the payload is
public; trust it ONLY if the signature validates.

### Remember
- header.payload.signature → algorithm, claims, signature. Base64 is readable.

---

## 5. Access token 🔥

### What is it?
A short-lived JWT used on every request to prove identity. Lives in the
`Authorization: Bearer <token>` header.

### Why is it used?
Stateless auth: frequent, safely short-lived; if stolen, it expires quickly.

### Interview Answer
"The access token is the JWT the client sends with every request in the
Authorization header. It's short-lived — in my config, 60 minutes — so if it leaks,
the damage window is small. It carries the user's id, email, and roles, so the API
knows who is calling without hitting the database each time."

### LearnPath Example
`JwtTokenGenerator.GenerateAccessToken` — `expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes)`
(60 in appsettings.json). Sent in `AuthResponseDto.AccessToken`.

### Common Mistake
- Storing access tokens in `localStorage` — XSS risk; safer in httpOnly cookie or
memory for a fresher project demo.

### Remember
- Access token = short-lived JWT, sent in header, validates every request.

---

## 6. Refresh token 🔥

### What is it?
A long-lived token used ONLY to get a new access token when the old one expires. It
is stored server-side, revocable, and rotated.

### Why is it used?
Short access tokens shouldn't log the user out every hour — the refresh token gets a
new one silently, and can be revoked on logout/compromise.

### Interview Answer
"A refresh token is long-lived and used only to mint new access tokens. In my design,
it's stored in the database (`RefreshToken` entity) so the server can validate and
revoke it. When the access token expires, the client calls `auth/refresh` with the
refresh token; `AuthService.RefreshTokenAsync` checks it's valid and unrevoked, marks
the old one revoked (rotation), and issues a fresh pair. Logout revokes all of them."

### LearnPath Example
`AuthService.RefreshTokenAsync` — validates stored token + expiry, sets IsRevoked,
returns new tokens. `RevokeTokenAsync` revokes all active tokens. Refresh token
expiry: 7 days (entity), 64 random bytes (`RandomNumberGenerator`).

### Common Mistake
- Putting the refresh token in the same short-lived JWT — it must be special,
stored, revocable. And always rotate (revoke old) on use.

### Remember
- Refresh token = long, stored, revocable, rotated. Gets access tokens, not data.

---

## 7. Claims 🔥

### What is it?
Name-value facts inside the token about the user: `sub` (user id), `email`, `jti`
(unique token id), `firstName`, roles.

### Why is it used?
The server learns identity/permissions from the token without querying the DB on
each request.

### Interview Answer
"Claims are the key-value facts stored inside a JWT — the user's id, email, first and
last name, roles, and a unique jti. The API reads them from the token. For example,
controllers call `User.FindFirstValue(ClaimTypes.NameIdentifier)` to get the current
user's id out of the token."

### LearnPath Example
`JwtTokenGenerator` builds claims: `JwtRegisteredClaimNames.Sub/Email/Jti`, custom
`firstName`/`lastName`, plus `ClaimTypes.Role` per role. Controllers read
`ClaimTypes.NameIdentifier` (see `UserController`, `AuthController.Revoke`).

### Common Mistake
- Trusting claims blindly — always trust only after signature validation (the
middleware guarantees that).

### Remember
- Claims = data in the token. Server reads who/permissions from them.

---

## 8. Roles 🔥

### What is it?
Named permission groups (Admin, Instructor, Student) attached to users; roles become
claims in the token and drive authorization.

### Why is it used?
Group-based access control: check one role instead of many individual permissions.

### Interview Answer
"Roles are named groups like Admin, Instructor, and Student. On registration I add
the Student role automatically. On login I read the user's roles with
`UserManager.GetRolesAsync` and put them into the token as role claims. Then
`[Authorize(Roles = "Admin")]` checks them. Roles are seeded with `RoleSeeder` and
managed through Identity's role tables."

### LearnPath Example
`AuthService.RegisterAsync` — `AddToRoleAsync(user, "Student")`. Token adds
ClaimTypes.Role per role (`JwtTokenGenerator:31`). Policies `AdminOnly`,
`InstructorOrAdmin` in Program.cs.

### Common Mistake
- Storing the user's roles only in the token without refreshing after a role change
— a role change takes effect after the token expires (short expiry helps).

### Remember
- Roles = role claims in the JWT → `[Authorize(Roles=...)]`.

---

## 9. Password hashing 🔥

### What is it?
Storing passwords as irreversible hashes instead of plain text. ASP.NET Core
Identity's `PasswordHasher` (PBKDF2) does it automatically.

### Why is it used?
If the DB leaks, hashed passwords can't be read as plain text; brute-force is slow.

### Interview Answer
"Passwords are never stored as plain text — only a hash. ASP.NET Core Identity does
this for me: `UserManager.CreateAsync(user, password)` hashes the password before
saving, and `CheckPasswordAsync` re-hashes and compares on login. The hash is
verified computationally, but the original password can't be recovered from it."

### Simple Example
```csharp
var result = await _userManager.CreateAsync(user, dto.Password);  // hashes internally
var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
```

### LearnPath Example
`AuthService.RegisterAsync`/`LoginAsync` use Identity's `UserManager` — no manual
hashing. Password policy in Program.cs: min 8 chars, upper + digit, lockout after 5
failed attempts for 5 minutes.

### Common Mistake
- Storing raw passwords or simple MD5 — use a proper hasher (PBKDF2/Identity).

### Remember
- Hash, never store plaintext. Identity's UserManager does it for you.

---

## 10. Token expiration 🔥

### What is it?
JWTs carry an `exp` claim; the middleware rejects expired tokens. Access tokens are
short-lived; refresh tokens longer.

### Why is it used?
Limits damage if a token leaks and forces periodic credential revalidation.

### Interview Answer
"Token expiry is controlled by the `exp` claim. My access tokens expire after 60
minutes and the middleware validates lifetime (`ValidateLifetime = true`) plus a
`ClockSkew` of zero, so expired tokens are rejected immediately. When it expires, the
client uses the refresh token (7 days) to get a new access token. Short expiry balances
security with UX."

### LearnPath Example
`JwtTokenGenerator` sets `expires = +ExpiryMinutes` (60). `Program.cs` —
`ValidateLifetime = true`, `ClockSkew = TimeSpan.Zero`. Refresh token `ExpiresAt =
UtcNow.AddDays(7)` validated in `RefreshTokenAsync`.

### Common Mistake
- Very long-lived access tokens with no refresh mechanism — increases breach risk.

### Remember
- exp claim + Lifetime validation + refresh flow = solid expiration handling.

---

## 11. JWT middleware 🔥

### What is it?
The ASP.NET Core middleware that validates the `Authorization: Bearer` token on every
request and populates `User` (ClaimsPrincipal).

### Why is it used?
One place validates tokens for every endpoint — controllers simply read claims.

### Interview Answer
"The JWT middleware (JwtBearer) validates the token on each request. Configured with
`AddJwtBearer`, it checks issuer, audience, lifetime, and signature, then builds the
`User` object from the token's claims. It's registered in the pipeline as
`UseAuthentication()` before `UseAuthorization()`. If the token is invalid, the
request ends with 401 before reaching a controller."

### LearnPath Example
`Program.cs:131-150` — `AddAuthentication().AddJwtBearer(...)` with
`TokenValidationParameters`; pipeline: `UseAuthentication()` → `UseAuthorization()`.

### Common Mistake
- Wrong pipeline order — Authorization without Authentication fails.

### Remember
- JwtBearer middleware = validate signature + exp, populate User. Order matters.

---

## 12. [Authorize] 🔥

### What is it?
The attribute that demands an authenticated user. Applied to a controller or action.
`[AllowAnonymous]` makes a endpoint public.

### Why is it used?
Simple, declarative endpoint protection.

### Interview Answer
"`[Authorize]` blocks the endpoint from anonymous users — a valid token is required,
otherwise the middleware returns 401. `[AllowAnonymous]` opens an endpoint, which I
use for register, login, and refresh, since you obviously need no token to log in. I
put `[Authorize]` at the controller level so every action is protected by default."

### LearnPath Example
`AuthController` — `[AllowAnonymous]` on register/login/refresh, `[Authorize]` on
revoke. `UserController`/`ProgressController` — `[Authorize]` at class level.

### Common Mistake
- Forgetting `[AllowAnonymous]` on login — a chicken-and-egg failure.

### Remember
- [Authorize] = gate; [AllowAnonymous] = public. Controller-level default.

---

## 13. Role-based authorization 🔥

### What is it?
Restricting endpoints to specific roles using `[Authorize(Roles = "Admin")]` or
policy-based checks (`RequireRole`).

### Why is it used?
Fine-grained access: students, instructors, admins get different powers.

### Interview Answer
"For role-based access I use `[Authorize(Roles = "Admin")]` on the admin controller
and `[Authorize(Roles = "Admin,Instructor")]` where instructors may act. I also
defined policies in Program.cs — `AdminOnly` and `InstructorOrAdmin` — that wrap
role requirements for reuse. Both feed off the role claims in the JWT."

### LearnPath Example
`AdminController` class-level `[Authorize(Roles = "Admin")]`; `LearningPathController`
create/update actions `[Authorize(Roles = "Admin,Instructor")]`; policies in
`Program.cs:153-155`.

### Common Mistake
- Only checking auth, not authz — a logged-in student must not reach admin work.

### Remember
- Roles in token + [Authorize(Roles=...)] or policies = role authorization.

---

## 14. CORS security basics 🟡

### What is it?
CORS controls which browser origins may call the API. It's a browser security
mechanism, not auth — tokens still enforce identity.

### Why is it used?
Prevents random websites from calling the API with a logged-in user's browser.

### Interview Answer
"CORS is a browser defense limiting which origins can call the API. I whitelist
exact origins in config — localhost:5173 for development, the real frontend domain in
production. I avoid `AllowAnyOrigin` in production, and I use `AllowCredentials`
only with specific origins. Crucially, CORS is not authentication — the JWT still
protects endpoints."

### LearnPath Example
`Program.cs` — `LearnPathCors` policy with `WithOrigins(config["AllowedOrigins"])`,
allow any headers/methods, `AllowCredentials()`.

### Common Mistake
- `AllowAnyOrigin()` + `AllowCredentials()` — an insecure combination browsers reject
anyway.

### Remember
- CORS = origin gate (browser), not identity. Whitelist real domains.

---

## 15. Common authentication mistakes 🔥

### What is it?
The frequently-tested pitfalls:
1. Storing plaintext passwords — always hash (Identity does it).
2. Putting the JWT secret in source control — env vars only.
3. Not validating issuer/audience/lifetime — only signature.
4. Huge clock skew or long expiry — token remains valid too long.
5. No refresh-token rotation/revocation — stolen tokens stay valid.
6. Returning "wrong password" vs "unknown email" — lets attackers enumerate users
   (the project returns generic "Invalid credentials").
7. Trusting claims without signature validation.
8. Missing [Authorize] on new endpoints.

### Why is it used?
Interviewers test if you understand security beyond just "using JWT".

### Interview Answer
"The main ones: storing plaintext passwords; hard-coding the JWT secret in code; not
validating issuer, audience, or lifetime; keeping tokens valid too long; and not
rotating refresh tokens. Also user enumeration — my login returns one generic
'Invalid credentials' message whether the email exists or not. And every protected
endpoint must actually be decorated with `[Authorize]`."

### LearnPath Example
The project does well: secret/env via `.env` + `AddInMemoryCollection`, full
`TokenValidationParameters` set, short 60-min expiry, refresh rotation + revoke,
status checks on login, generic error messages, append-only audit logs.

### Follow-Up Questions
Q: What if the JWT secret leaks?
A: All tokens become forgeable — rotate the secret immediately, all clients must
re-login.

### Remember
- Secret in env, full validation, short expiry, rotate refresh, no plaintext, no
user enumeration.

---

## Final Phase 7 "remember by heart" checklist

1. AuthN = who (login → JWT). AuthZ = what ([Authorize] + roles).
2. JWT = header.payload.signature; signed (HS256), not encrypted; carries claims +
   exp.
3. Access token (60 min) in Authorization header; refresh token (7 days) stored +
   rotated + revocable.
4. Passwords hashed by Identity; never stored/returned in plaintext.
5. JwtBearer middleware validates signature/issuer/audience/lifetime → User object.
6. Roles are claims → [Authorize(Roles=...)] / policies. CORS is not auth.

Next: `08_LearnPath_Architecture.md` — say "Proceed to next phase" when ready.