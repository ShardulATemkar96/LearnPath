# Phase 10 — Mock Technical Interview (LearnPath Prep)

Two simulated rounds. Do NOT read the answer key until after you answer each question
out loud (or in writing). Say your answer, then check.

---

# ROUND 1 — C#, OOP, .NET, ASP.NET Core, Web API

**Q1.** What is the difference between a value type and a reference type in C#?

Expected knowledge: Where data lives, copy vs share behaviour, examples of each.

---

**Q2.** Explain the four pillars of OOP. Give a LearnPath example for each.

Expected knowledge: Encapsulation, Inheritance, Polymorphism, Abstraction — real
examples, not just definitions.

---

**Q3.** Interface vs abstract class. When do you choose which?

Expected knowledge: Pure contract vs shared base, multiple interface inheritance,
concrete LearnPath instances.

---

**Q4.** Overloading vs overriding — what's the difference?

Expected knowledge: Compile-time vs runtime polymorphism, virtual/override roles.

---

**Q5.** What is `static`? Can a static method access instance members?

Expected knowledge: Type-level vs instance-level, ApiResponse static factory.

---

**Q6.** What is the difference between `FirstOrDefault`, `SingleOrDefault`, and
`Any`? When would each be used?

Expected knowledge: Throw vs null semantics, uniqueness, short-circuiting.

---

**Q7.** How does `async`/`await` work and why does an ASP.NET Core API use it?

Expected knowledge: Non-blocking I/O, thread pool, Task, DB calls.

---

**Q8.** What is IEnumerable and why does LINQ matter?

Expected knowledge: Loopable contract, lazy execution, materialization with ToList.

---

**Q9.** Explain the ASP.NET Core request pipeline. Why does middleware order matter?

Expected knowledge: Layered middleware, custom middleware in LearnPath, precise order.

---

**Q10.** What does `[ApiController]` give you? What is model binding?

Expected knowledge: Auto-validation, attribute routing assumptions, binding
FromBody/FromQuery/route.

---

**Q11.** How does Dependency Injection work in ASP.NET Core? What are the three
lifetimes and when do you use each?

Expected knowledge: Program.cs registration, constructor injection, Scoped/Singleton/
Transient, DbContext scoped.

---

**Q12.** Explain the differences between GET, POST, PUT, PATCH, and DELETE with
status codes.

Expected knowledge: Read/create/replace/partial/delete + 200/201/400/404 semantics.

---

**Q13.** How does configuration work? Where do secrets live?

Expected knowledge: appsettings.{Env}.json layering, env vars, IOptions.

---

# ROUND 2 — EF Core, JWT, REST, Architecture, Scenarios

**Q1.** What is EF Core and what problem does an ORM solve?

Expected knowledge: Entity↔table, LINQ→SQL, productivity over raw SQL.

---

**Q2.** Describe how relationships are configured in LearnPath. FK delete behavior?

Expected knowledge: Fluent API, one-to-many, many-to-many join (UserClassroom),
Restrict vs Cascade.

---

**Q3.** Migrations: why and what's your workflow?

Expected knowledge: Up/Down, `dotnet ef`, `MigrateAsync()` in Program.cs.

---

**Q4.** What is the difference between tracked queries and `AsNoTracking`? Where did
you use it?

Expected knowledge: Change tracking cost, read-only queries, AuditLog/Community
repos.

---

**Q5.** What is `Include` and what problem does it prevent?

Expected knowledge: Eager loading, N+1, ThenInclude deep graphs.

---

**Q6.** Walk me through your JWT flow from login to authorized request.

Expected knowledge: Login → claims → signed token → header → middleware validates →
claims read → refresh rotation.

---

**Q7.** What is the difference between authentication and authorization? Give a
LearnPath example of each.

Expected knowledge: who vs what; login vs [Authorize(Roles="Admin")].

---

**Q8.** Explain the full path of a request from React to the database and back.

Expected knowledge: Middleware → controller → DTO+validation → service → EF → SQL →
DTO → ApiResponse JSON.

---

**Q9.** Why DTOs instead of returning entities directly?

Expected knowledge: Security, contract stability, decoupling from DB.

---

**Q10.** How do you handle exceptions globally? Why not a try/catch in every method?

Expected knowledge: ExceptionMiddleware, type→status mapping, ApiResponse envelope.

---

## Practical scenario questions

**S1.** A student reports they can access an instructor-only endpoint. How do you
debug it?

Expected knowledge: Check controller has [Authorize(Roles=...)], principal has role
claim, token not stale, policy applied before endpoint, service-level ownership
check as fallback.

**S2.** An admin reports deleting a learning path fails with a foreign-key error.
What do you check?

Expected knowledge: Dependent rows (classrooms, certificates, modules, children);
FK delete behavior; the explicit transaction path in DeleteAsync; ExceptionMiddleware
mapping.

**S3.** Users say the API "freezes" during high traffic. List the top three things you
would investigate first.

