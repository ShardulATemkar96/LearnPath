# Phase 5 — Web API + REST (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

Endpoints referenced below are verified from `backend/Controllers/` (routes, verbs,
status codes) and `backend/Program.cs`.

---

## 1. What is Web API? 🔥

### What is it?
A backend application that exposes business functionality over HTTP as JSON, consumed
by clients (React frontend, mobile apps). Built here with ASP.NET Core.

### Why is it used?
One server serves many clients over the internet/network using standard HTTP.

### Interview Answer
"A Web API is a backend service that exposes data and operations over HTTP, usually
returning JSON. The frontend calls it with HTTP requests for login, fetching paths,
submitting assignments. My LearnPath API is a Web API — ASP.NET Core controllers
expose endpoints under `api/v1/...` that the React app calls."

### LearnPath Example
All of `backend/Controllers/` — e.g. `AuthController` exposes `POST api/v1/auth/login`.

### Common Mistake
- Saying Web API == REST — REST is a *style*; a Web API is the implementation.

### Remember
- Web API = HTTP + JSON backend used by clients. This project is one.

---

## 2. REST 🔥

### What is it?
REST (Representational State Transfer) is an architectural style: resources are
identified by URLs and manipulated with HTTP verbs, statelessly, with standard status
codes; clients and server communicate via representations (JSON).

### Why is it used?
Simple, scalable, cacheable, language-neutral — the de-facto standard for APIs.

### Interview Answer
"REST is an architectural style for designing APIs. The core ideas: model your domain
as resources (students, paths, posts); address them with URLs; use HTTP verbs to
operate on them (GET to read, POST to create, PUT/PATCH to update, DELETE to remove);
use standard status codes for outcomes; and keep the server stateless — each request
carries what it needs. JSON is the representation. My API follows this: a learning
path is a resource at `api/v1/paths`, and I operate on it with GET/POST/PUT/DELETE."

### LearnPath Example
`LearningPathController` — resource `api/v1/paths/{id}` with GET, POST, PUT, DELETE —
textbook RESTful resource design.

### Common Mistake
- Saying "REST is a protocol" — it's a style; HTTP is the protocol.

### Remember
- REST = resources + verbs + status codes + stateless + JSON.

---

## 3. REST vs SOAP (basic) ⚪

### What is it?
SOAP is an older XML-based *protocol* with strict WSDL contracts. REST is a lighter,
JSON-based *style* over HTTP.

### Why is it used?
REST won for modern apps: simpler, faster, easier; SOAP survives in legacy/enterprise
(heavy security/spec needs).

### Interview Answer
"SOAP is a protocol with XML envelopes and a strict machine-readable contract (WSDL).
REST is a lighter architectural style using plain HTTP and JSON. REST is simpler,
faster, and the standard for web/mobile; SOAP is mainly in legacy enterprise
integrations with strict governance."

### Common Mistake
- Treating them as comparable — REST is a style, SOAP is a protocol.

### Remember
- SOAP = XML + strict; REST = HTTP + JSON + light.

---

## 4. HTTP 🔥

### What is it?
HyperText Transfer Protocol — the request/response protocol of the web. Client sends
a request (method + URL + headers + body), server replies with a status code + body.

### Why is it used?
The universal transport for web APIs — stateless, text-based, universally supported.

### Interview Answer
"HTTP is the protocol the web runs on. A request has a method (GET, POST...), a URL,
headers, and an optional body; the server answers with a status code, headers, and a
body. REST APIs are HTTP APIs — the API's job is to handle these requests correctly."

### LearnPath Example
Every endpoint in the project is an HTTP request/response. `ApiController` + ASP.NET
handles the protocol details; I return `IActionResult` and the framework writes the
HTTP response.

### Remember
- HTTP = method + URL + headers + body → status + headers + body.

---

## 5. GET 🔥

### What is it?
HTTP method to READ/FETCH a resource. Safe (no side effects) and idempotent.

