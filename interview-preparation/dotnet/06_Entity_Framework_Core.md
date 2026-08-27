# Phase 6 — Entity Framework Core (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

All LearnPath references verified from source: `backend/Data/ApplicationDbContext.cs`,
`backend/Entities/*`, `backend/Configurations/*`, `backend/Migrations/*`,
`backend/Repositories/*`, and queries in services.

---

## 1. What is Entity Framework Core? 🔥

### What is it?
Entity Framework Core (EF Core) is an ORM (Object-Relational Mapper) for .NET that
lets you work with a database using C# objects (entities) instead of writing raw SQL.

### Why is it used?
Database work becomes type-safe C#: entities map to tables, LINQ queries to SQL,
save changes to statements — less SQL, fewer errors, faster development.

### Interview Answer
"EF Core is a library that maps my C# classes to database tables. I query and modify
data using C# objects and LINQ, and EF translates that into SQL. I never write raw
SQL for CRUD — I write `_context.Paths.Where(...)` — which is safer, typed, and
portable."

### LearnPath Example
`Microsoft.EntityFrameworkCore.SqlServer` package, `ApplicationDbContext`,
entities in `backend/Entities/`, migrations in `backend/Migrations/`.

### Common Mistake
- Saying EF Core is a database — it's an ORM *on top of* a database (SQL Server here).

### Remember
- EF Core = ORM. C# objects ↔ SQL Server tables, LINQ ↔ SQL.

---

## 2. Why ORM? 🔥

### What is it?
ORM = Object-Relational Mapping: bridging the gap between object-oriented code and
relational tables, so developers use objects, not SQL strings.

### Why is it used?
Productivity + safety: no hand-written SQL strings, no manual result-to-object code,
automatic change tracking, fewer bugs.

### Interview Answer
"An ORM bridges objects and database tables. Without it I'd write SQL strings,
execute them, and manually map rows to objects. With EF Core I write C# and LINQ,
and it handles the SQL and mapping. It also tracks changes and generates updates.
The trade-off is some performance overhead and generated SQL — that's the price of
productivity."

### LearnPath Example
`GenericRepository` and services do all data access through EF Core — no raw SQL
strings in the codebase (verified).

### Remember
- ORM = object↔table mapping. Productivity and type safety over hand-written SQL.

---

## 3. DbContext 🔥

### What is it?
The class representing the database session: holds DbSets, change tracking, and
saves changes. Configured with connection string and provider.

### Why is it used?
It's the unit of work — one context per request tracks all changes and commits them
together.

### Interview Answer
"The `DbContext` is the bridge between my code and the database. It exposes my
entities as `DbSet`s, tracks changes to entities, and its `SaveChangesAsync` pushes
all changes to the database in one transaction. In LearnPath it's `ApplicationDbContext`,
registered as scoped so each HTTP request gets one context."

### LearnPath Example
`backend/Data/ApplicationDbContext.cs` — `: IdentityDbContext<User>`, DbSets for every
entity, `OnModelCreating` applies configurations, overridden `SaveChanges` guards
audit logs.

### How it works
One query → built from DbSet LINQ → executed on a connection from the provider (SQL
Server) → results materialized into entity instances → tracked until disposed.

### Common Mistake
- Keeping a context alive too long — contexts should be short-lived (per request).

### Remember
- DbContext = session + unit of work + change tracker. Scoped per request.

---

## 4. DbSet 🔥

### What is it?
A typed collection representing a table: `context.Users` = the `Users` table, and
queries/CRUD start from it.

### Why is it used?
The entry point for queries and inserts — a typed handle on a table.

### Interview Answer
"A `DbSet<T>` represents a table in code. `_context.Users` is the Users table —
I query it with LINQ, add rows with `Add`, remove with `Remove`. It's how I write
"SELECT/FROM/INSERT" without SQL."

### Simple Example
```csharp
var users = await _context.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();
```