Expected knowledge: Blocking calls (.Result/.Wait) → deadlock; N+1/queries hitting
DB per request; missing indexes; DbContext misuse; rate limiter too tight.

**S4.** You need to change the password rule (min 8 → min 10). Only deployments
allowed, no manual DB edits.

Expected knowledge: Change the rule in code (PasswordOptions in Program.cs) — no
migration needed for validation rules; existing users unaffected; re-publish.

**S5.** Your JWT secret leaked into an internal repository. What do you do?

Expected knowledge: Rotate secret (env var), all tokens become invalid (force
re-login), remove from history/repo, avoid future commits via .gitignore/.env.

---

---

# ———— ANSWER KEY ————

> Check each after answering. Keep answers in your own words but hit the key points.

---

## ROUND 1 — Answer Key

**A1.** Value types (int, bool, struct, enum) store data directly on the stack and are
copied on assignment. Reference types (class, string, array) store an address to a
heap object — assignment shares the same object, so changes affect the original.
`string` is a reference type but immutable.

**A2.**
- Encapsulation: hidden private state, exposed public API — `private readonly`
  dependencies in services; `ApplicationDbContext.GuardAuditLogImmutability()` hides
  audit protection.
- Inheritance: `User : IdentityUser`, `ApplicationDbContext : IdentityDbContext<User>`.
- Polymorphism: interface-based — controllers depend on `IAuthService`, real
  `AuthService` runs at runtime; exception-type switch in `ExceptionMiddleware`.
- Abstraction: `IAuthService`, `IUserService` contracts hide implementation.

**A3.** Interface = pure contract, no implementation, a class can implement many.
Abstract class = base with possible implemented members, single inheritance.
Interfaces for capabilities and decoupling; abstract classes for shared code among
closely related classes. LearnPath: `IAuthService` (interface) vs FluentValidation's
`AbstractValidator<T>` (abstract, inherited by validators).

