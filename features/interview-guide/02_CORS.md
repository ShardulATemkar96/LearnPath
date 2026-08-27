# 02 — CORS (Cross-Origin Resource Sharing)

> Verified against the actual LearnPath backend source code.

---

## 1. What Is It?

CORS is a **browser security rule** that controls whether a web page loaded from one origin
(like `http://localhost:5173`) is allowed to make requests to a *different* origin
(like `http://localhost:5000`).

Simple definition: *"CORS is a browser mechanism that tells the server which other websites are allowed to call its APIs."*

An **origin** = protocol + domain + port. So `http://localhost:5173` and `http://localhost:5000`
are two different origins even though the port is the only difference.

---

## 2. Why Is It Used?

The LearnPath architecture is **two separate apps**:

- Frontend: React + Vite, served on `http://localhost:5173`.
- Backend: ASP.NET Core API, served on `http://localhost:5000`.

They are different origins. By default, browsers **block** the frontend JavaScript from
sending requests to the backend, because the frontend page origin (5173) doesn't match the
API origin (5000).

Without CORS configuration on the backend:

- API calls would be blocked by the browser.
- Every screen that loads data (quizzes, classrooms, history…) would fail in the browser.

The fix: the backend explicitly says *"I trust this frontend origin, so allow it."*

---

## 3. How Is It Used in LearnPath?

The configuration lives in **`backend/Program.cs`**:

```csharp
// Program.cs (lines 158–169)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LearnPathCors", policy =>
    {
        policy
            .WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(","))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

And middleware is registered in the pipeline (line 300):

```csharp
app.UseCors("LearnPathCors");   // placed BEFORE UseAuthentication / UseAuthorization
```

The allowed origins come from configuration — **`backend/appsettings.json`**:

```json
"AllowedOrigins": "http://localhost:5173"
```

So currently exactly **one origin** is allowed: `http://localhost:5173` (the Vite dev server).
The value is comma-splittable, so more origins can be added in production.

---

## 4. How It Works Internally

Flow of a real request from the React app to the API:

```
React page on http://localhost:5173
  → browser looks at target origin http://localhost:5000  → different origin
  → for "simple" requests the browser sends the request with an Origin header
  → for requests with JSON body / custom headers (Authorization), the browser FIRST sends a
      "preflight" OPTIONS request asking "am I allowed to do this?"
  → ASP.NET's UseCors policy checks: is Origin in AllowedOrigins? header/method allowed?
  → if yes → server responds with CORS headers:
        Access-Control-Allow-Origin: http://localhost:5173
        Access-Control-Allow-Headers: *
        Access-Control-Allow-Methods: *
        Access-Control-Allow-Credentials: true
  → browser sees the headers and lets the real request go through
  → API handles it (authentication etc.) → JSON response → React updates UI
```

Because `WithCredentials` is used, the backend never returns `*` for the origin — it must
echo the exact origin. This is also why `AllowCredentials()` requires explicit origins.

---

## 5. Important Internal Logic

- **Origins:** `WithOrigins(config["AllowedOrigins"].Split(","))` — list of trusted origins, currently `http://localhost:5173`.
- **Headers:** `AllowAnyHeader()` — any header is fine (needed because the frontend sends `Authorization: Bearer …` and `Content-Type: application/json`).
- **Methods:** `AllowAnyMethod()` — GET/POST/PUT/PATCH/DELETE all allowed.
- **Credentials:** `AllowCredentials()` — cookies/credentials allowed (the axios client uses `withCredentials: true`).
- **Ordering matters:** `app.UseCors("LearnPathCors")` is called **before** `app.UseAuthentication()` and `app.UseAuthorization()`. The request has to pass CORS in the pipeline before it can be checked for the JWT token.

What happens when a request is blocked (origin NOT in the list):

- The browser blocks the JavaScript from reading the response.
- The frontend axios call fails with a `NetworkError` / CORS error in the console.
- The request may still hit the server, but the browser hides the response from the app — this is why the UI shows a generic error rather than the API message.

---

## 6. Frontend Side

Flow:

```
Frontend service (e.g. quizService)
  → apiClient (axios)  →  baseURL = VITE_API_BASE_URL ?? "http://localhost:5000/api/v1"
  → axios config sets withCredentials: true
  → the browser enforces CORS on each request to the backend
  → response interceptor handles the API's ApiResponse envelope
  → UI state update
```