### LearnPath Example
`ApplicationDbContext` declares DbSets for all entities (`LearningPaths`, `Modules`,
`Progresses`, `Classrooms`, `Posts`, etc.). `GenericRepository` uses
`context.Set<T>()` generically.

### Remember
- DbSet = table handle. Queries start from DbSet.

---

## 5. Entity 🔥

### What is it?
A plain C# class mapped to a table; instances are rows; properties are columns.
They include navigation properties that link to related tables.

### Why is it used?
The "O" in ORM — your domain model as classes, plus relationships.

### Interview Answer
"An entity is a plain class that maps to a table. `LearningPath` → `LearningPaths`
table, its properties → columns. What's special: navigation properties like
`LearningPath.Modules` express relationships, so I can navigate a path to its modules
in code without writing JOINs."

### LearnPath Example
`backend/Entities/LearningPath.cs`, `Module.cs`, `User.cs` — POCOs (plain old C#
objects) with properties and navigation collections (`Modules`, `Progresses`,
`Resources`, `Tags`).

### Common Mistake
- Saying entities are the same as DTOs — entities mirror the DB; DTOs mirror the API.

### Remember
- Entity = table shape + relationships via navigation properties.

---

## 6. Database relationships 🥇🔥

### What is it?
How tables connect: one-to-many (primary-key/FK), one-to-one, many-to-many. In EF you
model them with navigation properties + configurations.

### Why is it used?
Real-world data is linked — a path has modules, a module has resources — and EF
reflects that in objects.

### Interview Answer
"Relationships model how tables refer to each other. The main ones are one-to-many
(a LearningPath has many Modules), one-to-one (a Module has one ModuleQuiz), and
many-to-many (Users join Classrooms via a join table). In EF I express them with
navigation properties on both sides, and the FK identifies the connection."

### LearnPath Example
`LearningPath` → many `Module`; `Module` → many `ModuleResource`/`ModuleObjective`/
`ModuleTag`/`Progress`. `User` → many `RefreshToken`, many `Progress`.

### Remember
- 1—many, 1—1, many—many — expressed with navigation properties + FKs.

---

## 7. One-to-one ⚪

### What is it?
Each record relates to exactly one record on the other side. Rare — data is usually
a row in each table sharing the same key or a FK that's unique.

### Why is it used?
Splitting a big table into two, or optional detail data.

### Interview Answer
"One-to-one means one row matches one row. In EF I model it with a navigation property
on both sides and a FK that must be unique. I use it less often — in the project the
closest is a Module and its optional `ModuleQuiz` (one module can have one quiz)."

### LearnPath Example
`Module` has `ModuleQuiz? ModuleQuiz` navigation (`backend/Entities/Module.cs:33`).

### Remember
- One-to-one = one ↔ one. Navigation property on both sides.

---

## 8. One-to-many 🔥

### What is it?
A parent row has many child rows: parent (LearningPath) → children (Modules). Child
holds the FK to the parent.

### Why is it used?
The most common relationship — almost every table pair in real apps.

### Interview Answer
"One-to-many is the most common relationship: one parent has many children, and each
child stores the parent's ID as a foreign key. A learning path has many modules; each
module carries `LearningPathId`. EF navigates it both ways: `path.Modules` and
`module.LearningPath`."

### Simple Example
```csharp
// LearningPath
public ICollection<Module> Modules { get; set; } = [];
// Module
public int LearningPathId { get; set; }
public LearningPath LearningPath { get; set; } = null!;
```

### LearnPath Example
`LearningPath` ↔ `Module`, `Classroom` ↔ `Assignment`/`UserClassroom`,
`User` ↔ `RefreshToken`. Configurations use `HasOne(...).WithMany(...).HasForeignKey(...)`.

### Remember
- One-to-many = FK on child + collection on parent.

---

## 9. Many-to-many 🔥

### What is it?
Records connect to many records on both sides (Users ↔ Classrooms). Represented by a
join table with two FKs, either explicit (entity) or implicit.

