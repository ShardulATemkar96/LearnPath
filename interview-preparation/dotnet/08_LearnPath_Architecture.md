# Phase 8 — LearnPath .NET Architecture (Interview Preparation)

This document connects everything from Phases 1–7 into one coherent story: the real
LearnPath request lifecycle, why each layer exists, and the project-based interview
questions.

Every file mentioned below was verified from source.

---

## 1. The big picture

### What is it?
LearnPath is a graph-based personalized learning platform:
- **Backend**: ASP.NET Core Web API (`.NET 10`), controllers-based, in `backend/`.
- **Frontend**: React app in `frontend/` (calls the API).
- **Database**: SQL Server (EF Core ORM).
- **Auth**: JWT access tokens + stored refresh tokens, roles via ASP.NET Identity.

### Why is it used?
Clean separation of concerns makes the app simple to build, test, and extend.

### Interview Answer
"LearnPath is a learning platform where users follow learning paths made of modules
with prerequisites — it's modeled as a graph. The backend is a controller-based
ASP.NET Core Web API: HTTP requests come through a middleware pipeline, hit
controllers, then services that apply business rules and talk to the database through
EF Core. Responses are wrapped in a consistent `ApiResponse` JSON envelope. The
frontend is a React app that calls this API. The whole backend is in `backend/` with
Controllers, Services, Repositories, Entities, DTOs, and Middleware folders."

---

## 2. Verified folder structure

```text
backend/
  Program.cs                    # config, DI wiring, middleware pipeline
  Controllers/                  # HTTP entry points (Auth, User, Paths, Classroom...)
  DTOs/                         # request/response data shapes (per feature)
  Validators/                   # FluentValidation rules per DTO
  Mappings/                     # AutoMapper profiles (entities ↔ DTOs)
  Interfaces/Services/          # service contracts (IAuthService, ...)
  Interfaces/Repositories/      # repository contracts (IGenericRepository<T>)
  Services/                     # business logic (Auth, LearningPath, ...)
  Repositories/                 # data access (GenericRepository<T>, CommunityRepository)
  Data/                         # ApplicationDbContext, Seeders
  Entities/                     # database models (User, LearningPath, Module, ...)
  Configurations/               # EF Fluent API configs per entity
  Migrations/                   # EF Core schema migrations
  Middleware/                   # Exception, Logging, RateLimiting
  Authentication/Jwt/           # JwtTokenGenerator, JwtSettings
  Common/                       # ApiResponse<T> envelope
  Validators/ + Swagger/        # docs + validation
```

---

## 3. The request lifecycle (memorize this diagram)

```text
Browser / React
     │  JSON request  (method + URL + Authorization header + body)
     ▼
1. ExceptionMiddleware     (catches any error from anything below)
2. LoggingMiddleware       (logs method, path, status, elapsed ms)
3. RateLimitingMiddleware  (per-IP throttle, 429 when exceeded)
4. UseCors("LearnPathCors") (origin gate)
5. UseAuthentication()     (JwtBearer validates token → User object)
6. UseAuthorization()      ([Authorize] + roles check)
     ▼
   ROUTER → Controller action
     │  model binding: [FromBody] DTO ← JSON body, [FromQuery] ← query string
     │  FluentValidation rules run (400 with ApiResponse envelope if invalid)
     ▼
7. Service (IAuthService / ILearningPathService / ...)
     │  business rules, ownership checks, graph validation, audit logging
     ▼
8. Repository / ApplicationDbContext (EF Core)
     │  LINQ → SQL → SQL Server → materialized entities
     ▼
9. Service maps entities → DTOs (AutoMapper / manual)
     │
     ▼
   Controller returns Ok(ApiResponse<T>.Ok(dto))
     │  serialized to JSON
     ▼
   Middleware unwinds, logs response, returns to React
```

### Why does each layer exist?

| Layer | Why it exists | Verified example |
|---|---|---|
| Middleware | Cross-cutting concerns for EVERY request, before controllers | `ExceptionMiddleware`, `LoggingMiddleware`, `RateLimitingMiddleware` |
| Controller | Thin HTTP entry: bind input, call service, shape response | `AuthController`, `UserController`, `LearningPathController` |
| DTO | Safe, contract-shaped payloads; never expose entities | `LoginRequestDto`, `AuthResponseDto`, `LearningPathResponseDto` |
| Validator | Reject bad input at the boundary with clear messages | `RegisterRequestValidator`, `CreateLearningPathValidator` |
| Service | Business rules + orchestration | `AuthService.LoginAsync`, `LearningPathService.GetByIdAsync` |
| Repository | Centralized/abstracted data access | `GenericRepository<T>`, `CommunityRepository` |
| DbContext | Unit of work, change tracking, queries | `ApplicationDbContext` |
| Configurations | Map entities to tables precisely | `LearningPathConfiguration`, `UserClassroomConfiguration` |
| ApiResponse | Consistent JSON envelope for success & errors | `Common/ApiResponse.cs` |

