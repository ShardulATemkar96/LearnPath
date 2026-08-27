# Phase 4 — ASP.NET Core (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

All LearnPath references verified from source (`backend/Program.cs`, controllers,
middleware, Swagger config).

---

## 1. What is ASP.NET Core? 🔥

### What is it?
ASP.NET Core is Microsoft's cross-platform framework for building web apps and APIs
on modern .NET.

### Why is it used?
Fast, cross-platform, cloud-ready, with built-in dependency injection, middleware,
authentication, and a unified hosting model (`WebApplication`).

### Interview Answer
"ASP.NET Core is the .NET framework for building web applications and REST APIs. It's
cross-platform, runs on the modern .NET runtime, and has everything built in —
hosting, dependency injection, middleware pipeline, authentication, and Swagger
support. My LearnPath backend is an ASP.NET Core Web API — `Program.cs` builds the
`WebApplication`, wires services and middleware, and maps controllers."

### LearnPath Example
`backend/Program.cs` — `var builder = WebApplication.CreateBuilder(args);` ...
`var app = builder.Build();` ... `app.Run();`

### Common Mistake
- Calling it "ASP.NET" or "ASP"— ASP.NET Core is the modern cross-platform successor.

### Remember
- ASP.NET Core = modern web framework on modern .NET. Program.cs = entry point.

---

## 2. Middleware 🔥

### What is it?
Components in the request pipeline, arranged in order; each one can process the
request, pass it to the next, and process the response on the way back. They wrap the
request/response like layers of an onion.

### Why is it used?
Cross-cutting concerns — logging, exceptions, rate limiting, auth, CORS — stay out of
controller code and apply to every request uniformly.

### Interview Answer
"Middleware are components that sit in the request pipeline. Each one can inspect or
modify the request, optionally call the next one with `await _next(context)`, and
then inspect the response on the way out. I have three custom middleware in
LearnPath: `ExceptionMiddleware` for errors, `LoggingMiddleware` for request logging,
and `RateLimitingMiddleware` for throttling."

### Simple Example
```csharp
public class LoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);                       // let the request continue
        // after: work with the response
    }
}
```

### LearnPath Example
`backend/Middleware/LoggingMiddleware.cs` — logs method, path, status, and elapsed ms
for every request. Registered first in the pipeline so it wraps everything.

### How it works
```text
Request → MW1 → MW2 → MW3 → Controller → MW3 → MW2 → MW1 → Response
          (each MW wraps the next with try/finally-style flow)
```

### Common Mistake
- Wrong order matters: exception middleware must be early so it catches errors from
everything below.

### Follow-Up Questions
Q: How do you write custom middleware?
A: A class with a `RequestDelegate _next` and an `InvokeAsync(HttpContext)` method,
registered with `app.UseMiddleware<T>()`.

### Remember
- Middleware = layered pipeline. Behind every request, exactly.

---

## 3. Request pipeline 🔥

### What is it?
The ordered series of middleware the request passes through, ending in an endpoint
(controller action). Registered with `app.Use...` in `Program.cs`.

### Why is it used?
A single, predictable place to apply cross-cutting concerns for all endpoints.

### Interview Answer
"The request pipeline is the sequence of middleware that runs for every request. In
Program.cs I add them in order: Exception, Logging, RateLimiting, then Swagger (dev),
CORS, Authentication, Authorization, and finally `MapControllers()`. The order
matters — authentication runs before authorization, and exception handling runs
first so it can catch errors from everyone downstream."