### Why is it used?
Real "both sides many" relations — students in many classrooms, classrooms with many
students.

### Interview Answer
"Many-to-many needs a join table holding two foreign keys. EF handles it two ways:
an explicit join entity (a class with the two keys) or the implicit skip navigation.
In LearnPath, Users and Classrooms are many-to-many through the explicit
`UserClassroom` join entity, which also stores extra data — the member's role in that
classroom."

### Simple Example
```csharp
// UserClassroom (join entity)
UserId + ClassroomId  (composite primary key)
// User has: UserClassrooms
// Classroom has: UserClassrooms
```

### LearnPath Example
`backend/Entities/UserClassroom.cs` + `UserClassroomConfiguration.cs` — composite key
`(UserId, ClassroomId)`, cascades, plus `Role` column. The self-referencing
`ModuleDependency` is also many-to-many (module ↔ module) with a composite key.

### Common Mistake
- Not knowing what the join table is — it's the extra table between two ones.

### Remember
- Many-to-many = join table with two FKs + optional extra columns.

---

## 10. Foreign keys 🔥

### What is it?
A column holding another table's primary key to link rows. Defines the relationship
and enforces referential integrity.

### Why is it used?
The mechanism of relationships; also decides behavior on delete (cascade/restrict).

### Interview Answer
"A foreign key is a column that stores another table's ID to link the rows. In
`Module`, `LearningPathId` is the FK to the learning path. On delete, FK behavior
matters — cascade deletes children, restrict blocks deletion if children exist. My
configurations choose this per relationship; audit logs and owned content are
protected."

### LearnPath Example
`LearningPathConfiguration` — `HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.Restrict)`.
`UserClassroomConfiguration` — Cascade. `ModuleDependencyConfiguration` — Restrict on
both sides.

### Remember
- FK = link column; delete behavior: Cascade vs Restrict by design.

---

## 11. Fluent API 🔥

### What is it?
Configuring the model in C# code with `IEntityTypeConfiguration<T>` classes or inside
`OnModelCreating` — keys, constraints, relationships, column types.

### Why is it used?
Precise, powerful mapping control without touching entity classes — keeps entities
clean POCOs.

### Interview Answer
"Fluent API configures entities in code rather than with attributes on the class.
Each entity has a configuration class implementing `IEntityTypeConfiguration<T>`
where I define keys, lengths, FK behavior. `ApplicationDbContext` applies them all
with `ApplyConfigurationsFromAssembly`, on `OnModelCreating`."

