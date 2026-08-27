# Phase 9 — Rapid Interview Revision (LearnPath Preparasyon)

Read this 30 minutes before the interview. For each concept: meaning → answer →
one follow-up. LearnPath touches marked **[LP]** are verified from source.

---

## 🔥 MUST KNOW

### Value types vs reference types
Meaning: value types hold the value directly (copied on assignment); reference types
hold an address to the object (shared).

Answer: "Value types store their data directly and are copied when assigned; reference
types store an address and share the same object. `int`, `bool`, `struct` are value
types; `class`, `string`, `array` are reference types."

Follow-up: What about `string`? → It is a reference type, but immutable.

### Classes vs objects
Meaning: class = blueprint; object = `new` instance.

Answer: "A class is a blueprint of data and behavior; an object is an instance created
with `new`. In LearnPath, `Module` is the class; each row loaded from the DB becomes
one object."

Follow-up: Class without constructor? → Compiler generates a default parameterless one.

### Encapsulation
Meaning: hiding internal state behind public members.

Answer: "Encapsulation hides internals with private fields and exposes only what's
needed publicly, so outside code can't corrupt state. In services, dependencies are
`private readonly` fields and only public methods are callable."

Follow-up: How implemented? → Access modifiers (`private`, `public`, `protected`).

### Inheritance
Meaning: a class reuses and extends a base class (`:`).

Answer: "Inheritance lets a class reuse members of a parent. C# allows single class
inheritance. `User : IdentityUser` gets all Identity's auth features and adds custom
fields like `FirstName`."

Follow-up: When not to use it? → For "has-a" relationships, use composition.

### Polymorphism
Meaning: one name, many behaviors — overriding + interfaces.

Answer: "Polymorphism means the same call behaves differently by actual type, via
`virtual`/`override` or interfaces. My controllers depend on `IAuthService`; the real
`AuthService` runs at runtime."

Follow-up: Two types? → Compile-time (overloading) and runtime (overriding).

### Abstraction + Interface
Meaning: interface = pure contract (no code), many per class.

Answer: "Abstraction hides how and exposes what. An interface is a contract of
methods with no implementation; classes implement it, and I inject interfaces so code
is decoupled and testable — `IAuthService`, `IUserService`."

Follow-up: Interface with state? → No; signatures/properties only, no implementation.

### Abstract class
Meaning: can't be instantiated; may share real code with subclasses.

Answer: "An abstract class can't be `new`'d; it's meant for inheritance and can mix
implemented methods with abstract ones. I don't define one in LearnPath, but my
FluentValidation validators inherit `AbstractValidator<T>`."

Follow-up: Abstract vs interface? → Interface = pure contract, many; abstract =
shared base with possible code, single.

### Method overloading vs overriding
Meaning: overloading = same name, different params (compile-time); overriding =
re-implement virtual base method (runtime).

Answer: "Overloading changes parameters and is resolved at compile time. Overriding
replaces a `virtual` base method's body with `override` and is resolved at runtime —
like `ApplicationDbContext.SaveChanges` overriding EF's."

Follow-up: Can return type differentiate overloads? → No, only parameters.

### static
Meaning: belongs to the type, not to an instance.

Answer: "Static members belong to the class and are shared; I call them via the type
name. `ApiResponse<T>.Ok(...)` is a static factory, and `DagValidator` is a static
class."

Follow-up: Static method access instance fields? → No — no instance exists.

### const vs readonly
Meaning: const = compile-time literal; readonly = set once at runtime.

Answer: "const is a compile-time constant so it must be a literal. readonly is set in
the constructor or initializer once and never changed. RateLimitingMiddleware has
`private const int MAX_REQUESTS = 50` and `private readonly RequestDelegate _next`."

Follow-up: When const invalid? → Anything not a compile-time literal (`new`,
`DateTime`) → use `static readonly`.

### Exception handling / try-catch-finally
Meaning: exceptions = runtime errors; handle once, respond gracefully.