### LearnPath Example
Verified pipeline in `Program.cs:285-303`:
```csharp
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
if (IsDevelopment) { UseSwagger(); UseSwaggerUI(); }
if (!IsDevelopment) UseHttpsRedirection();
app.UseCors("LearnPathCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### Common Mistake
- Swapping Authentication/Authorization order — authorization must come after
authentication.

### Remember
- Pipeline = use order. Exception first, auth before authz, controllers last.

---

## 4. Controllers 🔥

### What is it?
Classes that handle HTTP requests and return responses. They map HTTP verbs + routes
to action methods. Inherit from `ControllerBase` + marked `[ApiController]`.

### Why is it used?
They are the "entry point" of API logic: parse/extract data via model binding, call
services, and format the response with `Ok(...)`, `BadRequest(...)`, etc.

### Interview Answer
"A controller is a class that accepts HTTP requests and returns HTTP responses. It
inherits `ControllerBase` and is marked `[ApiController]`. Each action method maps to
a route + verb. Their job is thin: receive the request, call a service, and return a
proper `IActionResult` — they should not contain business logic."

### Simple Example
```csharp
[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
        => Ok(ApiResponse<UserProfileResponseDto>.Ok(await _service.GetProfileAsync(UserId)));
}
```

### LearnPath Example
`backend/Controllers/AuthController.cs`, `UserController.cs`, `AdminController.cs`,
`LearningPathController.cs`, etc. — thin controllers delegating to services.

### Common Mistake
- Fat controllers full of business logic — keep them thin and delegate to services.

### Follow-Up Questions
Q: ControllerBase vs Controller?
A: `ControllerBase` = no view support (APIs); `Controller` = adds MVC view features.

### Remember
- Controller = thin request handler → delegates to services → returns IActionResult.

---

## 5. Routing 🔥

### What is it?
Mechanism that matches an incoming URL + HTTP verb to a controller action.

### Why is it used?
URLs are the public contract of the API — routing maps them to code.

### Interview Answer
"Routing matches an incoming request's path and HTTP method to a controller action.
ASP.NET Core uses attribute routing: I put `[Route("api/v1/...")]` on the controller
and `[HttpGet("...")]`, `[HttpPost("...")]` etc. on actions. The `{...}` segments in
routes are route parameters, like `users/{userId}`."

### LearnPath Example
`[Route("api/v1/auth")]` + `[HttpPost("register")]` → POST `api/v1/auth/register`.
`[HttpGet("users/{userId}")]` in `AdminController` → route param `userId`.

### Common Mistake
- Forgetting the verb — default routing requires explicit verb attributes.

### Remember
- Route on class + verb+path on action + `{params}` = URL map.

---

## 6. Attribute routing 🔥

### What is it?
Routing defined directly on controllers/actions via attributes (`[Route]`,
`[HttpGet]`, `[HttpPost]`, `[HttpGet("{id}")]`) instead of a central config.

### Why is it used?
Readable, self-documenting, flexible route templates.

### Interview Answer
"Attribute routing means putting route templates on controllers and actions instead
of a central route table. `[Route("api/v1/learning-paths")]` on the class and
`[HttpGet("{id:int}")]` on an action. It keeps the URL visible at the code that
handles it. It's the standard for Web APIs."

### Simple Example
```csharp
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet("certificates/{id:int}")]   // GET api/v1/admin/certificates/42
    public async Task<IActionResult> GetCertificate(int id) { ... }
}
```

### LearnPath Example
`AdminController` — `{id:int}` route constraint (only integers); `{userId}` string.
`AuthController` — `api/v1/auth/register`.

### Common Mistake
- Missing constraints like `{id:int}` when you need to avoid string-to-int errors.

### Remember
- Attribute routing = URL defined next to code. Constraints: `{id:int}`.

---

## 7. Model binding 🔥

### What is it?
ASP.NET Core builds action parameters from request data automatically: `[FromBody]`
from JSON body, `[FromQuery]` from query string, route params from the URL, `[FromHeader]`
from headers.

### Why is it used?
No manual parsing — the framework maps HTTP data to typed objects, including
validation of types.

### Interview Answer
"Model binding is how ASP.NET Core turns request data into method parameters. By
default, route values and query strings are bound; I use `[FromBody]` for JSON bodies
and `[FromQuery]` for query parameters. The framework creates the DTO object and
fills its properties from the request."

### Simple Example
```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequestDto dto) { ... }