### Simple Example
```csharp
public class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.HasOne(p => p.CreatedBy)
               .WithMany(u => u.CreatedPaths)
               .HasForeignKey(p => p.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### LearnPath Example
18 configuration classes in `backend/Configurations/` (verified) — including
`LearningPathConfiguration`, `UserClassroomConfiguration`,
`ModuleDependencyConfiguration`. This is why entities stay clean.

### Common Mistake
- Writing all config in `OnModelCreating` — cleaner to split per entity.

### Remember
- Fluent API = precise mapping in dedicated config classes.

---

## 12. Data annotations 🟡

### What is it?
Mapping/validation attributes *on the entity class*: `[Key]`, `[Required]`,
`[MaxLength(200)]`, `[Column]`, `[Table]`.

### Why is it used?
Quick, simple mapping rules — but they clutter entities and are less powerful than
Fluent API.

### Interview Answer
"Data annotations are attributes on the entity class — `[Key]`, `[Required]`,
`[MaxLength]`. They're quick but mixed into the class. I prefer Fluent API in LearnPath
to keep entities clean; the few data annotations that appear are from ASP.NET Core
Identity's base types (like `IdentityUser`'s internal configuration)."

### LearnPath Example
Entities are clean POCOs — no mapping annotations on them. All mapping lives in
`Configurations/` (Fluent API). (Notable honest note: the project deliberately chose
Fluent API.)

### Common Mistake
- Assuming entities use annotations just because it's a common style — check the
project first.

### Remember
- Annotations OR Fluent — LearnPath chose Fluent API.

---

## 13. Migrations 🔥

### What is it?
Versioned C# files that describe schema changes: `Up()` applies them, `Down()` reverts.
Generated by comparing your model with the current DB.

### Why is it used?
Database schema evolves with code and is reproducible on any environment (dev → prod).

### Interview Answer
"Migrations are how EF Core keeps the database schema in sync with my C# model. When
I change an entity, I generate a migration — EF writes an `Up`/`Down` pair of schema
operations. Applying it alters the real database. This makes schema changes versioned
and repeatable across environments. It's the 'database version control' of EF."

### LearnPath Example
`backend/Migrations/20260615050934_InitialCreate.cs` (the initial schema) + the model
snapshot. Program.cs calls `db.Database.MigrateAsync()` at startup so the schema is
applied automatically on deploy.

### Common Mistake
- Editing the database by hand — migrations and code should be the source of truth.

### Remember
- Migration = versioned Up/Down schema change. Applied via ef or startup.

---

## 14. Migration workflow 🟡

### What is it?
Change model → add migration → apply (or generate SQL script) → repeat.
Commands: `dotnet ef migrations add X`, `dotnet ef database update`.

### Why is it used?
A clear, safe repeatable process for schema evolution.

### Interview Answer
"My workflow: change the entity/config, then run `dotnet ef migrations add Name`.
EF compares the model to the last migration, writes the new `Up`/`Down`, and I apply
it with `dotnet ef database update`. In production I prefer generating a SQL script
with `dotnet ef migrations script` for review, or rely on `MigrateAsync()` on startup
which this project does."

### LearnPath Example
`Program.cs:306-309` — `db.Database.MigrateAsync()` runs pending migrations on
startup, then seeds roles/admin.

### Common Mistake
- Forgetting to apply migrations — schema doesn't match the model → runtime errors.

### Remember
- Model change → `add migration` → `update database` → repeat.

---

## 15. LINQ with EF Core 🔥

### What is it?
Writing LINQ queries that EF translates into SQL and executes in the database.

### Why is it used?
Type-safe queries: compile-time checking, projections to DTOs, and filtered, paginated
data instead of loading whole tables.

### Interview Answer
"I query the database with LINQ and EF Core translates it into SQL. `Where` becomes
WHERE, `OrderBy` becomes ORDER BY, `Select` projects columns, and `Include` does the
JOINs. I always use the async versions — `ToListAsync`, `FirstOrDefaultAsync`,
`CountAsync` — so the thread isn't blocked while the DB works."

### LearnPath Example
Every service: `LearningPathService` (Includes + filters), `AdminController` (filter +
pagination with `Skip`/`Take` + `CountAsync`), `AnalyticsService` (`GroupBy`).

### How it works
LINQ → expression tree → EF translates to SQL (parameterized) → SQL Server runs it →
results materialized into entities/DTOs.

### Common Mistake
- Calling `.ToList()` then filtering in memory — filters must stay *before* materialization
so they run in SQL (less data transferred).

### Remember
- LINQ = SQL in C#. Keep filters before ToList → server-side, not client-side.

---

## 16. Tracking vs AsNoTracking 🔥

### What is it?
By default EF *tracks* query results in the change tracker — edits are detected and
saved on `SaveChangesAsync`. `AsNoTracking()` skips tracking for read-only queries.

### Why is it used?
Only track when you'll UPDATE — read-only data doesn't need to be watched; no tracking
is faster and uses less memory.

### Interview Answer
"By default, entities returned by queries are tracked — EF watches them and compares
them on `SaveChangesAsync` to generate UPDATEs. For pure reads that I won't edit,
I add `.AsNoTracking()` to skip tracking — faster and lighter. The rule: track when
you're going to update, don't track for read-only responses."

### Simple Example
```csharp
var pathTitle = await _context.LearningPaths
    .AsNoTracking()   // read-only, no change tracking overhead
    .FirstOrDefaultAsync(p => p.Id == id);