Answer: "Exceptions are thrown on failures. I throw specific types and handle them in
one place: `ExceptionMiddleware` maps them to status codes, controllers catch special
cases, and `finally` guarantees cleanup (though I prefer `using`/`await using`)."

Follow-up: finally without catch? → Valid; cleanup still runs.

### Collections / List vs Dictionary vs HashSet
Meaning: List = ordered many; Dictionary = key→value; HashSet = unique members.

Answer: "`List` is an ordered dynamic array, `Dictionary` gives fast key-based lookup,
and `HashSet` gives fast uniqueness checks. I use `.ToHashSet()` of completed module
ids for O(1) Contains checks."

Follow-up: Lookup speed? → HashSet/Dictionary O(1); List O(n).

### IEnumerable / lazy LINQ
Meaning: anything foreach-able; lazy by default.

Answer: "`IEnumerable<T>` is the contract for anything loopable. LINQ is lazy — the
query runs when iterated, not when defined; `ToList()` materializes it. In EF Core
the query translates to SQL when enumerated."

Follow-up: Returning IEnumerable twice? → Each iteration may re-run it; materialize
once.

### LINQ core verbs
Meaning: Where filter, Select map, OrderBy sort, GroupBy group.

Answer: "Where filters, Select transforms, OrderBy sorts, and GroupBy groups. Keep
filters before materialization so they execute in SQL, not in memory. `Any` for
existence, `Count` for how many."

Follow-up: Any vs Count()>0? → `Any` short-circuits; faster for existence.

### First vs FirstOrDefault / Single vs SingleOrDefault
Meaning: First/FirstOrDefault → first match (throw vs null). Single → exactly one.

Answer: "First throws if empty; FirstOrDefault returns null. Single demands exactly
one and throws otherwise. My services use `FirstOrDefaultAsync(...) ?? throw
KeyNotFoundException` — explicitly choosing the null-then-throw pattern."

Follow-up: When use Single? → When data must be unique (e.g. 0 or 1+ are bugs).

### async/await + Task
Meaning: async = non-blocking I/O; Task = promised result.

Answer: "async/await pauses at I/O without blocking a thread — the thread returns to
the pool and resumes later. `Task<T>` carries the result. All my service methods are
async using `ToListAsync`, `FirstOrDefaultAsync`."

Follow-up: async for CPU work? → No; async is for I/O-bound. CPU-bound stays sync.

### Generics
Meaning: code written with type `T`, made concrete on use.

Answer: "Generics write one class for any type with compile-time safety.
`ApiResponse<T>` wraps any response, and `GenericRepository<T>` serves any entity."

Follow-up: What are constraints? → `where T : class`, `new()`, etc. limit allowed
types.

### .NET vs .NET Framework
Meaning: modern cross-platform .NET vs old Windows-only Framework.

Answer: "Modern .NET is the open-source, cross-platform successor (5+, LearnPath is
`net10.0`). .NET Framework is the old Windows-only line for legacy apps."

Follow-up: Same runtime? → No; framework apps don't run on modern .NET.

### CLR + managed code
Meaning: runtime that JIT-compiles IL to machine code; GC manages memory.

Answer: "The CLR is the runtime: it JIT-compiles IL to native code and manages memory
via the garbage collector. C# running on it is managed code — memory handled
automatically; I still dispose resources like DB connections with `using`."

Follow-up: GC.Collect? → Rarely needed; the runtime decides when to collect.

### NuGet
Meaning: .NET package manager.

Answer: "NuGet provides reusable libraries referenced in the csproj — my project
uses EF Core, JwtBearer, FluentValidation, Swashbuckle, AutoMapper."

Follow-up: How to add? → `dotnet add package <Name>`.

### Dependency Injection + lifetimes
Meaning: objects receive dependencies from the container; Transient/Scoped/Singleton.

Answer: "DI gives classes their dependencies via constructors instead of `new`.
Transient = new each request to container, Scoped = per HTTP request, Singleton = app
lifetime. DbContext is scoped; `JwtTokenGenerator` is singleton."