### Why is it used?
Fetching data.

### Interview Answer
"GET reads data. It should never change server state — it's safe and idempotent.
I use it for endpoints like getting public paths, my profile, or my progress."

### LearnPath Example
`GET api/v1/paths/public` (`LearningPathController.GetPublic`),
`GET api/v1/users/me` (`UserController.GetProfile`),
`GET api/v1/progress` (`ProgressController.GetMyProgress`).

### Remember
- GET = read. Never mutate. Query params carry filters.

---

## 6. POST 🔥

### What is it?
HTTP method to CREATE a new resource or trigger an action. Not idempotent — each call
creates something new.

### Why is it used?
Creating records, login, uploads, actions.

### Interview Answer
"POST creates a new resource or triggers an action. Unlike GET it changes server
state and is not idempotent — calling it twice creates two resources. Create endpoints
like registering a user or creating a learning path are POSTs."

### LearnPath Example
`POST api/v1/auth/register`, `POST api/v1/auth/login`, `POST api/v1/paths`
(returns 201 Created via `CreatedAtAction`).

### Follow-Up Questions
Q: Idempotency meaning?
A: Repeating the call has the same effect as one call. GET/PUT/DELETE are idempotent; POST is not.

### Remember
- POST = create/action, changes state, not idempotent, body carries the data.

---

## 7. PUT 🟡

### What is it?
HTTP method to REPLACE a resource fully at a known URL. Idempotent — same request many
times = same result.

### Why is it used?
Full updates to a resource identified in the URL.

### Interview Answer
"PUT replaces a whole resource. The URL identifies it and the body is the complete
new state. It's idempotent — calling it again with the same body has the same result.
I use it for updating a learning path or module."

### LearnPath Example
`PUT api/v1/paths/{id:int}`, `PUT api/v1/paths/{pathId}/modules/{moduleId:int}`.

### Common Mistake
- Confusing PUT with PATCH — PUT replaces everything; PATCH changes only some fields.

### Remember
- PUT = full replacement, URL identifies the resource, idempotent.

---

## 8. PATCH 🟡

### What is it?
HTTP method to PARTIALLY update a resource — change only the fields sent.

### Why is it used?
Efficient updates (send just the changed field) and state-transition actions.

### Interview Answer
"PATCH applies a partial update — I send only the fields I want to change. It's the
right method for state transitions like publishing or archiving (change one flag).
My project uses PATCH for actions like `PATCH api/v1/quizzes/{id}/publish` and
updating a submission status."

### LearnPath Example
`PATCH api/v1/quizzes/{id:int}/publish`, `PATCH api/v1/submissions/{submissionId}/status`,
`PATCH api/v1/questionbanks/{id:int}/archive`.

### Remember
- PATCH = partial update / change a flag. PUT = full replace.

---

## 9. DELETE 🟡

### What is it?
HTTP method to REMOVE a resource. Idempotent (already-deleted behaves identically).

### Why is it used?
Removing data (with safety checks in business logic).

### Interview Answer
"DELETE removes a resource. It is idempotent — deleting the same thing twice has the
same end state. My delete endpoints come with business checks — e.g. you can't delete
a learning path that still has classrooms or certificates assigned."

### LearnPath Example
`DELETE api/v1/paths/{id:int}` (checks for dependent classrooms/certificates),
`DELETE api/v1/classrooms/{id:int}`, `DELETE api/v1/admin/users/{userId}`.

### Remember
- DELETE = remove + business rules first. Route identifies the resource.

---

## 10. HTTP status codes 🔥

### What is it?
Three-digit codes in the response: 2xx success, 3xx redirection, 4xx client error,
5xx server error.

### Why is it used?
A standard, machine-readable outcome so clients know if a call succeeded and why.