```

### LearnPath Example
`AuditLogService` and `CommunityRepository` read with `.AsNoTracking()`;
`AdminController` reads a title with `AsNoTracking()`; mutations use tracked
entities + `_context.Update(entity)`.

### Common Mistake
- Tracking huge read queries — memory bloat; know when to turn it off.

### Remember
- Track = I will change it. AsNoTracking = read-only, faster.

---

## 17. Include / ThenInclude 🔥

### What is it?
Eager loading related data: `Include(p => p.Modules).ThenInclude(m => m.Resources)`
generates JOINs and loads the whole object graph in one query.

### Why is it used?
Avoiding "N+1 queries" — loading children lazily one query per parent row is the
classic performance trap.

### Interview Answer
"`Include` tells EF to load related data eagerly with JOINs, and `ThenInclude` goes
deeper. It prevents the N+1 problem, where loading 100 modules would otherwise fire
100 extra queries. I use it to load a path with modules, then their resources,
objectives, and tags in a single query."

### Simple Example
```csharp
var path = await _context.LearningPaths
    .Include(p => p.Modules)
        .ThenInclude(m => m.Resources)
    .FirstOrDefaultAsync(p => p.Id == id);
```

### LearnPath Example
`LearningPathService.GetByIdAsync` — deep graph loading with `Include`/`ThenInclude`
for modules, resources, objectives, tags, dependencies, and progress.

### Common Mistake
- Over-including everything (huge cartesian queries) or under-including (N+1).

### Remember
- Include = eager load; protects against N+1.

---

## 18. SaveChanges 🔥

### What is it?
The method that writes all tracked changes to the database. It runs a transaction,
executes INSERT/UPDATE/DELETE statements, returns affected row count.

### Why is it used?
It is where changes become reality — the commit point of the DbContext.

### Interview Answer
"`SaveChangesAsync` pushes every pending change in the context to the database: added
entities become INSERTs, modified become UPDATEs, removed become DELETEs — all inside
one transaction. The ChangeTracker already knows what changed, so I just call it once
at the end of a service method."

### Simple Example
```csharp
await _context.Progresses.AddAsync(new Progress { ... });
await _context.SaveChangesAsync();   // INSERT happens here
```

### LearnPath Example
Every mutation service calls `SaveChangesAsync` after modifying/add/removing entities.
`ApplicationDbContext` overrides it to first guard audit-log immutability.

### How it works
ChangeTracker diffs tracked entities → generates statements from the changes → one
transaction → SQL Server executes → result count returned → tracker re-syncs.

### Common Mistake
- Calling SaveChanges multiple times — batches changes instead.

### Remember
- SaveChanges = one commit point for all tracking changes, in a transaction.

---

## 19. Add / Update / Delete 🔥

### What is it?
The CRUD verbs on DbSet: `Add` (insert), `Update` (mark modified), `Remove` (delete).
Async variants: `AddAsync`/`FirstOrDefaultAsync`, etc.

### Why is it used?
Direct, simple CRUD — the everyday operations.

### Interview Answer
"`Add` marks an entity for insert, `Update` marks it modified, `Remove` marks it for
deletion — and `SaveChangesAsync` executes them. `.RemoveRange` deletes many, and
`AddRange` inserts many. I create entities with `AddAsync`, modify existing ones
(they update automatically since they're tracked), and delete with `Remove`."

### Simple Example
```csharp
_context.Modules.RemoveRange(await _context.Modules.Where(...).ToListAsync());
await _context.SaveChangesAsync();
```

### LearnPath Example
`GenericRepository` — `AddAsync`, `Update`, `Delete`, `SaveChangesAsync`.
`LearningPathService` — `AddAsync` new path/module, `RemoveRange` in the delete
routine, `Update` semantics via tracked entities.

### Common Mistake
- Not realizing tracked entities update automatically — you don't call `Update` for
an entity you just loaded.

### Remember
- Add = insert, tracked change = update, Remove = delete; SaveChanges commits all.

---

## 20. Transactions (basic) 🔥

### What is it?
A unit of work where multiple operations succeed or fail together — all-or-nothing.
`context.Database.BeginTransactionAsync()`.

### Why is it used?
Multi-step operations (delete a path + its modules + dependent rows) must not apply
halfway.

### Interview Answer
"A transaction makes several database operations atomic — either all succeed or all
roll back. `SaveChangesAsync` itself is already transactional. For multi-save flows I
start an explicit transaction. Deleting a learning path in LearnPath is a great
example — I remove many dependent rows in FK-safe order, then commit; if anything
fails, everything rolls back."

### Simple Example
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... multiple operations ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch { await transaction.RollbackAsync(); throw; }
```