Follow-up: Why is DbContext scoped? → One unit of work per request; no cross-request
state sharing.

### Configuration + appsettings + Options
Meaning: layered key-value settings (appsettings.{Env}.json, env vars) bound to typed
classes.

Answer: "Configuration reads appsettings.json, environment-specific files, and
environment variables — later wins. JWT secret and connection string come from
`.env`/env vars. `Configure<JwtSettings>(GetSection(...))` + `IOptions<JwtSettings>`
bind them typed."

Follow-up: Precedence? → Command line > env vars > user secrets > appsettings.{Env}
> appsettings.json.

### Middleware + request pipeline
Meaning: ordered chain around every request (like onions).

Answer: "Middleware layers wrap every request. My pipeline: Exception → Logging →
RateLimit → CORS → Authentication → Authorization → Controllers. Middleware can
inspect, short-circuit, and act on the response."

Follow-up: Order matters? → Yes — exception first, auth before authz.

### Controllers
Meaning: HTTP entry points, thin, delegate to services.

Answer: "Controllers are thin: `[ApiController]` + attribute routing, model binding
from body/query/route, automatic validation, return `IActionResult`. They call
services and wrap results in `ApiResponse<T>`."

Follow-up: ControllerBase vs Controller? → API uses ControllerBase (no views).

### Model binding + validation
Meaning: bind request → typed params; validate before services.

Answer: "Model binding maps JSON body (`[FromBody]`), query (`[FromQuery]`), and route
values to parameters. Validation via FluentValidation (`RuleFor(...).NotEmpty()`)
runs automatically and returns 400 with my `ApiResponse` envelope."

Follow-up: Where does auto-400 come from? → `[ApiController]` + the custom
`InvalidModelStateResponseFactory`.

### Web API + REST + HTTP verbs
Meaning: HTTP + JSON backend using resources + verbs.

Answer: "My API exposes resources (paths, users) via REST conventions: GET reads,
POST creates, PUT replaces, PATCH partial-updates, DELETE removes — with proper status
codes and JSON."

Follow-up: PUT vs PATCH? → PUT = full replace; PATCH = change some fields.

### Status codes
Meaning: 2xx success, 4xx client error, 5xx server error.

Answer: "I use 200 OK, 201 Created, 400 bad input, 401 unauthenticated, 403 forbidden,
404 not found, 409 conflict, 429 rate limited, 500 server error."

Follow-up: 401 vs 403? → 401 identify yourself; 403 identified but not allowed.

### Authentication vs authorization
Meaning: authN = who; authZ = what can you do.

Answer: "Authentication proves identity — via the JWT issued at login. Authorization
enforces permissions — `[Authorize]` and roles like Admin. Pipeline order is
`UseAuthentication()` then `UseAuthorization()`."

Follow-up: What gates admin? → `[Authorize(Roles = "Admin")]` + policies.

### JWT
Meaning: signed, stateless token `header.payload.signature` carrying claims.

Answer: "JWT is a signed token with a header (HS256), payload (claims like user id,
email, roles, exp), and signature. HMAC-signed with the secret, so it can't be
tampered with; payload is base64 (readable), not encrypted."

Follow-up: JWT encrypted? → No — signed only; don't put secrets in it.

### Access vs refresh token
Meaning: access = short-lived JWT per request; refresh = long, stored, revocable.

Answer: "Access token (60 min) goes in the Authorization header for every call. The
refresh token (7 days) is stored in DB and only used to get a new access token;
`AuthService.RefreshTokenAsync` validates it, revokes it, and issues a fresh pair."

Follow-up: Rotation? → Yes — each refresh revokes the used token and issues a new one.

### EF Core / ORM
Meaning: maps C# entities ↔ tables, LINQ → SQL.

Answer: "EF Core is an ORM: entities are tables, LINQ is SQL, SaveChanges generates
INSERT/UPDATE/DELETE in one transaction. Schema evolves via migrations applied on
startup (`MigrateAsync`)."