### Interview Answer
"Status codes are the standard outcome language of HTTP. The ones I use daily:
200 OK, 201 Created, 400 Bad Request (invalid input), 401 Unauthorized (not logged
in), 403 Forbidden (no permission), 404 Not Found, 409 Conflict (e.g. FK violation),
429 Too Many Requests, and 500 Internal Server Error."

### Simple Example
```csharp
return Ok(...);                          // 200
return CreatedAtAction(...);             // 201
return BadRequest(...);                  // 400
return NotFound(...);                    // 404
return Unauthorized(...);                // 401
```

### LearnPath Example
`AuthController` returns 200 / 401; `ExceptionMiddleware` maps types (400/403/404/409/500);
`RateLimitingMiddleware` returns 429; `HealthController` returns 503 when the DB is down.

### How to remember
1xx=info, 2xx=success, 3xx=redirect, 4xx=your fault, 5xx=server's fault.

### Follow-Up Questions
Q: 401 vs 403?
A: 401 = "identify yourself"; 403 = "identified but not allowed."

### Remember
- 2xx ok, 4xx client, 5xx server. 400/401/403/404 are the everyday ones.

---

## 11. Request vs response 🔥

### What is it?
Request = what the client sends (method, URL, headers, body, query). Response = what
the server returns (status code, headers, body).

### Why is it used?
It's the fundamental unit of HTTP — everything in the API is building or answering
these.

### Interview Answer
"A request has a method, URL, headers, and an optional body; a response has a status
code, headers, and a body. My controllers read the request via model binding and
return an `IActionResult` that becomes the response — always wrapped consistently in
the `ApiResponse` envelope so the frontend parses the same shape everywhere."

### LearnPath Example
Every controller action receives the request (`[FromBody]`, `[FromQuery]`, route
params) and returns `ApiResponse<T>` JSON — e.g. `Ok(ApiResponse<UserDto>.Ok(...))`.

### Remember
- Request in, response out. Shape it consistently (`ApiResponse`).

---

## 12. Headers 🟡

### What is it?
Key-value metadata sent with the request/response: `Content-Type`, `Authorization`,
`Accept`, custom headers like `X-RateLimit-*`.

### Why is it used?
Authorization tokens, content type negotiation, rate-limit info, caching.

### Interview Answer
"Headers are metadata key-value pairs. `Content-Type` says what the body is
(application/json), `Authorization` carries the JWT as `Bearer <token>`. I also set
custom headers — the rate limiter writes `X-RateLimit-Limit` and
`X-RateLimit-Remaining` on every response."

### Simple Example
```csharp
context.Response.Headers["X-RateLimit-Limit"] = MAX_REQUESTS.ToString();
```

### LearnPath Example
`RateLimitingMiddleware` sets `X-RateLimit-Limit` / `X-RateLimit-Remaining`.
Swagger UI sends `Authorization: Bearer <jwt>` for protected endpoints.

### Common Mistake
- Sending the JWT in the body/query — must be the Authorization header.

### Remember
- Headers = metadata: content type, tokens, rate limits.

---

## 13. Query parameters 🟡

### What is it?
Key-value pairs after `?` in the URL (`?search=csharp&page=2`).

### Why is it used?
Filtering, searching, sorting, pagination — without new URLs.

### Interview Answer
"Query parameters come after `?` in the URL and are for filtering, searching, and
pagination. I bind them with `[FromQuery]` — for example searching posts or
paginating user lists with `page` and `pageSize`."

### Simple Example
```csharp
[HttpGet("users")]
public async Task<IActionResult> GetUsers(
    [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
```

### LearnPath Example
`AdminController.GetUsers` (`?search=`), `CommunityController` list endpoints
(`category`, `search`, `tag`, `sort`, `page`, `pageSize`), `LearningPathController`
(`?includeUnpublished=`, `?moveUp=`).

### Remember
- Query string = filters + pagination. `[FromQuery]`.

---

## 14. Route parameters 🟡

### What is it?
Dynamic segments inside the URL template: `paths/{id}` — bind to method params.