### LearnPath Example
`LearningPathService.DeleteAsync` — uses `CreateExecutionStrategy` + explicit
transaction + manual FK-safe delete order, then `CommitAsync()`.

### Common Mistake
- Forgetting to commit — changes are only saved on `CommitAsync`.

### Remember
- Transaction = all-or-nothing. Explicit transactions for multi-step saves

---

## 21. Repository pattern 🟡

### What is it?
An abstraction layer over EF Core: an interface (`IGenericRepository<T>`) hides
data-access details; implementations wrap DbContext operations.

### Why is it used?
Centralized data access, easier mocking in tests, consistent operations.

### Interview Answer
"A repository wraps data access behind an interface. `IGenericRepository<T>` exposes
generic CRUD — `GetByIdAsync`, `FindAsync`, `AddAsync`, `Update`, `Delete`.
Implementations use the DbContext. The pattern keeps services from touching EF
directly and makes tests easy — I can mock the interface. But with EF Core, some
teams skip repositories entirely because DbContext is already an abstraction; I used
a generic repository in LearnPath for the common operations while services use the
context directly for complex queries."

### LearnPath Example
`backend/Repositories/GenericRepository.cs` + `IGenericRepository<T>`;
`backend/Repositories/CommunityRepository.cs` + `ICommunityRepository` — the one
specific repository registered in DI (`AddScoped<ICommunityRepository, CommunityRepository>`)
in Program.cs.

### Common Mistake
- Claiming you used "repository everywhere" — be precise: generic repo + one
specialized repo; services also use DbContext directly for complex queries.

### Remember
- Repository = data-access abstraction; services use it where it fits.

---

## 22. Service + repository flow 🔥

### What is it?
Layer flow: Controller → Service (business rules) → Repository/DbContext → Database
→ DTO → back out. Services contain logic; repositories/EF handle storage.

### Why is it used?
Separation of concerns — controllers stay thin, business rules are testable, data
access is centralized.

### Interview Answer
"A controller receives the request and calls a service. The service applies business
rules, coordinates database work through the DbContext or repository, and returns
DTOs. The controller only packages the result. For example, `LearningPathController`
delegates to `ILearningPathService`; the service checks ownership, builds the query,
maps to DTOs, and returns — the controller just returns `Ok(...)`."

### LearnPath Example
`AuthController → IAuthService/AuthService → UserManager + DbContext` — service checks
status, roles, issues JWT, saves the refresh token.
`LearningPathController → ILearningPathService → DbContext` — ownership checks,
graph validation, audit logging.

### Remember
- Controller = thin HTTP → Service = logic + data → Repo/EF = storage → DTO out.

---

## 23. Common EF Core mistakes 🔥