---

## 4. How Dependency Injection connects the layers

### What is it?
All wiring happens once in `Program.cs`; controllers and services never use `new`
for dependencies.

### Verified wiring
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();          // per request
builder.Services.AddScoped<IClassroomService, ClassroomService>();
...
builder.Services.AddSingleton<JwtTokenGenerator>();               // app lifetime
builder.Services.AddDbContext<ApplicationDbContext>(...);          // scoped
builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();
builder.Services.AddAutoMapper(...);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### How it flows
```text
Register in Program.cs → container → builds controller → needs IAuthService →
container builds AuthService → needs UserManager + JwtTokenGenerator +
ApplicationDbContext + IMapper + IAuditLogService → all resolved automatically
```

### Interview Answer
"DI wires the layers. In `Program.cs` I register interfaces to implementations, and
the container builds the whole chain. A controller gets `IAuthService`; the service
in turn gets `UserManager`, `JwtTokenGenerator`, `ApplicationDbContext`, `IMapper`,
and `IAuditLogService`. Nobody creates dependencies by hand, which is why I can swap
implementations and mock things in tests — `backend.Tests` does exactly that with
`AuthServiceTests`."

---

## 5. How authentication fits into a request

1. User calls `POST api/v1/auth/login` (`[AllowAnonymous]`).
2. `AuthService.LoginAsync` → finds user, `CheckPasswordAsync`, checks account status,
   loads roles.
3. `JwtTokenGenerator.GenerateAccessToken` → signed JWT (claims: sub, email, name,
   roles; expiry 60 min). `GenerateRefreshToken` → 64 random bytes.
4. Refresh token saved to DB (7 days) → `AuthResponseDto` returned: access + refresh
   token.
5. Next request: React sends `Authorization: Bearer <accessToken>`.
6. `UseAuthentication` (JwtBearer) validates signature/issuer/audience/exp → builds
   `User` claims principal.
7. `UseAuthorization` applies `[Authorize]` / role checks.
8. Controller reads current user: `User.FindFirstValue(ClaimTypes.NameIdentifier)`.

### Interview Answer
"Authentication happens at the boundary of the pipeline. Login issues a JWT; after
that, every request carries it in the Authorization header. `UseAuthentication`
validates the token and builds the `User` object, `UseAuthorization` enforces the
`[Authorize]` and role attributes, and controllers read the user id from claims.
When the access token expires, the app calls `auth/refresh`, which rotates the stored
refresh token and returns a fresh pair."

---

## 6. How exceptions are handled

1. Services throw typed exceptions (`ArgumentException`, `UnauthorizedAccessException`,
   `KeyNotFoundException`, `InvalidOperationException`).
2. Controllers may catch specific ones for special codes (login → 401).
3. Everything else bubbles to `ExceptionMiddleware` (registered FIRST):
   - logs the full error,
   - maps type → status: Argument 400 / Unauthorized 403 / NotFound 404 /
     FK-violation 409 / else 500,
   - returns safe JSON via `ApiResponse.Fail(...)`, never leaking internals.
4. Validation failures never reach services — the automatic model-validation returns
   400 with the same envelope.

### Interview Answer
"Error handling is layered. Services throw precise exceptions. Controllers catch a
few for special cases. The global `ExceptionMiddleware` is the safety net: it logs
the error, maps the exception type to the right HTTP status, and returns a safe,
consistent `ApiResponse` message. So the frontend always reads `success: false` plus
`message`, no matter what failed."

---

## 7. Project-based interview questions (with answers)

### Q: "Explain your project architecture."
**Answer**: "LearnPath is a learning platform backend built as an ASP.NET Core Web
API. Requests enter a middleware pipeline (exception handling, logging, rate
limiting, CORS, authentication, authorization), reach controllers, which validate
input and delegate to services. Services hold the business rules and query the
database through EF Core, either via the DbContext or a generic repository. Data
comes back as DTOs wrapped in a consistent `ApiResponse` envelope. The frontend is
React. Three pillars: controllers are thin, services do the work, responses are
consistent."

### Q: "Why did you use services?"
**Answer**: "To keep controllers thin and business rules testable. A controller should
only bind HTTP input and return a response. The real logic — ownership checks,
prerequisite graph validation, token handling, audit logging — lives in services like
`AuthService` and `LearningPathService`. That separation makes the code easy to test
in isolation, which is what `backend.Tests` does."

### Q: "Why DTOs?"
**Answer**: "Because entities reflect the database and often contain things clients
must not see, and the API shape should be stable. Login takes a `LoginRequestDto`
and returns `AuthResponseDto`. If I changed the database model, the API contract
stays the same; and I never accidentally leak internal fields. I also validate DTOs
at the boundary with FluentValidation."