Follow-up: Why not raw SQL? → Type safety, productivity, auto schema sync.

### DbContext + DbSet + Entity
Meaning: session/unit of work, table handle, row model.

Answer: "`ApplicationDbContext` is the session (scoped), DbSets are tables
(`context.Modules`), entities are rows with navigation properties for relationships."

Follow-up: Context lifetime? → Scoped — one per request.

### Relationships + FKs
Meaning: one-to-many (FK on child), many-to-many (join table), one-to-one.

Answer: "LearningPath 1→many Modules; Users and Classrooms many-to-many via the
`UserClassroom` join table; Module to ModuleQuiz is one-to-one. FKs define the links
and delete behavior — I use Restrict to protect owned data."

Follow-up: Cascade vs Restrict? → Cascade deletes children; Restrict blocks if
children exist.

### Fluent API + Migrations
Meaning: mapping config in C# classes; versioned schema Up/Down.

Answer: "Each entity has an `IEntityTypeConfiguration<T>` class (keys, lengths, FK
behavior) applied via `ApplyConfigurationsFromAssembly`. Migrations are Up/Down pairs
that sync the DB — `AddWithRetry`/`MigrateAsync`."

Follow-up: Annotation vs Fluent? → LearnPath uses Fluent to keep entities clean.

### SaveChanges + Add/Update/Delete + tracking
Meaning: one commit point; tracking detects edits.

Answer: "`SaveChangesAsync` writes all tracked changes in one transaction — Add →
INSERT, edits → UPDATE, Remove → DELETE. Tracked entities update automatically."

Follow-up: AsNoTracking? → For read-only queries; faster, no change tracking.

### Include / N+1
Meaning: eager load related data to avoid per-row queries.

Answer: "`Include`/`ThenInclude` load related data with JOINs in one query, avoiding
the N+1 trap. `GetByIdAsync` loads modules plus resources, objectives, tags this way."

Follow-up: What is N+1? → Loading children with a query per parent row.

---

## 🟡 SHOULD KNOW

### Constructors
Meaning: runs on creation; sets valid state / injects deps.

Answer: "The constructor runs automatically at `new` — in this project it receives
dependencies (constructor injection), e.g. `AuthController(IAuthService)`."

Follow-up: Default constructor? → Compiler adds one if none declared.

### Access modifiers
Meaning: visibility control (public/private/protected/internal).

Answer: "public is global, private is class-only, protected adds subclasses, internal
is assembly-wide. Services use `private readonly` dependencies and public methods."

Follow-up: protected vs internal? → protected = class + derived; internal = assembly.

### Properties + encapsulation
Meaning: field + get/set accessors with optional logic.

Answer: "Properties are fields with controlled access. Auto-properties `{ get; set; }`
— used by all entities/DTOs. Can include computed getters and validation."

Follow-up: Auto vs computed? → Auto stores; computed (`=>`) runs each read.

### sealed
Meaning: cannot be inherited.

Answer: "`sealed` blocks inheritance of a class. Not used in LearnPath (verified) —
but it's the tool when extensibility must stop."

Follow-up: When sealed? → When subclassing would be wrong; also enables JIT
optimizations.

### Nullable types
Meaning: `string?`, `int?`, `DateTime?` — may hold null.

Answer: "`int?`/`DateTime?` make value types nullable; `string?` enables compiler null
warnings. Optional entity fields like `ArchivedAt`, `AvatarUrl` use them."

Follow-up: Safe access? → `?.`, `??`, `HasValue`.

### string vs StringBuilder
Meaning: string immutable; StringBuilder = mutable buffer.

Answer: "Strings are immutable so concatenation copies. StringBuilder avoids this in
loops. LearnPath uses interpolation (`$"..."`) for short messages."

Follow-up: Rule of thumb? → Interpolation for few fixed strings; StringBuilder in
loops.