### What is it?
The recurring pitfalls freshers hit:
1. N+1 queries (not using Include / lazy loading in loops).
2. Client-side filtering — `.ToList()` before `Where`/`Select`.
3. Not using async methods in ASP.NET (blocking threads).
4. Tracking everything (memory bloat) — need `AsNoTracking` for reads.
5. Wrong DeleteBehavior — cascade deleting protected data.
6. Forgetting migrations — model/DB drift — errors at runtime.
7. Writing SELECT * and mapping by hand — let EF project with `Select`.

### Why is it used?
Knowing these shows you understand EF beyond basic CRUD.

### Interview Answer
"The big ones: N+1 — loading children inside a loop without Include; materializing too
early — `ToList()` before filtering, so filtering happens in memory; forgetting async —
blocking the thread; and wrong delete behavior — cascading into data you wanted to
protect. I avoid them by using Include, keeping filters before materialization, using
Async methods, and designing delete behavior in the configurations."

### LearnPath Example
The project demonstrates good practice: `Include`/`ThenInclude` for graphs,
`AsNoTracking()` for reads, `EnableRetryOnFailure`, explicit FK-safe transaction for
the complex delete, and audit-log immutability guard.

### Remember
- Include before N+1, filter before ToList, Async always, design deletes.

---

## 24. LearnPath’s actual EF Core usage 🔥

Full verified picture for the "Explain your data layer" question:

- **Provider & context**: SQL Server via `UseSqlServer` with `EnableRetryOnFailure`
  (`Program.cs:105-108`), `ApplicationDbContext : IdentityDbContext<User>`.
- **Entities**: 25+ POCOs in `backend/Entities/` (User, LearningPath, Module,
  Progress, Classroom, Assignment, Submission, Certificate, Post, Comment, Group,
  RefreshToken, Quiz/Question/Option, AuditLog, ...).
- **Identity**: tables + user/role management provided by Identity (UserManager,
  RoleManager); `User : IdentityUser`, roles seeded via `RoleSeeder`.
- **Mapping**: Fluent API via `IEntityTypeConfiguration<T>` classes + `ApplyConfigurationsFromAssembly`
  (backend/Configurations/). Include composite keys (UserClassroom, ModuleDependency),
  FK delete behavior (Restrict for protected data), max lengths.
- **Migrations**: `Migrations/` + startup `MigrateAsync()` + seeders (roles, admin).
- **Queries**: LINQ + async (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`,
  `CountAsync`) with `Include`/`ThenInclude`, `AsNoTracking()` for reads,
  `GroupBy`/`ToDictionaryAsync` for analytics/dependencies.
- **Writes**: AddAsync/SaveChangesAsync, Remove/RemoveRange, explicit transaction +
  execution strategy for the big delete (LearningPathService).
- **Repos**: `GenericRepository<T>` + `CommunityRepository`; services also use
  DbContext directly — a pragmatic mix.
- **Safety**: audit logs append-only (ChangeTracker guard), error mapping in
  ExceptionMiddleware.

### Follow-Up Questions
Q: Why didn't you use raw SQL?
A: EF gives type safety, productivity, and automatic schema sync; for the rare
complex query you can still write raw SQL in EF — but I didn't need to here.

Q: How would you measure EF performance?
A: Check the generated SQL with logging, watch for N+1, use AsNoTracking for reads,
and query projections.

### Remember
- Full stack: SQL Server ← DbContext + Fluent API + Migrations ← Repos/Service queries
  ← Controllers ← JSON to React. That is the data story of LearnPath.

---

## Final Phase 6 "remember by heart" checklist

1. EF Core = ORM: entities → tables, LINQ → SQL, SaveChanges → transaction.
2. DbContext = session (scoped), DbSet = table handle.
3. Relationships: 1-many (FK on child), many-many (join table), 1-1 (unique FK).
4. Fluent API in configuration classes + migrations + MigrateAsync.
5. Async methods + Include/ThenInclude + AsNoTracking + filter before ToList.
6. Transactions for multi-step writes; DeleteBehavior designed per relationship.

Next: `07_Authentication_Security.md` — say "Proceed to next phase" when ready.