Key file: **`frontend/src/services/apiClient.ts`** — all services (`authService`, `quizService`,
`classroomService`, etc.) go through this single axios instance, so CORS is applied consistently
across the whole app. The developer can change the API URL and CORS target via the
`VITE_API_BASE_URL` environment variable.

Note: the backend URL and the frontend dev URL must match the `AllowedOrigins` entry, otherwise
every call fails in the browser.

---

## 7. Backend Side

- Where defined: `Program.cs` (service registration + middleware).
- Policy name: `"LearnPathCors"`.
- Config source: `AllowedOrigins` key in `appsettings.json` (overridable per-environment).
- Middleware order: CORS → Authentication → Authorization → Controllers.

There is no separate CORS code in services/controllers — it's pure pipeline configuration.

---

## 8. Database Side

Not applicable — CORS is transport-level; no tables, entities or query impact.

---

## 9. Security / Authorization

- CORS is **not** a security boundary for the API itself. The API still requires the JWT
  (`[Authorize]` on controllers). CORS only controls what *browser pages* may call it.
- A non-browser client (Postman, curl, mobile app) is **not** affected by CORS.
- Because it's an explicit allowlist (not `AllowAnyOrigin`), only the configured frontend
  can call the API from a browser.

---

## 10. Simple Real Example

A user logs in from the React app on `http://localhost:5173`. The app calls
`POST http://localhost:5000/api/v1/auth/login`. The browser sees a cross-origin request and
first sends a preflight `OPTIONS` request. ASP.NET Core's `LearnPathCors` policy checks the
origin `http://localhost:5173` against the allowlist, finds a match, and answers with the
CORS headers. The browser then sends the actual login request, gets the JWT, and stores it.

If the frontend were instead opened on `http://localhost:5174` (not configured), the same
call would be blocked at the browser and the login would silently fail.

---

## 11. Interview Answer

> "LearnPath has a React frontend and an ASP.NET Core API on different ports, so the browser
> blocks requests between them by default. I enabled CORS on the backend in `Program.cs` using a
> named policy, `LearnPathCors`, which explicitly allows the frontend origin from configuration —
> `http://localhost:5173` — and allows any header, any method, and credentials. The middleware
> runs before authentication. On the frontend, all services share a single axios client pointing
> at the API, so the CORS setup is consistent across the whole app. I used an explicit allowlist
> rather than allowing any origin, which is safer."

(≈ 30–40 seconds)

---

## 12. Follow-Up Questions

**Q: Why is CORS needed at all?**
Because the frontend and backend run on different origins (different ports), and browsers block cross-origin requests from JavaScript unless the server explicitly allows them.

**Q: Where exactly is it configured?**
In `backend/Program.cs` — `AddCors` defines the `LearnPathCors` policy, and `app.UseCors("LearnPathCors")` activates it. The origins list comes from `appsettings.json` (`AllowedOrigins`).

**Q: What's a preflight request?**
When a request uses methods like PUT/POST with a JSON body or custom headers (`Authorization`), the browser first sends an `OPTIONS` request to ask the server which origins/methods/headers are allowed. If the preflight fails, the real request never happens.

**Q: Why `AllowCredentials()` instead of `*` (any origin)?**
Browsers don't allow `Allow-Credentials` with a wildcard origin. Since our axios client sends credentials (`withCredentials: true`), we must list real origins and echo the exact allowed origin.

**Q: Does CORS protect the API from attackers?**
No. It only controls browsers. The real protection is the JWT authorization on every controller. Anyone using curl or Postman is unaffected by CORS.

**Q: What happens if the origin is not allowed?**
The browser blocks the response from reaching the JavaScript. The app gets a network error even though the server may have processed the request.

**Q: How would you add another frontend (e.g. a mobile web app)?**
Add its origin to the comma-separated `AllowedOrigins` value in configuration, or move the list to an environment variable / secrets.

**Q: What are the limitations?**
If the frontend and API are ever served from the *same* origin (e.g. behind a proxy), CORS isn't needed. The allowlist must be maintained as new frontends appear.

---

## 13. Functionality

Confirmed from source code:

- CORS policy `LearnPathCors` defined in `Program.cs` (lines 158–169).
- Middleware `app.UseCors("LearnPathCors")` runs before authentication/authorization (line 300).
- Origin allowlist from configuration: `http://localhost:5173` (`appsettings.json`).
- `AllowAnyHeader()`, `AllowAnyMethod()`, `AllowCredentials()` enabled.
- Frontend axios client uses `withCredentials: true`, `baseURL` from `VITE_API_BASE_URL`.