### ref / out / in
Meaning: ref = by ref r/w; out = by ref must-assign; in = read-only ref.

Answer: "out produces results (TryGetValue), ref modifies in place, in passes large
structs cheaply without mutation. `TryGetValue(key, out var v)` appears in
`DagValidator`."

Follow-up: out without init? → out doesn't require pre-initialization; ref does.

### Garbage collection
Meaning: automatic heap reclamation by generations.

Answer: "GC frees objects nothing references, in generations 0/1/2. Non-memory
resources (DB connections, transactions) are disposed via `using`/`await using`."

Follow-up: Force GC? → You can, but the runtime manages it.

### NuGet packages in LearnPath
Meaning: the csproj lists verified packages.

Answer: Versioned packages: EF Core SqlServer, Identity EF, JwtBearer, FluentValidation,
AutoMapper, Swashbuckle, DotNetEnv, Azure.Identity, PdfPig."

Follow-up: What does DotNetEnv do? → Loads `.env` variables for secrets.

### Options pattern
Meaning: typed config via Configure<T> + IOptions<T>.

Answer: "`Configure<JwtSettings>(GetSection("JwtSettings"))` then `IOptions<JwtSettings>`
inject the typed settings — `JwtTokenGenerator` reads Secret/Issuer/Audience/Expiry."

Follow-up: IOptions vs IOptionsSnapshot? → Snapshot reloads on change.

### Logging
Meaning: structured diagnostics via ILogger + levels.

Answer: "I inject `ILogger<T>` and log structured messages. Configured levels in
appsettings: Information default, EF Core SQL visible in Development. `LoggingMiddleware`
logs every request's method/path/status/duration."

Follow-up: Levels? → Trace..Error/Critical; config picks the floor.

### Swagger/OpenAPI
Meaning: auto-generated docs + test UI.

Answer: "Swagger generates OpenAPI docs and UI from the code. I configured the Bearer
security scheme to test protected endpoints with the JWT, enabled in Development
only."

Follow-up: Why dev-only? → Exposing the UI in production is an attack surface.

### Filters vs middleware
Meaning: filters wrap actions; middleware wraps the pipeline.

Answer: "Filters run around controller actions — `[Authorize]` is a filter. Middleware
runs for every request. LearnPath uses built-in filters and custom middleware."

Follow-up: Which runs first? → Middleware first; authorization filter inside the
request.

### Transactions
Meaning: multi-step DB ops all-or-nothing.

Answer: "A transaction ensures multiple operations succeed or fail together.
`LearningPathService.DeleteAsync` uses the execution strategy + explicit transaction,
deleting dependencies in FK-safe order before commit."

Follow-up: SaveChanges transactional? → Yes, by itself it runs in a transaction.

### Repository pattern
Meaning: data-access abstraction behind an interface.

Answer: "`IGenericRepository<T>` wraps CRUD; `CommunityRepository` is the specific one.
Services use it and also the DbContext directly for complex queries — a pragmatic
mix."

Follow-up: Is DbContext already a repository? → Yes — many skip the pattern; I use it
where it fits.

### Claims
Meaning: key-value facts inside the JWT.

Answer: "Claims are the token's data: sub (user id), email, first/last name, roles.
Controllers read them, e.g. `User.FindFirstValue(ClaimTypes.NameIdentifier)`."

Follow-up: Trust claims? → Only after signature validation (middleware does it).

### Password hashing
Meaning: store hashes, never plaintext.

Answer: "Identity's `UserManager` hashes passwords (PBKDF2) automatically on create
and compares on login. My password policy: min 8, upper + digit, lockout after 5
failed tries."

Follow-up: Can hashes be reversed? → Computationally no; that's the point.

### Token expiration
Meaning: exp claim + middleware lifetime validation.

Answer: "Access tokens expire in 60 min; `ValidateLifetime = true` with zero clock
skew rejects expired tokens. Refresh token 7 days. Expired access → refresh flow."

Follow-up: Why so short? → Limits damage if a token leaks.