### Q: "Why Entity Framework Core?"
**Answer**: "EF Core is an ORM that maps my entities to SQL Server tables and my LINQ
queries to SQL. I get type safety, automatic change tracking (SaveChanges generates
the correct INSERT/UPDATE/DELETE), and schema sync through migrations — the project
applies migrations at startup. For the complex queries I use Include/ThenInclude,
AsNoTracking for reads, and an explicit transaction for the multi-step path delete.
I never wrote SQL by hand."

### Q: "How does a request reach the database?"
**Answer**: "The request goes through middleware, then the router calls a controller
action. The controller binds and validates the DTO, calls a service, and the service
runs LINQ against `ApplicationDbContext` — EF translates it to a parameterized SQL
query, SQL Server executes it, and entities are materialized. The service maps them
to DTOs, wraps them in `ApiResponse`, and the controller returns JSON. For example,
`GET api/v1/paths/public` flows: controller → `LearningPathService.GetAllPublicAsync`
→ LINQ Where/Include → SQL → DTOs."

### Q: "How does authentication happen?"
**Answer**: "Login validates credentials via `UserManager`, then `JwtTokenGenerator`
issues a signed access token (60 min) containing the user id, email, and roles, plus
a 7-day refresh token stored in the database. Each request sends the access token in
the Authorization header; `UseAuthentication` validates signature, issuer, audience,
and expiry, and `UseAuthorization` enforces `[Authorize]` and role-based checks.
Expired access tokens are renewed through the refresh endpoint, which rotates the
refresh token."

### Q: "What middleware did you write?"
**Answer**: "Three custom ones: `ExceptionMiddleware` (global error mapping),
`LoggingMiddleware` (logs method, path, status, duration), and `RateLimitingMiddleware`
(per-IP 50 requests/minute with headers and 429 responses). Plus the built-in CORS,
authentication, and authorization middleware. Order matters — exception handling
first so it catches everything."

### Q: "Why the ApiResponse envelope?"
**Answer**: "So success and failure look identical in shape — `success`, `message`,
`data`, `errors`, `timestamp`. The frontend writes one parser and handles all
responses the same way. Even validation errors and global exceptions use it, which I
configured in `Program.cs`."

### Q: "How do you prevent students from accessing admin routes?"
**Answer**: "Two layers. First `[Authorize(Roles = "Admin")]` on `AdminController` and
`[Authorize(Roles = "Admin,Instructor")]` on privileged actions, backed by policies
`AdminOnly` and `InstructorOrAdmin`. Second, the service layer also checks ownership —
for example `GetOwnedPathAsync` throws `UnauthorizedAccessException` if the caller
isn't the path owner. Defense in depth: URL protection AND service-level checks."

### Q: "Describe a hard problem you solved."
**Answer** (choose one):
- "Deleting a learning path had many dependent rows, and SQL Server has retry logic,
  so a normal transaction isn't enough. I used the EF execution strategy, an explicit
  transaction, and deleted children in FK-safe order before committing — verified in
  `LearningPathService.DeleteAsync`."
- "Module prerequisites form a graph. `DagValidator` runs a DFS to reject dependencies
  that would create a cycle, and `GetByIdAsync` computes unlocked/completed states
  using a HashSet of completed module ids for O(1) checks."

---

## 8. Follow-up defenses (short answers)

Q: What would you improve if you had more time?
A: Add API versioning (v2 lives next to v1), pagination on bigger lists, and
instrumentation/metrics — plus caching for hot read endpoints.

Q: Why scoped DbContext?
A: One per request = a single unit of work; no state leaking across requests; avoids
concurrency issues.

Q: How do you keep controllers thin but not anemic?
A: Controllers orchestrate HTTP; services contain behavior. Validation is at the DTO
boundary; services are where rules live (ownership, prerequisites).

Q: How would you scale it?
A: Stateless API (JWT) scales horizontally; move rate limiting to a distributed store;
add caching, background jobs for audit/AI calls, and a CDN for static content.

---

## Final Phase 8 "remember by heart" checklist

1. Flow: Middleware → Controller → DTO + Validation → Service → Repository/EF →
   DB → DTO → ApiResponse JSON.
2. Controllers are thin; services hold rules; EF owns data; ApiResponse wraps
   everything.
3. DI in Program.cs connects all layers; nobody uses `new` for services.
4. Auth: JWT at the pipeline boundary (UseAuthentication → UseAuthorization), roles
   as claims, refresh rotation in AuthService.
5. Errors: typed exceptions → ExceptionMiddleware → mapped status + safe JSON.
6. Be ready to explain DELETE transaction and DAG validation to show depth.

Next: `09_Rapid_Revision.md` — say "Proceed to next phase" when ready.