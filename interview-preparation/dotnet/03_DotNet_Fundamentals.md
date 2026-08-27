# Phase 3 — .NET Fundamentals (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

All LearnPath references verified from source (`backend/Program.cs`, csproj,
appsettings files, middleware, services).

---

## 1. What is .NET? 🔥

### What is it?
.NET is a free, open-source, cross-platform *development platform* (runtime +
libraries + tools) from Microsoft for building apps. C# is the main language used
on it.

### Why is it used?
One platform for web, APIs, desktop, cloud, and games — with a huge standard library,
high performance, and the same code running on Windows, Linux, and macOS.

### Interview Answer
".NET is Microsoft's cross-platform development platform — it provides the runtime
that executes code, a large class library, and tools. It has several workloads:
ASP.NET Core for web APIs, Entity Framework Core for data, and .NET itself has the
runtime/CLR underneath. My backend is an ASP.NET Core app running on .NET — the csproj
targets `net10.0`."

### How it works
```text
C# source → compiler → IL (Intermediate Language) → CLR JIT-compiles → machine code
```

### LearnPath Example
`backend/LearnPath.API.csproj` line 3: `<TargetFramework>net10.0</TargetFramework>`.

### Common Mistake
- Saying ".NET is a language" — it's a platform; C# is the language.

### Follow-Up Questions
Q: What does "cross-platform" mean here?
A: Apps run on Windows, Linux, macOS; that's why it can be deployed in Docker (the
repo has `docker-compose.yml`).

### Remember
- .NET = platform/runtime. C# = language. One codebase, many OSes.

---

## 2. .NET vs .NET Framework 🔥

### What is it?
- **.NET Framework**: the old, Windows-only framework (versions 4.x), still used by
legacy apps.
- **.NET (Core / 5+)**: the modern, cross-platform, open-source successor, one
official version line from .NET 5 onward (5, 6, 7, 8, 9, 10...).

### Why is it used?
Modern .NET replaced Framework for new apps: faster, cross-platform, unified APIs,
ship-in-the-box runtime.

### Interview Answer
".NET Framework is the original Windows-only framework. Modern .NET is the
successor — open source, cross-platform, faster, and one unified platform starting at
.NET 5. New applications should use modern .NET; Framework is only for maintaining
legacy systems. LearnPath runs on modern .NET 10."

### Simple Example
```csharp
// modern .NET csproj
<TargetFramework>net10.0</TargetFramework>
```

### LearnPath Example
`backend/LearnPath.API.csproj` targets `net10.0` — modern .NET, not Framework.

### Common Mistake
- Interchanging the names at an interview — ".NET" today means modern .NET (Core).

### Follow-Up Questions
Q: Are they compatible?
A: No — .NET Core/5+ apps do not run on the .NET Framework runtime and vice versa.

### Remember
- Framework = old Windows-only. Modern .NET = current, cross-platform. LearnPath:

---

## 3. CLR (Common Language Runtime) 🟡

### What is it?
The runtime engine of .NET: it loads assemblies, JIT-compiles IL to machine code,
manages memory (GC), security, and executes the app.

### Why is it used?
It provides the services every .NET app relies on automatically — you focus on code,
CLR handles execution and memory.

### Interview Answer
"The CLR is the heart of .NET — it executes managed code. It takes the compiled
Intermediate Language, compiles it to machine code when needed (JIT), and manages
memory through the garbage collector. Every .NET app runs on the CLR."

### How it works
```text
IL in assembly → JIT compiles method-by-method on first call → native machine code
(cached for the process lifetime)
```