### Why is it used?
Identifying a specific resource.

### Interview Answer
"Route parameters are variables inside the URL path — like `{id}` in
`api/v1/paths/{id}`. They identify which resource. I define them in the route
template and they bind to method parameters, often with constraints like `{id:int}`."

### Simple Example
```csharp
[HttpGet("paths/{id:int}")]
public async Task<IActionResult> GetById(int id) { ... }
```

### LearnPath Example
`LearningPathController` — `{id:int}`, `{pathId:int}/modules/{moduleId:int}`,
`AdminController` — `users/{userId}`.

### Common Mistake
- Confusing route params with query params — `{id}` is part of the path; `?x=1` is
the query string.

### Remember
- `{id:int}` = path variable; `?x=` = query. Route identifies the row.

---

## 15. Request body 🔥

### What is it?
The payload of the request, usually JSON, carrying the data being created/updated.

### Why is it used?
POST/PUT/PATCH actions need data — the body is where it lives.

### Interview Answer
"The body is the JSON payload of the request — for login it's email + password, for
creating a path it's the path details. It's bound to a DTO with `[FromBody]`,
validated, then passed to a service. A GET has no body; POST/PUT/PATCH typically do."

### Simple Example
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
```

### LearnPath Example
`LoginRequestDto` (`Email`, `Password`) bound in `AuthController.Login`.
`CreateLearningPathDto` bound in `LearningPathController.Create`.

### Remember
- Body = JSON data. [FromBody] + DTO + validation.

---

## 16. JSON 🟡

### What is it?
JavaScript Object Notation — the text format APIs exchange: `{"key": "value", ...}`.

### Why is it used?
Human-readable, language-neutral, lightweight — the default for REST APIs.

### Interview Answer
"JSON is the standard data format for web APIs — key-value pairs and arrays. The
backend serializes C# objects to JSON for responses and deserializes JSON to DTOs
for requests. ASP.NET Core uses System.Text.Json; I configured it to serialize enums
as strings (`JsonStringEnumConverter`) so the frontend sees readable values like
'Beginner' instead of '0'."

### LearnPath Example
`Program.cs:202-205` — `JsonStringEnumConverter` for enum-as-string. Every response
is JSON via `ApiResponse<T>`.

### Remember
- JSON = API data format. C# ↔ JSON via System.Text.Json. Enums→strings configured.

---

## 17. DTO (recap) 🟡

### What is it?
Plain objects shaped for the API — request DTOs carry input, response DTOs carry
output. Never send entities directly.

### Why is it used?
Security (no leaked internals), stability (API shape independent of DB), clarity.

### Interview Answer
"DTOs are the objects my API actually accepts and returns. `LoginRequestDto` for input,
`AuthResponseDto` for output. Using them decouples my API contract from my entities,
so I can change the database model without breaking the frontend, and I never
accidentally expose internal fields."

### LearnPath Example
`backend/DTOs/` — organized by feature: `Auth/`, `User/`, `LearningPath/`,
`Classroom/`, `Admin/`. AutoMapper maps entities ↔ DTOs (`Mappings/AuthMappingProfile.cs`).

### Remember
- DTO = API-shaped data. In and out, keep entities internal.

---

## 18. Serialization / deserialization 🟡

### What is it?
Serialization = converting objects to JSON (response). Deserialization = JSON back to
objects (request). Done by System.Text.Json in ASP.NET Core.

### Why is it used?
Objects live in memory; the wire needs text. The framework handles both directions
for you.

### Interview Answer
"Serialization turns C# objects into JSON; deserialization turns JSON into C# objects.
ASP.NET Core does this automatically — when I return `Ok(dto)` it serializes; when a
request body arrives it deserializes into the `[FromBody]` DTO. It's configurable —
I added the enum-as-string converter."

### LearnPath Example
Model binding (deserialization) + `ApiResponse<T>` responses (serialization) across
all endpoints. `ExceptionMiddleware` manually serializes with `JsonSerializer.Serialize`.

### Common Mistake
- Manually writing JSON strings — the framework + DTOs make it automatic.

### Remember
- Serialize out, deserialize in — automatic in ASP.NET Core.

---

## 19. API validation (recap) 🟡

### What is it?
Checking input data before the service runs: FluentValidation rules in this project.

### Why is it used?
Reject bad input with a clear 400, not a confusing service error.

### Interview Answer
"API validation checks the incoming DTO before business logic. I use FluentValidation
rules — `RuleFor(x => x.Email).EmailAddress()`, `MinimumLength(8)`. Automatic
`[ApiController]` model validation returns 400, and I customized the response factory
in Program.cs to wrap the errors in the standard `ApiResponse` envelope."

### LearnPath Example
`backend/Validators/Auth/RegisterRequestValidator.cs`, `CreateLearningPathValidator.cs`,
`CreateClassroomValidator.cs`. Registered via `AddValidatorsFromAssemblyContaining<T>`
and the custom `InvalidModelStateResponseFactory` from Phases 3-4.

### Remember
- Validate at the boundary with FluentValidation → consistent 400 JSON.

---

## 20. API error handling 🔥

### What is it?
A consistent way to return errors: status code + structured JSON message, centralised
in the exception middleware.

### Why is it used?
Clients need to distinguish error types and show the right UI (bad request vs
forbidden vs not found).

### Interview Answer
"Error handling means every failure returns a useful status code and a structured
JSON message. My `ExceptionMiddleware` converts typed exceptions into status codes —
400 for bad input, 404 for missing, 403 for blocked, 409 for conflicts — and always
wraps the message in `ApiResponse.Fail(...)`. Plus validation failures return the
same envelope. The frontend can read `success: false` + `message` uniformly."

### LearnPath Example
`backend/Middleware/ExceptionMiddleware.cs` + `ApiResponse<object>.Fail(...)` +
controller-level try/catch for special cases (login failure → 401).

### Common Mistake
- Returning raw exception text or a bare 500 — clients can't act on it.

### Remember
- One error format + typed status codes + safe messages.

---

## 21. Swagger (recap) 🟡

### What is it?
Auto-generated OpenAPI docs + interactive UI where you can call endpoints with the
JWT token (dev only).

### Why is it used?
Documentation, quick testing, and a machine-readable contract.

### Interview Answer
"Swagger documents the API automatically from the code — every endpoint's verb,
route, params, and responses. The UI lets me click any endpoint and call it, and I
configured a Bearer security scheme so I can paste the login token and test protected
endpoints. I enable it only in Development."

### LearnPath Example
`Program.cs` Swagger setup with Bearer security definition + `SwaggerConfig.cs`.
`[ProducesResponseType]` attributes in `CommunityController` enrich the docs.

### Remember
- Swagger = dev docs + test UI. Bearer token configured. Dev-only.

---

## 22. API versioning (basic) ⚪

### What is it?
Letting different API versions coexist (`/api/v1/...`). Common approaches: URL
versioning, header/query versioning.

### Why is it used?
Changing the API contract without breaking existing clients.

### Interview Answer
"API versioning allows the API to evolve without breaking old clients. A common way
is URL versioning — every route in my project starts with `api/v1/...`. Future
breaking changes can live under `api/v2` while v1 keeps serving existing users."

### LearnPath Example
All controllers use `[Route("api/v1/...")]` — the `v1` is the current version. The
project doesn't use the ASP.NET versioning package (no `ApiVersion` attributes) —
good to mention honestly.

### Follow-Up Questions
Q: Why version at all?
A: Breaking changes (renamed fields, removed endpoints) must not break old clients.

### Remember
- v1 in the URL = versioning. Families: URL, query, header.

---

## 23. REST API best practices 🔥

### What is it?
Agreed conventions: resource nouns, correct verbs, plural names, status codes, JSON,
camelCase, pagination, idempotent PUT/DELETE, validation, consistent errors.

### Why is it used?
Predictable, self-documenting, easy to consume and scale.

### Interview Answer
"The practices I follow: model resources as nouns and keep them plural
(`/paths`, `/users`); use the right verb per action; return proper status codes;
always accept/return JSON; return a consistent envelope; validate input at the
boundary; paginate list endpoints; and keep the API stateless via tokens. LearnPath
follows these — resources like `paths`, `classrooms`, `certificates` with consistent
`ApiResponse` JSON."

### LearnPath Example
`LearningPathController` — resources + verbs + 201/200/400/403/404. `ApiResponse<T>`
envelope everywhere. Paginated lists in `AdminController`/`CommunityController`.

### Common Mistake
- Making verbs part of URLs (`/getUser`) — use nouns + verbs.
- Returning entities directly instead of DTOs.

### Remember
- Nouns + verbs + status codes + JSON + pagination + consistent errors.

---

## 24. Authentication vs authorization 🔥

### What is it?
Authentication = WHO are you? (identity proof via credentials/token). Authorization =
WHAT can you do? (permissions/roles).

### Why is it used?
Security: prove identity, then enforce access.

### Interview Answer
"Authentication answers who you are — proving identity with the JWT. Authorization
answers what you're allowed to do — the roles and permissions attached to that
identity. In ASP.NET Core the `UseAuthentication` middleware validates the token,
then `UseAuthorization` + `[Authorize]`/roles enforce access. Login is
authentication; locking admin endpoints to the Admin role is authorization."

### LearnPath Example
Login/register (authentication) → issues JWT. `[Authorize]` (must be logged in),
`[Authorize(Roles = "Admin")]` (admin only), policies `AdminOnly`/`InstructorOrAdmin`
(authorization). Deep dive in Phase 7.

### Common Mistake
- Using the terms interchangeably — this is a favorite gotcha question.

### Remember
- AuthN = who. AuthZ = allowed to do what.

---

## 25. CORS 🟡

### What is it?
Cross-Origin Resource Sharing — browser rules saying which origins (frontend domain)
may call the API.

### Why is it used?
Browsers block cross-origin calls by default (security); CORS lets the API allow
specific trusted origins.

### Interview Answer
"CORS controls which origins a browser may call from. Browsers block cross-origin
requests by default, so the API whitelists the frontend's origin. I configured a
`LearnPathCors` policy in Program.cs that allows the configured origins, any headers,
any methods, and credentials, and applies it with `app.UseCors`. Development allows
localhost:5173; production allows the real frontend domain."

### Simple Example
```csharp
builder.Services.AddCors(o => o.AddPolicy("LearnPathCors", p => p
    .WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(","))
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
app.UseCors("LearnPathCors");
```

### LearnPath Example
`Program.cs:158-169` — policy `LearnPathCors`; origins from config
(`http://localhost:5173` in dev). Middleware order places it before authentication.

### Common Mistake
- `AllowAnyOrigin()` in production — a security hole; whitelist real domains.

### Remember
- CORS = browser's cross-origin gate. Whitelist origins in config; dev localhost vs
prod domain.

---

## Final Phase 5 "remember by heart" checklist

1. Resource nouns + verbs (GET read / POST create / PUT replace / PATCH partial /
   DELETE remove) + status codes 2xx/4xx/5xx.
2. 401=not logged in, 403=no permission, 404=missing, 400=bad input, 409=conflict.
3. Body→DTO→validate→service→`ApiResponse<T>` JSON.
4. Route `{id:int}` vs query `?filter=` vs body JSON — know all three input sources.
5. Consistent error envelope + global middleware + Swagger (dev) + CORS whitelist.
6. Authentication (who) vs authorization (what) — know the difference by heart.

Next: `06_Entity_Framework_Core.md` — say "Proceed to next phase" when ready.