### CORS
Meaning: browser origin gate for the API.

Answer: "CORS whitelists origins allowed to call the API — localhost:5173 in dev, the
real domain in prod. It is not authentication; the JWT still protects endpoints."

Follow-up: AllowAnyOrigin ok? → Not with credentials/production.

---

## ⚪ BASIC AWARENESS

### What is C#?
Meaning: modern statically-typed OOP language by Microsoft.

Answer: "C# is a statically-typed OOP language; types are checked at compile time. It
built the whole backend (`Program.cs` start, top-level statements)."

Follow-up: Compiled or interpreted? → Compiled to IL, then JIT to machine code.

### Variables & data types basics
Meaning: int/decimal/bool/string/DateTime; var = inferred.

Answer: "Value types, primitives, `decimal` for money, `var` is inferred not dynamic."

Follow-up: var vs dynamic? → var is compile-time inferred; dynamic defers to runtime.

### Events & delegates
Meaning: delegate = method as value; event = subscribe/publish.

Answer: "Delegates are typed function references (`Func`, `Action`); lambdas create
them. Events are constrained delegates for notifications. Not a custom pattern in
LearnPath — LINQ lambdas are the everyday use."

Follow-up: Lambda vs delegate? → Lambda is inline syntax that becomes one.

### REST vs SOAP
Meaning: style over HTTP+JSON vs XML protocol.

Answer: "REST is a style (resources, verbs); SOAP is a strict XML protocol. REST
won for modern web/mobile APIs."

Follow-up: SOAP left where? → Legacy enterprise integrations.

### API versioning
Meaning: URLs like `api/v1` let API evolve safely.

Answer: "Versioning keeps old clients working. LearnPath uses `v1` in the URL; I
didn't add the versioning package."

Follow-up: Why version? → Breaking changes must not break existing clients.

### API best practices
Meaning: nouns, verbs, status codes, JSON, pagination, consistent errors.

Answer: "I follow: plural resource nouns, correct verbs, proper status codes, JSON,
DTOs, pagination, and one `ApiResponse` shape for success and failure."

Follow-up: Verbs in URLs? → Anti-pattern; nouns + verbs together.

### Assemblies / DLL vs EXE
Meaning: compiled units; DLL = library, EXE = app.

Answer: "Assemblies are compiled DLL/EXE with IL + metadata. My API builds
`LearnPath.API.dll`; NuGet packages are DLLs too."

Follow-up: Entry point? → EXE has one; DLL doesn't.

### GroupBy / analytics usage
Meaning: grouping data for statistics.

Answer: "`GroupBy(p => p.Module.ContentType)` in `AnalyticsService` counts completions
per content type — LINQ grouping in EF."

Follow-up: Projection after GroupBy? → `Select(g => new { g.Key, Count = g.Count() })`.

### DTO role recap
Meaning: contract-shaped, entity-free data.

Answer: "DTOS keep entities internal: requests and responses never expose DB
internals, and the API shape is stable."

Follow-up: Who maps? → AutoMapper profiles (`AuthMappingProfile`).

---

## 10-second panic list (last look)

1. DI = give dependencies, don't create.
2. Scoped per request, Singleton app-wide, Transient every ask.
3. Interface = contract; abstract = partial blueprint.
4. override needs `virtual`. static = per type. sealed = no children. const = literal.
5. Where=filter, Select=map. Any=exists, Count=number. FirstOrDefault=maybe null.
6. async + await for DB/IO, never `.Result`.
7. EF: Include→JOIN, AsNoTracking→read-only, SaveChanges→commit, Migrations→schema.
8. JWT signed HS256, header.payload.signature, exp checked, access 60m / refresh 7d
   rotated.
9. Error = typed throw → ExceptionMiddleware → status + ApiResponse JSON.
10. Layers: Controller → Service → EF → SQL Server → DTO → JSON.

Next: `10_Mock_Technical_Interview.md` — say "Proceed to next phase" when ready.