### LearnPath Example
Not visible in app code (it's infrastructure), but the compiled output (`bin/`)
contains IL that the CLR runs. Mention: "the CLR is invisible to me in code — it's
the runtime that makes `dotnet run` work."

### Common Mistake
- Calling the CLR the "compiler" — the compiler produces IL; the CLR executes it.

### Follow-Up Questions
Q: What is JIT?
A: Just-In-Time compilation — compiling IL to native code at runtime, method by
method, on first execution.

### Remember
- CLR = the runtime: JIT + GC + execution. Compiler makes IL, CLR runs it.

---

## 4. Managed code 🟡

### What is it?
Code that runs under the CLR's control — the runtime manages its memory (GC), type
safety, and safety checks. C#/VB.NET are managed languages. Unmanaged code (C/C++)
manages memory itself.

### Why is it used?
Managed code gives safe memory management and cross-language interop (all .NET
languages produce IL) at a small performance cost paid by the JIT.

### Interview Answer
"Managed code is code that runs on the CLR, which controls memory and execution.
The CLR's garbage collector frees objects for me automatically. C# code in my API is
managed code — that's why I rarely worry about memory leaks or manual deallocation."

### LearnPath Example
All the C# in `backend/` is managed code. The API interops with unmanaged resources
only for system calls (e.g. cryptography in `JwtTokenGenerator`) — otherwise purely
managed.

### Common Mistake
- Not knowing what "unmanaged" means — resources like file handles, DB connections
outside the GC need `using`/explicit release.

### Follow-Up Questions
Q: What is unmanaged resource?
A: OS resources like file handles, network connections, DB connections — not managed
by GC; release with `Dispose`/`using`.

### Remember
- Managed = GC handles memory. Unmanaged = you dispose it (using).

---

## 5. Garbage collection 🟡

### What is it?
Automatic memory reclamation by the CLR: objects no longer referenced are freed
automatically, done in generations (Gen0/1/2).

### Why is it used?
Developers don't track memory manually — GC handles allocation/reclamation, preventing
leaks and double-free errors.

### Interview Answer
"The garbage collector automatically reclaims memory from objects nothing references
anymore. It groups objects by age into generations — short-lived ones (Gen0) are
collected most often, long-lived ones fewer times. For non-memory resources like
database connections I still dispose them explicitly with `using`."

### Simple Example
```csharp
using var rng = RandomNumberGenerator.Create();   // deterministic cleanup (not GC)
```

### LearnPath Example
`JwtTokenGenerator` uses `using var rng` for the crypto RNG; `LearningPathService`
uses `await using var transaction` for DB transactions. GC handles memory; `using`
handles resources.

### How it works
GC thread pauses threads (briefly), walks object references from roots, keeps objects
still reachable, frees the rest, compacts.

### Common Mistake
- Calling `GC.Collect()` hoping to optimize — the runtime manages it.

### Follow-Up Questions
Q: Does GC collect instantly when an object goes out of scope?
A: No — collection happens when the GC decides (Gen0 fill); scope exit just makes the
object unreachable.

### Remember
- GC = automatic memory. `using` for non-memory resources. Never force GC.

---

## 6. Assemblies ⚪

### What is it?
The compiled unit of .NET — a DLL or EXE containing IL, metadata (types), and a
manifest. The basic versioning/deployment unit.

### Why is it used?
It packages code into a deployable, reusable unit with version info and references.

### Interview Answer
"An assembly is a compiled unit of .NET — a DLL or EXE containing the IL and metadata
of the code. The build output of my project, `LearnPath.API.dll` plus its
dependencies, is the deployment unit."

### LearnPath Example
Build output of the API project is `LearnPath.API.dll` (and `LearnPath.API.exe` launcher
on Windows). NuGet packages are assemblies shipped for reuse.

### Common Mistake
- Confusing assembly with namespace — namespaces organize code inside assemblies.

### Remember
- Assembly = compiled unit (DLL/EXE) with IL + metadata.

---

## 7. DLL vs EXE ⚪

### What is it?
- **EXE**: executable — has an entry point (`Main`/top-level statements), can be run
directly.
- **DLL**: library — code to be used by apps, no runnable entry point.
Both are assemblies.

### Why is it used?
EXE = application; DLL = reusable library.

### Interview Answer
"EXE is an executable application with an entry point, DLL is a library of code that
an application loads. My API builds a library `LearnPath.API.dll` that the ASP.NET
runtime hosts (`dotnet run` / web host), while the NuGet packages I reference are
DLLs too."

### LearnPath Example
The web project creates `LearnPath.API.dll`. Referenced packages like
`Microsoft.EntityFrameworkCore.SqlServer` are DLLs.

### Common Mistake
- Saying DLL can be "double-clicked to run" — it's a library, not an app.

### Remember
- EXE = runnable app; DLL = reusable library. Both are assemblies.

---

## 8. NuGet 🔥

### What is it?
NuGet is .NET's package manager — a registry of libraries you install into a project
with a version, listed in the `.csproj`.

### Why is it used?
Reuse libraries instead of writing everything — EF Core, JWT, Swagger, AutoMapper
come as packages.

### Interview Answer
"NuGet is the package manager for .NET. I add packages in the csproj — each entry is
a package name and version — and the restore step downloads them. My project uses
packages like `Microsoft.EntityFrameworkCore.SqlServer`, `FluentValidation`,
`Swashbuckle.AspNetCore`, and `AutoMapper`."

### Simple Example
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.9" />
```

### LearnPath Example
`backend/LearnPath.API.csproj` — 10 package references, including JWT Bearer,
Identity EF, EF Tools, DotNetEnv, PdfPig.

### Common Mistake
- Confusing NuGet with npm (JS) — same idea: central package registry per ecosystem.

### Follow-Up Questions
Q: How do you add a package?
A: `dotnet add package <Name>` or editing the csproj then `dotnet restore`.

### Remember
- NuGet = .NET package library. `dotnet add package`.

---

## 9. Project structure 🟡

### What is it?
The folder layout of a .NET solution: solution (.sln) → projects (.csproj) →
folders by responsibility (Controllers, Services, Entities, DTOs, etc.)

### Why is it used?
A clear structure keeps the codebase maintainable and each layer responsible for one
thing.

### Interview Answer
"I organize by responsibility. LearnPath has one solution with a `backend` API project
and a `backend.Tests` test project. Inside the API: `Controllers` for endpoints,
`Services` + `Interfaces` for business logic, `Repositories` for data, `Entities` for
database models, `DTOs` for API payloads, `Configurations` for EF mappings,
`Middleware`, `Validators`, `Migrations` and `Data`."

### LearnPath Example
Actually verified folders under `backend/`:
`Controllers/` `Services/` `Interfaces/` `Repositories/` `Entities/` `DTOs/`
`Configurations/` `Middleware/` `Validators/` `Migrations/` `Data/` `Common/`
`Authentication/Jwt/` `Algorithms/` `Swagger/`.

### Common Mistake
- Not being able to explain why folders exist — each folder maps to a layer with a
clear responsibility (see Phase 8).

### Remember
- Controllers → Services → Repositories → Entities + DTOs + Migrations + Middleware.

---

## 10. Dependency Injection 🔥

### What is it?
A pattern where objects receive their dependencies (interfaces) from an external
container instead of creating them with `new`. ASP.NET Core has it built in.

### Why is it used?
Low coupling, easy testing (swap fake implementations), centralized object lifecycle
management.

### Interview Answer
"Dependency Injection is a technique where a class receives the objects it needs
from outside instead of creating them itself. It reduces coupling and makes the app
easier to test. ASP.NET Core has DI built in — in Program.cs I register services like
`AddScoped<IAuthService, AuthService>()`, and controllers receive them through their
constructors. I use this heavily in LearnPath."

### Where it appears in LearnPath
- Registration: `Program.cs` — `builder.Services.AddScoped<IAuthService, AuthService>()`
  etc.
- Consumption: `AuthController` constructor takes `IAuthService`; `AuthService`
  constructor takes `UserManager`, `JwtTokenGenerator`, `IMapper`, `ApplicationDbContext`,
  `IAuditLogService`.

### How it works
```text
Program.cs registers (type → implementation, lifetime)
   │
DI container on request → builds AuthController
   │
needs IAuthService → container creates AuthService (+ its own dependencies)
```

### Common Mistake
- Using `new` inside a controller to create a service — kills testability and
lifecycle.

### Follow-Up Questions
Q: What problem does DI solve?
A: Tight coupling + hard-to-test code: the class no longer decides how its
dependencies are built.

### Remember
- DI = receive dependencies from container. Controllers depend on interfaces;
   Program.cs wires them.

---

## 11. Service lifetimes (Singleton / Scoped / Transient) 🔥

### What is it?
How long the DI container reuses an object:
- **Transient** — new instance every time it is requested.
- **Scoped** — one instance per HTTP request (per scope).
- **Singleton** — one instance for the whole app lifetime (shared by all requests).

### Why is it used?
Choose based on state sharing + safety: singletons share state (must be thread-safe),
scoped matches a request, transient for stateless/cheap services.

### Interview Answer
"Transient means a new instance every time something asks for it. Scoped means one
instance per HTTP request — this is the default and safest for most services in an
API. Singleton means one instance for the entire app lifetime, shared by everyone.
The general rule: stateless services can be scoped; stateful/expensive wrappers might
be singleton; DbContext must be scoped per request."

### Simple Example
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();       // per request
builder.Services.AddSingleton<JwtTokenGenerator>();            // one for app
builder.Services.AddTransient<SomeCheapService>();             // per request-instance
```

### LearnPath Example
`Program.cs`:
`AddDbContext` (scoped by default — one `ApplicationDbContext` per request),
`AddScoped<IAuthService, AuthService>()` and all other services,
`AddSingleton<JwtTokenGenerator>()` (stateless JSON token generator).

### How it works
The container holds references
according to the lifetime; transient = always new, scoped = cached on the current
scope (request), singleton = cached on the root scope.

### Common Mistake
- Registering a scoped service (like DbContext) inside a singleton — the singleton
captures one context forever, causing stale data / connection issues.
- Using singleton for stateful, non-thread-safe services.

### Follow-Up Questions
Q: Why is DbContext scoped?
A: One request = one context — a single unit of work/change tracker, no sharing bugs
across requests.

### Remember
- Transient = new each time; Scoped = per request; Singleton = whole app. DbContext
is scoped.

---

## 12. Configuration 🔥

### What is it?
A key-value configuration system that reads from many sources: appsettings.json,
environment variables, secrets, in-memory providers. Final value wins by order.

### Why is it used?
Separates settings (connection strings, JWT secret, URLs) from code, and overrides per
environment without recompiling.

### Interview Answer
"Configuration in ASP.NET Core is a layered key-value system. The default provider
chain reads appsettings.json, environment-specific files, environment variables, and
command-line args — later sources override earlier ones. That's how I keep secrets
out of code: the JWT secret and DB connection come from environment variables
(`.env` → `AddInMemoryCollection`), overriding appsettings values."

### LearnPath Example
`Program.cs`:
- `builder.Configuration["JwtSettings:Issuer"]` / `GetSection("JwtSettings")`.
- Reads `.env`, maps env names to config keys (`JwtSettings:Secret`,
  `ConnectionStrings:DefaultConnection`), and calls
  `builder.Configuration.AddInMemoryCollection(...)` to override appsettings.
- `GetConnectionString("DefaultConnection")` for the DB.

### Common Mistake
- Hard-coding secrets/jump to production config — bad practice; use env vars.

### Follow-Up Questions
Q: Precedence order?
A: command line > env vars > user secrets > appsettings.{Env}.json > appsettings.json.
(Each provider later in the chain typically overrides earlier.)

### Remember
- Config = many sources, later wins. GetSection + GetConnectionString + envs.

---

## 13. appsettings.json 🟡

### What is it?
The main JSON config file of the app, loaded automatically at startup into
Configuration as a set of key-value entries.

### Why is it used?
A readable, checked-in place for non-secret settings.

### Interview Answer
"`appsettings.json` is the default configuration file. It holds settings as JSON
sections — JWT options, allowed CORS origins, upload limits, AI provider config,
logging levels. I read sections with `GetSection("JwtSettings")` and bind them to
typed classes."

### LearnPath Example
`backend/appsettings.json` holds `JwtSettings`, `AllowedOrigins`,
`UploadSettings`, `AiOptions`, `Logging`.

### Common Mistake
- Putting real secrets in appsettings.json that gets committed — the JWT Secret and
connection strings come from env vars here, not this file.

### Remember
- appsettings.json = non-secret settings; secrets via env vars.

---

## 14. Environment-specific configuration 🟡

### What is it?
`appsettings.Development.json` / `appsettings.Production.json` override base settings
per runtime environment (`ASPNETCORE_ENVIRONMENT`).

### Why is it used?
Different environments need different values — dev uses localhost CORS/verbose logs,
production uses real domain/tighter logs.

### Interview Answer
"ASP.NET Core loads `appsettings.json` then `appsettings.{Environment}.json` — the
environment is chosen by `ASPNETCORE_ENVIRONMENT` (Development, Production). So dev
settings override base settings in Development and production settings override in
Production. The app checks `app.Environment.IsDevelopment()` to enable dev-only
features."

### LearnPath Example
- `appsettings.Development.json` — localhost CORS origin, verbose EFCore SQL logging.
- `appsettings.Production.json` — production CORS domain, empty secrets/comps filled
  from env vars, Warning logging level.
- `Program.cs` — Swagger only `if (app.Environment.IsDevelopment())`.

### Common Mistake
- Forgetting the environment variable entirely — defaults to Production with a
warning in some hosting setups.

### Follow-Up Questions
Q: How do I switch environment locally?
A: Set `ASPNETCORE_ENVIRONMENT=Development` (or `dotnet run` with launchSettings
profile).

### Remember
- appsettings.{Env}.json overrides appsettings.json based on the environment var.

---

## 15. Logging 🟡

### What is it?
Structured diagnostic output (`ILogger<T>`), levels: Trace → Debug → Information →
Warning → Error → Critical. Providers write to console/file/etc.

### Why is it used?
Observability — knowing what happened, when, why (errors, slow requests) in
production.

### Interview Answer
"Logging records what the app is doing via `ILogger<T>`, which I inject into classes.
Levels control verbosity: Information for normal events, Warning for suspicious,
Error for failures. I use structured messages with placeholders, and the config file
sets the minimum level per namespace."

### Simple Example
```csharp
_logger.LogInformation("{Method} {Path} responded {StatusCode} in {Elapsed}ms",
    method, path, status, elapsed);
```

### LearnPath Example
`LoggingMiddleware` logs method/path/status/duration of every request. `ExceptionMiddleware`
logs full exceptions. `LearningPathService` logs audit-log failures. Log levels are
set in appsettings (`Default: Information`, `Microsoft.AspNetCore: Warning`).

### Common Mistake
- String concatenation in log messages — use placeholders to keep structured data.

### Follow-Up Questions
Q: What are log levels?
A: Trace/Debug/Information/Warning/Error/Critical — set by config, higher ones shown.

### Remember
- ILogger + levels + structured placeholders. Also used in audit via `AuditLog`
service.

---

## 16. Options pattern 🟡

### What is it?
Binding a config section to a typed class: `Configure<T>(section)` + inject
`IOptions<T>` / `IOptionsSnapshot<T>`.

### Why is it used?
Strong typing — you get settings as a typed object with compile-time checks instead
of string lookups everywhere.

### Interview Answer
"The Options pattern binds a config section to a typed class. I register
`Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"))` and then
inject `IOptions<JwtSettings>` to read it as a strongly-typed object. It replaces
stringly-typed config lookups."

### Simple Example
```csharp
// registration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// consumption
public JwtTokenGenerator(IOptions<JwtSettings> options) => _settings = options.Value;
```

### LearnPath Example
`Program.cs` registers `Configure<JwtSettings>`, `Configure<UploadSettings>`,
`Configure<AiOptions>`. `JwtTokenGenerator` injects `IOptions<JwtSettings>` and reads
`options.Value` (`Secret`, `Issuer`, `Audience`, `ExpiryMinutes`).

### Common Mistake
- Not knowing `IOptions<T>.Value` is the accessor (the class has a `.Value` property).

### Follow-Up Questions
Q: IOptions vs IOptionsSnapshot?
A: IOptions = same instance for app lifetime; Snapshot reloads on config change.

### Remember
- Configure<T>(section) + IOptions<T> = typed settings.

---

## Final Phase 3 "remember by heart" checklist

1. .NET = platform (net10.0); C# = language; CLR = runtime (JIT + GC).
2. .NET Core/modern .NET vs old .NET Framework — always say "modern .NET".
3. NuGet packages are compiled DLLs; csproj lists them.
4. DI: register in Program.cs → inject via constructors. DbContext scoped.
5. Transient/Scoped/Singleton — per-call / per-request / per-app.
6. appsettings.json + appsettings.{Env}.json + env vars (later wins).
7. Logging with ILogger + levels; Options pattern = typed config.

Next: `04_ASPNET_Core.md` — say "Proceed to next phase" when ready.