[HttpGet("users")]
public async Task<IActionResult> GetUsers([FromQuery] string? search) { ... }
```

### LearnPath Example
`AdminController` uses `[FromBody]` DTOS for create/update, `[FromQuery]` for filters
and pagination (`page`, `pageSize`), and route params like `{userId}`, `{id:int}`.

### Common Mistake
- Forgetting `[FromBody]` on a complex object (in an API controller it is assumed,
but explicit is readable) or expecting JSON body in a query param.

### Follow-Up Questions
Q: Where does the JSON come from?
A: The body, deserialized by System.Text.Json into the parameter type.

### Remember
- Body/query/route/header → parameters. JSON goes to DTOs with `[FromBody]`.

---

## 8. Model validation 🔥

### What is it?
Checking that incoming data is correct before using it. Two ways: data annotations
(`[Required]`) and FluentValidation (rules like `RuleFor(...).NotEmpty()`).

### Why is it used?
Bad input is the developer's #1 enemy — validate once at the boundary, not in every
service.

### Interview Answer
"Validation ensures bad data never reaches my services. `[ApiController]` triggers
automatic validation of the bound model, and I use FluentValidation rules for
complex checks — minimum length, email format, password complexity. If validation
fails, ASP.NET responds 400 automatically. In my project I customized this to return
the `ApiResponse` envelope instead of raw error details, so the frontend always gets
my consistent error format."

### LearnPath Example
- FluentValidation rules: `backend/Validators/Auth/RegisterRequestValidator.cs`
  (email, min length, uppercase, digit).
- `Program.cs` — `builder.Services.AddValidatorsFromAssemblyContaining<Program>()`
  auto-registers all validators.
- `Program.cs` — `ConfigureApiBehaviorOptions` converts ModelState failures into
  `ApiResponse<object>.Fail(...)` with 400.

### How it works
```text
Request body → bind to DTO → validators run → errors collected → model state invalid
→ AutomaticModelValidation returns 400 (custom envelope) → controller never runs
```

### Common Mistake
- Relying only on DataAnnotations in DTO classes and not mentioning FluentValidation.

### Remember
- Validate at the boundary. FluentValidation + automatic 400 via [ApiController].

---

## 9. DTOs (Data Transfer Objects) 🔥

### What is it?
Plain classes that carry data between layers / over the wire — using only the fields
the API needs, decoupled from entities.

### Why is it used?
Entities often have fields that must not be exposed (passwords, internal flags).
DTOs shape the response, avoid over-posting, and decouple API contract from DB model.

### Interview Answer
"A DTO is a plain object that carries data between layers or to the client. I use
different DTOs for requests and responses — for example `LoginRequestDto` for input
and `AuthResponseDto` for the response with token + user info. This way I never
expose entity internals and the API shape is stable even if the database model
changes."

### LearnPath Example
`backend/DTOs/Auth/LoginRequestDto.cs`, `AuthResponseDto.cs`;
`backend/DTOs/LearningPath/LearningPathResponseDto.cs`; with AutoMapper mappings
(`backend/Mappings/AuthMappingProfile.cs`).

### Common Mistake
- Returning entities directly from controllers — exposes extra data + couples DB to
API.

### Remember
- DTO = trimmed, safe, contract-shaped data. Entities never cross wires directly.

---

## 10. Dependency Injection in ASP.NET Core 🔥

### What is it?
Built-in container: register services in `Program.cs` (`AddScoped`, `AddSingleton`,
`AddTransient`) and inject them via constructors. The container resolves the full
dependency graph automatically.

### Why is it used?
Decoupling, testability, centralized lifecycle management — without a third-party
library.

### Interview Answer
"ASP.NET Core has a built-in DI container. I register everything in Program.cs —
scoped services, the singleton `JwtTokenGenerator`, the scoped `ApplicationDbContext` —
and controllers/services receive them through constructors. When a request comes in,
the container builds the controller and all of its dependencies automatically. This
is exactly the Dependency Injection pattern from Phase 3, applied at framework
level."

### LearnPath Example
`Program.cs:209-226` — all `AddScoped<IXService, XService>()` registrations, plus
`AddSingleton<JwtTokenGenerator>()`, `AddDbContext`, `AddHttpClient`.

### Common Mistake
- Registering DbContext as Singleton — must be scoped.

### Remember
- Register in Program.cs, inject in constructors, container resolves the rest.

---

## 11. Filters 🔥

### What is it?
Attributes or classes that run code around controller actions: before, after, or
instead of an action. Types: authorization, resource, action, exception, result.

### Why is it used?
Reusable logic for selected endpoints — [Authorize], logging, caching, validation.

### Interview Answer
"Filters run around controller actions and give more targeted control than
middleware. The main types are authorization filters (security check first), action
filters (around the action's work), exception filters (catch errors in an action),
and result filters. The `[Authorize]` attribute I use everywhere is the most famous
filter."

### LearnPath Example
The project uses built-in filters: `[Authorize]`, `[Authorize(Roles = "Admin")]`,
`[ProducesResponseType(...)]` (documents Swagger). Custom filters are not defined — 
custom cross-cutting logic lives in middleware instead.

### Common Mistake
- Filter vs middleware confusion — middleware = whole pipeline; filter = around a
specific action/controller.

### Remember
- Filters = per-endpoint hooks. [Authorize] is a filter.

---

## 12. Authorization filters 🔥

### What is it?
The earliest filter stage — determines whether a user is allowed at all, before
model binding. Implemented via `[Authorize]` / policies.

### Why is it used?
Reject unauthorized calls before any action work happens. Security gate #1.

### Interview Answer
"Authorization filters run first — before model binding — to decide if the user is
allowed. `[Authorize]` is the built-in one: no valid identity, 401; wrong role, 403.
I put `[Authorize]` on protected controllers and `[Authorize(Roles = "Admin")]` where
only admins may act."

### LearnPath Example
`UserController` (`[Authorize]` at class level), `AdminController`
(`[Authorize(Roles = "Admin")]`), `AuthController` has `[AllowAnonymous]` on
register/login/refresh. Policies defined in Program.cs: `AdminOnly`, `InstructorOrAdmin`.

### Common Mistake
- Forgetting `[AllowAnonymous]` on the login/register endpoints (they'd need a token
to get a token).

### Remember
- [Authorize] = gate at the start. Roles + policies refine it.

---

## 13. Resource filters ⚪

### What is it?
A filter stage that runs right after authorization, before model binding — can
short-circuit based on the resource/state.

### Why is it used?
Rarely used; mainly for caching or request-wide checks tied to the resource.

### Interview Answer
"Resource filters run between authorization and model binding. They can short-circuit
the pipeline and serve a cached result before the action runs. They're a specialized
filter — I haven't needed a custom one in the project; exceptions go through
middleware and access checks through authorization."

### Common Mistake
- Claiming you used them — keep to general knowledge if unfamiliar.

### Remember
- Resource filter = early, can short-circuit; niche.

---

## 14. Action filters ⚪

### What is it?
Run just before and after the action method executes — can wrap the action work.

### Why is it used?
Adding behavior around actions (logging, benchmarking, modifying params/results).

### Interview Answer
"Action filters wrap the action method itself — code that runs before it and after
it returns. Useful for logging action timing, modifying arguments, or messing with
results. Not used as a custom class in my project — the middleware and logging
middleware cover the needs."

### Common Mistake
- Overcomplicating: if the concern spans all requests, middleware; if one endpoint,
filter.

### Remember
- Action filter = around the action. Custom ones absent in the project.

---

## 15. Exception handling 🔥

### What is it?
Dealing with exceptions so users get a clean error and the app keeps running. In
ASP.NET Core: local try/catch + global middleware + built-in developer exception page.

### Why is it used?
Unhandled exceptions otherwise return ugly 500s or crash requests; structured error
handling returns consistent, safe messages.

### Interview Answer
"Exception handling in ASP.NET Core is layered. Controllers can try/catch specific
exceptions, and a global middleware catches anything that escapes. My
`ExceptionMiddleware` is the last line of defense: it maps `UnauthorizedAccessException`
to 403, `KeyNotFoundException` to 404, `ArgumentException` to 400, FK-violations to
409, and everything else to a generic 500, all in the `ApiResponse` JSON format."

### LearnPath Example
`AuthController.Login` has try/catch → 401. `AdminController` catches
`KeyNotFoundException` / `ArgumentException` → 404/400. Everything else →
`ExceptionMiddleware`.

### Common Mistake
- Catching in every single method — one global handler + specific local catches is
the clean pattern.

### Remember
- Throw typed exceptions; global middleware maps them to HTTP codes.

---

## 16. Global exception handling 🔥

### What is it?
A central place that catches every unhandled exception across all endpoints.

### Why is it used?
No error leaks to raw 500s; consistent JSON errors; better security (no stack leaks).

### Interview Answer
"Global exception handling is one middleware on top of the pipeline that catches any
unhandled exception. Mine is `ExceptionMiddleware` registered first. It logs the full
error, then by exception type chooses a status code and a safe user-facing message —
never leaking stack traces — so every failure returns the same `ApiResponse` shape."

### LearnPath Example
`backend/Middleware/ExceptionMiddleware.cs` — catches, logs with `ILogger`, switches on
exception type, writes `ApiResponse<object>.Fail(...)` JSON. Also guards against
writing a body when the response already started.

### Common Mistake
- Returning the raw exception message in production — leaks internals.

### Remember
- One middleware catches all; switch on type → status + safe message.

---

## 17. Configuration (recap for ASP.NET Core) 🟡

### What is it?
Reads settings from multiple sources (appsettings, env vars) into a key-value
`IConfiguration`; used everywhere via `builder.Configuration`.

### Why is it used?
Secrets and environment-specific settings never live in compiled code.

### Interview Answer
"In ASP.NET Core, `builder.Configuration` collects settings from appsettings.json,
appsettings.{Environment}.json, and environment variables. I read connection strings
with `GetConnectionString`, sections with `GetSection`, and bind sections to typed
options classes. Secrets like the JWT secret come from env vars to keep them out of
source control."

### LearnPath Example
`Program.cs` — `.env` loaded with DotNetEnv, mapped to config keys, and injected with
`AddInMemoryCollection`. Verified earlier in Phase 3.

### Common Mistake
- Hard-coding config values; leaking secrets into git.

### Remember
- IConfiguration reads layered sources; env wins over files.

---

## 18. Logging (ASP.NET Core) 🟡

### What is it?
`ILogger<T>` injectable everywhere; providers (console, debug, file) and levels
controlled in appsettings.

### Why is it used?
Observe behavior in production without debuggers.

### Interview Answer
"ASP.NET Core logging is built in: I inject `ILogger<T>` and call
`_logger.LogInformation` / `LogError` with structured messages. The appsettings
`Logging` section sets minimum levels per namespace, e.g. EF Core SQL logs get
verbose in Development but stay quiet in Production."

### LearnPath Example
`LoggingMiddleware` and `ExceptionMiddleware` use `ILogger`. `Logging` section in
`appsettings.Development.json` sets `Microsoft.EntityFrameworkCore.Database.Command`
to Information for SQL visibility in dev.

### Common Mistake
- `Console.WriteLine` instead of ILogger — breaks structured logging + config levels.

### Remember
- ILogger + config-driven levels. Never Console.WriteLine for app diagnostics.

---

## 19. Swagger / OpenAPI 🟡

### What is it?
OpenAPI = machine-readable API definition. Swagger = tooling (Swashbuckle) that
generates a UI from it — a live, click-to-try documentation page.

### Why is it used?
Documentation + testing for free from annotations/XML comments; a de-facto standard
for APIs.

### Interview Answer
"Swagger generates interactive API documentation from the code. With
Swashbuckle, an OpenAPI JSON is produced at runtime and the Swagger UI lets you
call endpoints from the browser. I configured the Bearer security scheme so each
request can carry the JWT for testing protected endpoints. It's enabled only in
Development in my project."

### LearnPath Example
`Program.cs:240-279` — `AddSwaggerGen` with title, XML comments, and a "Bearer" JWT
security definition. `SwaggerConfig.cs` has additional setup. Only served when
`app.Environment.IsDevelopment()`.

### Common Mistake
- Leaving Swagger/UI enabled in production — a public attack surface.

### Remember
- Swagger = live docs + test UI. Bearer security for JWT testing. Dev-only.

---

## 20. Important ASP.NET Core interview questions 🔥

Quick ammo (each 15-30 sec answers):

Q: What is `[ApiController]`?
A: Enables automatic model validation, attribute routing assumptions, and
`BadRequest` responses on invalid models.

Q: What is the difference between `AddControllers` and `AddControllersWithViews`?
A: APIs don't need views; use `AddControllers` for a pure Web API.

Q: What does `app.MapControllers()` do?
A: Wire controller actions into the endpoint routing — the request pipeline ends at
a matched action.

Q: `IActionResult` vs `ActionResult<T>`?
A: `ActionResult<T>` declares the response type for Swagger + consumers (typed);
`IActionResult` is more general.

Q: What is `ControllerBase`?
A: The base class for API controllers without MVC view support.

Q: What is endpoint routing?
A: Modern routing where middleware can also route (MapGet, MapControllers) — the
pipeline uses a single route table.

Q: How do you enable CORS?
A: `AddCors` with a policy (origins/methods/headers) + `app.UseCors("PolicyName")`
between routing and auth. In LearnPath it's `LearnPathCors`.

Q: What is `Program.cs` structure in minimal vs controller-based APIs?
A: Minimal APIs use `MapGet`/`MapPost` lambdas; controller-based (this project) uses
controllers with attribute routing. Both share the same hosting model.

### Common Mistake
- Answering these with textbooks — keep each to 2-3 sentences and mention the project.

### Remember
- [ApiController] + MapControllers + attribute routing + middleware order + DI.

---

## Final Phase 4 "remember by heart" checklist

1. Program.cs: builder → services → build → middleware order → run.
2. Pipeline order: Exception → Logging → RateLimit → CORS → Authn → Authz →
   Controllers.
3. Controllers are thin; attributes carry routes; DTOs carry data; services carry
   logic.
4. Validation = FluentValidation + automatic 400 envelope.
5. Global exception middleware = consistent error JSON.
6. Swagger = dev-only docs with Bearer JWT.

Next: `05_Web_API_REST.md` — say "Proceed to next phase" when ready.