**A4.** Overloading = same name, different parameters, chosen at compile time
(return type can't differentiate). Overriding = same signature, body replaced via
`override` over a `virtual` base, resolved at runtime. Example: `ApplicationDbContext.SaveChanges`
overrides EF's.

**A5.** Static members belong to the type, not instances — shared and called via class
name. Static methods cannot access instance members (no object exists). Examples:
`ApiResponse<T>.Ok(...)`, `DagValidator`.

**A6.** `First` throws if empty; `FirstOrDefault` returns null. `Single` throws for 0
or >1 matches; `SingleOrDefault` tolerates 0. `Any` returns bool and short-circuits —
it answers "exists?" faster than `Count() > 0`. Pattern: `FirstOrDefaultAsync(...) ??
throw new KeyNotFoundException(...)`.

**A7.** At an `await`, the method yields and the thread returns to the pool; when the
I/O completes, execution resumes. This lets a server handle thousands of requests
with few threads instead of blocking each on a thread. All DB/HTTP in LearnPath is
async (`ToListAsync`, `FirstOrDefaultAsync`).

**A8.** `IEnumerable<T>` is the contract for anything for-each-able; LINQ queries are
lazy (run on iteration, not definition). `ToList()` materializes. With EF the query
runs in SQL when enumerated.

**A9.** Each request flows through middleware in registration order. Verified LearnPath
pipeline: Exception → Logging → RateLimit → (dev: Swagger) → CORS → Authentication →
Authorization → Controllers. Order matters: exceptions first to catch everything,
authentication before authorization.

**A10.** `[ApiController]` enables automatic model validation (400 on invalid),
assumes binding rules, improves routing. Model binding builds method parameters from
the request: `[FromBody]` JSON, `[FromQuery]`, route `{...}`, headers.

**A11.** DI registers type→implementation in Program.cs and constructs the whole
graph. Transient = new each ask; Scoped = per HTTP request (DbContext); Singleton =
whole app (JwtTokenGenerator). DbContext is scoped to keep one change-tracked unit of
work per request.

**A12.** GET reads (200), POST creates (201), PUT full replace (idempotent), PATCH
partial update, DELETE removes — 400 bad input, 401 not authenticated, 403 no
permission, 404 missing, 409 conflict. Verified across LearningPathController,
QuizController (PATCH publish/archive).

**A13.** Configuration layers: appsettings.json → appsettings.{Env}.json → user
secrets → env vars → command line, later wins. Secrets (JWT secret, DB connection)
come from `.env`/env vars via `AddInMemoryCollection`, never committed. Typed access
via `Configure<T>` + `IOptions<T>`.

---

## ROUND 2 — Answer Key

**A1.** EF Core maps entities to tables, LINQ to SQL, SaveChanges to transactional
statements, migrations to schema. ORM value: type safety, productivity, no hand SQL;
trade-off is the abstraction/generated SQL.

**A2.** Relationships via Fluent API classes implementing `IEntityTypeConfiguration<T>`.
- One-to-many: `LearningPath` ↔ `Module` (FK `LearningPathId`).
- Many-to-many: Users ↔ Classrooms through `UserClassroom` join entity (composite
  key, extra `Role` column, Cascade).
- Self many-to-many: `ModuleDependency` (composite key, Restrict).
- DeleteBehavior chosen per relation: Restrict for protected data (CreatedBy, deps),
  Cascade for owned children.

**A3.** Migrations are Up/Down schema version files. Workflow: change model →
`dotnet ef migrations add Name` → `dotnet ef database update` (or script for prod).
LearnPath: migrations generated at build/dev and `MigrateAsync()` runs pending ones at
startup, followed by role/admin seeding.

**A4.** Tracked queries cost memory and watch changes to generate UPDATEs;
`AsNoTracking` skips that for read-only data. Used in `AuditLogService`,
`CommunityRepository`, and read paths — they never update those entities.

**A5.** `Include`/`ThenInclude` eager-load related data with JOINs in ONE query. It
prevents N+1 — firing one query per parent row. `GetByIdAsync` loads modules →
resources/objectives/tags/dependencies/progress this way.

**A6.** Login: find user → `CheckPasswordAsync` → status checks → read roles →
`JwtTokenGenerator` signs HS256 token (claims: sub, email, name, roles; exp 60 min) +
64-byte refresh token (7 days, stored). Requests: `Authorization: Bearer <token>` →
`UseAuthentication` validates signature/issuer/audience/lifetime → builds `User` →
`UseAuthorization` honors `[Authorize]`/roles → controllers read id via
`ClaimTypes.NameIdentifier`. Expired → `auth/refresh` revokes old refresh and issues
a new pair; logout revokes all.

**A7.** Authentication = verifying who (login, JWT). Authorization = what you may do
(roles). LearnPath: login/register are authentication; `[Authorize(Roles = "Admin")]`
and `AdminOnly`/`InstructorOrAdmin` policies are authorization.

**A8.** React → middleware (Exception/Logging/RateLimit/CORS/Authn/Authz) → controller
binds and validates DTO → service applies rules (ownership, prerequisites, audit) →
DbContext/repository executes LINQ→SQL on SQL Server → entities materialized →
mapped to DTO (AutoMapper/manual) → `ApiResponse<T>` JSON → middleware unwinds →
React.

**A9.** DTOs keep the API contract stable and safe: entities mirror the DB and could
leak internals or couple clients to schema changes. Request/response shapes
(LoginRequestDto/AuthResponseDto) exist per feature; AutoMapper maps between them.

**A10.** Services throw typed exceptions; `ExceptionMiddleware` (registered first)
logs them, maps type→status (400/403/404/409/500), and returns a safe `ApiResponse`
JSON. Local catches only for special cases (login 401). Validation failures never
reach services. One global handler beats scattered try/catch.

---

## SCENARIO ANSWER KEY

**S1.** Check: does the endpoint actually have `[Authorize(Roles = ...)]`? Is the
token fresh (role claim updated after a role change — old tokens carry old roles)?
Is `UseAuthentication` before `UseAuthorization`? Add service-level ownership checks as
a second layer (like `GetOwnedPathAsync`).

**S2.** Look for dependent rows: classrooms, certificates, modules and their children
(resources/objectives/tags/dependencies/progress/attempts). FK behavior is Restrict
on protected relations, so deletion must remove children first — exactly what
`DeleteAsync` does inside an explicit transaction in FK-safe order. Confirm the
execution-strategy + transaction pattern and that ExceptionMiddleware returns 409 for
FK violations with a clear message.

**S3.** (1) Any synchronous blocking (`.Result`/`.Wait`) on async — partial or total
request pile-up. (2) N+1 or unmaterialized queries hammering SQL. (3) Missing indexes
/ oversized tracking. Also: rate limiter config and DbContext lifetime misuse.

**S4.** Change `options.Password.RequiredLength` (and related rules) in
`Program.cs`'s `AddIdentity` block, rebuild/publish only — validation rules are
runtime config, no migration needed; existing users keep their hashes.

**S5.** Rotate the JWT secret immediately (env var) → all old tokens fail validation,
forcing re-login. Remove the secret from the repo/commit history, and guard it going
forward (`.env` gitignored, secrets excluded from source control).

---

## Before you walk in — final confidence checklist

- [ ] Can I explain the request flow diagram in 60 seconds?
- [ ] JWT flow: login → header → middleware → claims → refresh rotation?
- [ ] The four OOP pillars with LearnPath examples?
- [ ] EF: Include, AsNoTracking, migrations, relationships, delete transaction?
- [ ] DI lifetimes + why DbContext is scoped?
- [ ] authN vs authZ, 401 vs 403, PUT vs PATCH?
- [ ] Global error handling story?
- [ ] Can I answer "why services / why DTOs / why EF Core" without notes?

Good luck — you've got the whole LearnPath stack mapped out.