# LearnPath Interview Preparation

## Phase 1 — Graph-Based Learning Platform

### What Makes LearnPath a Graph-Based Learning Platform?

LearnPath uses a **graph structure** to represent learning materials. Unlike a simple linear list of modules, modules can have dependencies on other modules. These dependencies form a **Directed Acyclic Graph (DAG)** — a structure where:

- Each module is a **node**
- Each dependency (module A depends on module B) is a **directed edge** from A to B
- The graph has **no cycles** — you can never follow dependencies forever

This allows for flexible learning journeys where:
- Some modules can be started immediately
- Other modules require completing specific prerequisite modules first
- The system can determine which modules are "unlocked" for each student

### Learning Path

A **Learning Path** is a container for organized learning content. It has:
- A title and description
- Published/public status
- Multiple modules organized within it
- Created by a specific user (instructor)

In the code, `LearningPath` entity contains a collection of `Module` objects. When a learning path is fetched, the system includes all its modules with their dependencies, progress, and other details.

### Module

A **Module** is an individual learning unit within a learning path. Each module has:
- Title and description
- Content (video, article, quiz, etc.)
- Difficulty level
- Order number (for sequential progression)
- Status (NotStarted, InProgress, Completed, etc.)
- Dependencies (which other modules must be completed first)
- Resources (files, links, etc.)
- Objectives (learning goals)
- Tags for categorization

In the code, the `Module` entity has:
- `Id`, `Title`, `Description`, `ContentUrl`, `ContentType`
- `Order`, `Difficulty`, `EstimatedDurationMinutes`
- `Status` (from `ModuleStatus` enum)
- `LearningPathId` (links to which path it belongs)
- `Dependencies` and `Dependents` (for the graph structure)
- `Progresses` (tracking student progress)

### Module Ordering

Modules within a learning path have an **Order** number. This serves two purposes:

1. **Sequential progression**: When a module has no explicit dependencies, the system checks if the previous module (by order number) is completed. This creates a default linear flow: Module 1 → Module 2 → Module 3, etc.

2. **Reordering**: Instructors can manually reorder modules using the reorder API. The order values are swapped between modules.

The `GetByIdAsync` method in `LearningPathService` sorts modules by order and checks both DAG and sequential unlocking conditions.

### Module Dependencies

Dependencies define which modules must be completed before others can start. This is the core of the graph structure.

- A dependency is stored in the `ModuleDependency` table
- Each dependency has: `ModuleId` (the module that depends) and `DependsOnModuleId` (the module that must be completed first)
- The `DagValidator` ensures adding a new dependency won't create a cycle

**Example**: If Module 3 depends on Module 1, then Module 1 must be completed before Module 3 can be accessed.

### DAG (Directed Acyclic Graph)

The **DAG** is the overall graph structure of all module dependencies within a learning path.

Key properties:
- **Directed**: Dependencies have a direction (Module A → Module B means A must come before B)
- **Acyclic**: No cycles exist — you can never have A → B → C → A
- **Cycle prevention**: When adding a new dependency, the system runs `DagValidator.WouldCreateCycle()` to check if it would create a loop

The code uses a **depth-first search (DFS)** algorithm to detect cycles. If adding Module A → Module B would create a path from B back to A (through existing dependencies), the addition is rejected.

### How the Graph is Represented in the Database

The graph structure is stored in the `ModuleDependencies` table:

| Column | Description |
|--------|-------------|
| ModuleId | The module that has the dependency |
| DependsOnModuleId | The module that must be completed first |

In Entity Framework Core, the `Module` entity has:
- `ICollection<ModuleDependency> Dependencies` — dependencies where this module is the dependent
- `ICollection<ModuleDependency> Dependents` — dependencies where this module is the prerequisite

When fetching a learning path, the code includes these dependencies:
```csharp
.Include(p => p.Modules.Where(m => ...).ThenInclude(m => m.Dependencies))
```

### How Dependencies Are Checked

When a student tries to access a module, the system checks two conditions:

1. **DAG unlocking**: All explicit dependencies are completed
   - The system gets all dependency IDs for the module
   - Checks if all those dependency module IDs are in the student's completed set
   - `var dagUnlocked = dependencyIds.All(dId => completedModuleIds.Contains(dId));`

2. **Sequential unlocking**: If no explicit dependencies, the previous module by order is completed
   - If the module has no dependencies, check if the previous module (by order number) is completed
   - `var sequentialUnlocked = dependencyIds.Any() ? true : completedModuleIds.Contains(prevModuleId);`

3. **Final unlock status**: `isUnlocked = dagUnlocked && sequentialUnlocked`

If a module is not unlocked, the system throws: `"Please complete the previous module to unlock this module."`

### How Students Move Through Learning Paths

Students progress through a learning path by:

1. **Starting with unlocked modules**: Modules without dependencies or with completed dependencies are initially unlocked

2. **Completing modules**: When a module is completed, a `Progress` record is created/updated:
   - `UserId` + `ModuleId` + `IsCompleted` = true + `CompletedAt` timestamp

3. **Unlocking subsequent modules**: After completion, the system re-evaluates which modules are now unlocked:
   - Modules that depended on the completed module may now be unlocked (DAG)
   - Modules that follow sequentially may now be unlocked

4. **Moving to the next module**: The student can now start the next unlocked module

The `GetModuleContentAsync` method performs these checks every time a module is accessed, ensuring the graph state is always current.

### Progress Tracking

Progress is tracked via the `Progress` entity:
- Links a `User` to a `Module`
- `IsCompleted` flag indicates if the student finished the module
- `CompletedAt` timestamp when completed

When a student completes a module, the system creates/updates a Progress record. The progress data is used to:
- Determine which modules are unlocked next
- Calculate overall path completion
- Generate statistics/reports

**Example flow**:
1. Student completes Module 1 → Progress record created with `IsCompleted = true`
2. System checks Module 2 (which depends on Module 1) → DAG unlocking now passes
3. Module 2 becomes unlocked and the student can start it

### How Completion Works

A module is considered **completed** when:
- The student marks it as complete (frontend action)
- A `Progress` record is saved with `IsCompleted = true` and `CompletedAt` set

The system then:
1. Saves the Progress record to the database
2. Re-evaluates module unlocking for all students
3. The next time the learning path is fetched, previously-locked modules may now appear as unlocked

**Important**: The `IsCompleted` check uses the student's progress records:
```csharp
var completedModuleIds = path.Modules
    .Where(m => m.Progresses.Any(p => p.UserId == userId && p.IsCompleted))
    .Select(m => m.Id)
    .ToHashSet();
```

### Algorithms Involved

The project includes graph algorithms in `backend/Algorithms/Graph/DagValidator.cs`:

1. **Cycle detection** (`WouldCreateCycle`): Uses DFS to check if adding a dependency would create a cycle. Prevents invalid graph structures.

2. **Topological sort** (`TopologicalSort`): Orders modules so dependencies come before dependents. Useful for determining valid learning sequences.

These algorithms ensure the graph remains valid and modules can be properly ordered for student progression.

### How the Graph Prevents Invalid Learning Sequences

The system prevents invalid sequences in two ways:

1. **At dependency-add time**: When an instructor adds a new dependency, `DagValidator.WouldCreateCycle()` checks if it would create a loop. If yes, the operation is rejected with: `"Adding this dependency would create a cycle."`

2. **At runtime**: When a student tries to access a module, the system verifies:
   - All explicit dependencies are completed (DAG check)
   - The sequential predecessor is completed (if no explicit dependencies)
   
   If either check fails, access is denied with: `"Please complete the previous module to unlock this module."`

This ensures students always follow valid learning sequences — they can only start a module when all its prerequisites are met.

### Interview Note: Resume vs Implementation

**Resume claim**: "Designing and developing a graph-based learning platform to provide structured learning journeys"

**Actual implementation**: The project implements a DAG-based system where:
- Dependencies are explicit (stored in `ModuleDependencies` table)
- Cycle prevention is enforced via algorithm
- Unlocking uses both DAG and sequential ordering
- Progress tracking determines what's unlocked

**Warning**: The resume mentions "graph-based learning platform" which is accurate — the code does implement a graph structure with dependencies, DAG validation, and topological ordering. However, the implementation focuses on prerequisite-based module unlocking rather than general graph traversal algorithms.

---

## Phase 2 — REST APIs and ASP.NET Core

### ASP.NET Core

**ASP.NET Core** is the framework for building the web API. It provides:
- Web server functionality
- Routing (mapping URLs to controllers/actions)
- Middleware pipeline
- Dependency injection built-in
- Authentication and authorization support

In LearnPath, the backend is built with ASP.NET Core minimal APIs (Controllers inheriting from `ControllerBase`). The project uses the standard .NET minimal API pattern with controllers decorated with attributes like `[ApiController]`, `[Route()]`, and `[Authorize]`.

### Web API

**Web API** refers to the HTTP-based API that the frontend (React) consumes. Key characteristics:
- Uses standard HTTP methods: GET, POST, PUT, DELETE
- Returns JSON data
- Follows REST principles (resources identified by URLs, CRUD operations)

All endpoints in LearnPath follow REST patterns. For example:
- `GET api/v1/paths` — get all learning paths
- `POST api/v1/paths` — create a new learning path
- `GET api/v1/paths/{id}` — get a specific learning path
- `PUT api/v1/paths/{id}` — update a learning path
- `DELETE api/v1/paths/{id}` — delete a learning path

### Controllers

**Controllers** handle HTTP requests and return responses. In LearnPath:
- Controllers are in `backend/Controllers/` folder
- Each controller handles a specific area (LearningPath, Auth, Quiz, Progress, etc.)
- Controllers inherit from `ControllerBase`
- They use dependency injection to get services

**Example**: `LearningPathController` (backend/Controllers/LearningPathController.cs):
- Route: `api/v1/paths`
- Handles path creation, retrieval, updating, deletion
- Handles module management within paths
- Handles dependency management
- Uses `[Authorize]` for protection, `[AllowAnonymous]` for public endpoints

Controllers do not contain business logic — they receive requests, validate input, and delegate to services.

### Routes

**Routes** map URLs to controller actions. LearnPath uses attribute routing:
- `[Route("api/v1/paths")]` — base route for LearningPathController
- `[HttpGet("public")]` — GET api/v1/paths/public
- `[HttpGet("my")]` — GET api/v1/paths/my
- `[HttpGet("{id:int}")]` — GET api/v1/paths/{id}

Route parameters:
- `{id:int}` — integer ID parameter
- `{pathId:int}` — learning path ID
- `{moduleId:int}` — module ID

If no route is specified, default routes are used. The `[Authorize]` attribute restricts access to authenticated users with specific roles.

### DTOs (Data Transfer Objects)

**DTOs** transfer data between layers (frontend ↔ backend, controller ↔ service). They avoid exposing internal entity shapes directly.

LearnPath has DTOs in `backend/DTOs/` folder. Examples:
- `LearningPathResponseDto` — response shape for learning paths
- `LearningPathDetailResponseDto` — detailed response with modules and dependencies
- `ModuleResponseDto` — module data for response
- `CreateLearningPathDto` — input for creating a path
- `UpdateLearningPathDto` — input for updating a path
- `AddDependencyDto` — adding a module dependency
- `ProgressDto` — progress tracking data

**Why DTOs?** To control what data is sent/received, avoid sending too much/unnecessary data, and separate internal database shape from API response shape.

In the code, controllers receive DTOs from the request body and return DTOs in responses. Mapping between entities and DTOs happens in the service layer (e.g., `LearningPathService.MapToResponse()`).

### Entity Framework Core

**Entity Framework Core (EF Core)** is the ORM (Object-Relational Mapper) that bridges C# code and SQL Server. It allows working with databases using C# objects instead of writing raw SQL.

Key EF Core concepts in LearnPath:
- `DbContext` — the main class that coordinates EF Core functionality
- `DbSet<T>` — represents a table of entities
- Migrations — creating/updating database schema
- LINQ queries — filtering, sorting, including related data
- Change tracking — EF Core tracks changes to entities and saves them

### DbContext

**DbContext** is the primary class in EF Core. In LearnPath:

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<LearningPath> LearningPaths { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<ModuleDependency> ModuleDependencies { get; set; }
    public DbSet<Progress> Progresses { get; set; }
    public DbSet<User> Users { get; set; }
    // ... other DbSets
}
```

The `ApplicationDbContext` is registered in the dependency injection container and injected into services/controllers. It provides `DbSet` properties for each database table.

### Database Operations

EF Core is used for all database operations in LearnPath. Pattern flow:

1. **Fetch**: `_context.LearningPaths.Find(id)` or `_context.LearningPaths.Where(...).ToListAsync()`
2. **Save**: `_context.SaveChangesAsync()` or `_context.SaveChangesAsync()` within a transaction
3. **Add**: `_context.Entities.Add(entity)` followed by `SaveChangesAsync()`
4. **Remove**: `_context.Entities.Remove(entity)` followed by `SaveChangesAsync()`
5. **Update**: Set entity properties, then `SaveChangesAsync()`

**Example from LearningPathService**:
```csharp
await _context.LearningPaths.AddAsync(path);
await _context.SaveChangesAsync();
```

The service layer uses EF Core to:
- Query data (`.Where()`, `.Include()`, `.Select()`)
- Save changes (`await _context.SaveChangesAsync()`)
- Execute raw SQL when needed (`await _context.Database.ExecuteSqlRawAsync()`)
- Use transactions for atomic operations

### Request → Controller → Service → EF Core → Database Flow

This is the core request flow in LearnPath:

1. **Frontend sends HTTP request** (e.g., GET api/v1/paths/5)
2. **Router matches the route** and invokes the controller action
3. **Controller receives the request** and extracts parameters (user ID from claims, route ID, etc.)
4. **Controller calls the service method** (e.g., `_service.GetByIdAsync(id, userId)`)
5. **Service uses EF Core** via `_context` to query/save data
6. **Database returns the data**
7. **Service maps to DTO** and returns it
8. **Controller wraps in response** and returns HTTP OK/Other status
9. **Frontend receives JSON response** and updates the UI

**Concrete example** (Get learning path by ID):
```
Frontend: GET /api/v1/paths/5
↓
LearningPathController.GetById(5)
↓
_service.GetByIdAsync(5, userId)
↓
EF Core: .Include(p => p.Modules).ThenInclude(m => m.Dependencies).FirstOrDefaultAsync()
↓
Database: Returns learning path with modules and dependencies
↓
Service: Maps to LearningPathDetailResponseDto
↓
Controller: Ok(ApiResponse<LearningPathDetailResponseDto>.Ok(result))
↓
Frontend: Receives JSON, displays path and modules
```

### Response Flow

After a controller action executes, the response flows back:

1. **Controller action returns** `IActionResult` (e.g., `Ok(result)`, `NotFound()`, `BadRequest()`)
2. **ASP.NET Core serializes** the return value to JSON
3. **HTTP response sent** back to frontend with:
   - Status code (200, 404, 400, etc.)
   - JSON body
   - Headers (content-type, etc.)

**ApiResponse wrapper**: LearnPath uses a custom `ApiResponse<T>` class that standardizes responses:
```csharp
// Ok response
ApiResponse<List<LearningPathResponseDto>>.Ok(result)
// Returns: { success: true, data: [...], message: "..." }

// Fail response  
ApiResponse<object>.Fail(ex.Message)
// Returns: { success: false, data: null, message: "Error message" }
```

### Validation

**Validation** ensures input data is correct before processing. LearnPath validates at multiple levels:

1. **Data Annotations on DTOs**: Attributes like `[Required]`, `[StringLength]`, `[Range]` on DTO properties
2. **Manual validation in services**: Checking business rules before operations
3. **Controller-level checks**: Authorized roles, ownership checks

**Examples from code**:
- Adding a module checks if title already exists:
  ```csharp
  var titleExists = await _context.Modules
      .AnyAsync(m => m.LearningPathId == pathId && m.Title == dto.Title);
  if (titleExists) throw new ArgumentException("A module with this title already exists.");
  ```
- Adding a dependency checks for cycles:
  ```csharp
  if (DagValidator.WouldCreateCycle(adjacency, dto.ModuleId, dto.DependsOnModuleId))
      throw new ArgumentException("Adding this dependency would create a cycle.");
  ```
- Module must have content before publishing:
  ```csharp
  var hasContent = !string.IsNullOrWhiteSpace(module.ContentBody)
      || !string.IsNullOrWhiteSpace(module.ContentUrl);
  if (!hasContent) throw new ArgumentException("At least one content item is required.");
  ```

If validation fails, an `ArgumentException` is thrown and caught by the middleware, returning a 400 Bad Request response.

### Exception Handling

**Exception handling** in LearnPath:

1. **Controller try-catch**: Some controllers have explicit try-catch blocks
2. **UnauthorizedAccessException**: Thrown when student tries locked module → returns 403 Forbidden
3. **KeyNotFoundException**: Thrown when resource not found → returns 404 Not Found
4. **ArgumentException**: Thrown for invalid input → returns 400 Bad Request
5. **Global exception handling**: Middleware can catch unhandled exceptions

**Example** (GetModuleContent in LearningPathController):
```csharp
try
{
    var result = await _service.GetModuleContentAsync(pathId, moduleId, UserId);
    return Ok(ApiResponse<ModuleResponseDto>.Ok(result));
}
catch (UnauthorizedAccessException ex)
{
    return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
}
```

### Important APIs

Here are the key API areas based on verified implementation:

#### Learning Paths
- `GET api/v1/paths/public` — get all published/public paths (AllowAnonymous)
- `GET api/v1/paths/my` — get paths created by current user (Auth required)
- `GET api/v1/paths/{id}` — get specific path with modules
- `POST api/v1/paths` — create new path (Admin/Instructor)
- `PUT api/v1/paths/{id}` — update path (Admin/Instructor)
- `DELETE api/v1/paths/{id}` — delete path (Admin only)

#### Modules
- `GET api/v1/paths/{pathId}/modules/{moduleId}` — get module content
- `POST api/v1/paths/{pathId}/modules` — add module (Admin/Instructor)
- `PUT api/v1/paths/{pathId}/modules/{moduleId}` — update module
- `DELETE api/v1/paths/{pathId}/modules/{moduleId}` — delete module (Admin)
- `PUT api/v1/paths/{pathId}/modules/{moduleId}/publish` — publish module
- `PUT api/v1/paths/{pathId}/modules/{moduleId}/dependencies` — add dependency
- `DELETE api/v1/paths/{pathId}/dependencies/{moduleId}/{dependsOnModuleId}` — remove dependency

#### Auth
- `POST api/v1/auth/login` — login, generate JWT token
- `POST api/v1/auth/register` — register new user
- `POST api/v1/auth/refresh` — refresh JWT token

#### Quizzes
- `GET api/v1/quizzes` — get quizzes
- `POST api/v1/quizzes` — create quiz
- `POST api/v1/attempts` — submit quiz attempt
- `GET api/v1/attempts/{id}` — get attempt details

#### Progress
- `GET api/v1/progress` — get progress data
- Progress tracks module completion per user

#### Classrooms
- `GET api/v1/classrooms` — get classrooms
- `POST api/v1/classrooms` — create classroom

**Note**: The resume mentions "analytics" but the project has basic progress/statistics functionality. The `AnalyticsController` exists but covers basic statistical data. Interview note: The resume mentions analytics, but the verified implementation currently covers basic progress/statistical functionality.

---

### Interview Answers (Phase 2)

**Q: Why EF Core?**
EF Core is used as the ORM to interact with SQL Server. It allows working with database tables as C# objects, reduces boilerplate code for CRUD operations, and handles migrations for schema changes. It integrates well with ASP.NET Core's dependency injection.

**Q: Why DTOs?**
DTOs are used to control what data is exposed via the API. They separate the internal database model (entities) from the API response shape. This prevents over-fetching (sending too much data) and under-fetching (not sending enough), and allows changing the database schema without breaking API clients.

**Q: How does a request reach the database?**
Frontend → HTTP request → ASP.NET Core router → Controller action → Service method → EF Core DbContext → SQL Server query → results returned through the same path back to frontend.

**Q: Why JWT?**
JWT (JSON Web Token) is used for stateless authentication. After login, the server generates a token that the frontend stores. Each subsequent request includes this token in the Authorization header. The backend validates the token to identify the user without looking up a session in the database. This scales well for web applications.

**Q: Why ASP.NET Core?**
ASP.NET Core provides a performant, cross-platform framework for building web APIs. It has built-in support for routing, middleware, dependency injection, authentication (including JWT), and authorization. It's the standard .NET choice for modern web APIs.

**Q: What validation is implemented?**
Validation happens at the DTO level (data annotations) and in the service layer (business rule checks). Common validations include: checking for duplicate titles, preventing cycle creation in the module dependency graph, ensuring required content exists before publishing, and role-based access control.

---

## Phase 3 — SQL Server and Authentication

### DATABASE: SQL Server

**SQL Server** is the relational database management system used by LearnPath. It stores all data: users, learning paths, modules, progress, dependencies, and more.

**Relational database** organizes data into tables with rows and columns. Relationships between tables ensure data integrity.

#### Tables (Entities)

The database has tables corresponding to the C# entities. Key tables:

| Table/Entity | Purpose |
|-------------|---------|
| `LearningPaths` | Learning paths created by instructors |
| `Modules` | Individual learning modules |
| `ModuleDependencies` | Graph dependencies between modules |
| `Progress` | Student progress through modules |
| `Users` | User accounts (via ASP.NET Identity) |
| `Classrooms` | Classroom groupings |
| `Quizzes` | Quiz assignments |
| `Certificates` | Issued certificates |

#### Primary Keys

Each table has a **primary key** — a unique identifier for each row:
- `LearningPaths`: `Id` (int)
- `Modules`: `Id` (int)
- `Users`: `Id` (string, from Identity)
- `Progress`: `Id` (int)
- `ModuleDependencies`: Composite key of `(ModuleId, DependsOnModuleId)`

Primary keys ensure each record is uniquely identifiable and are used in foreign key relationships.

#### Foreign Keys

**Foreign keys** link two tables together, enforcing referential integrity. When a row in one table references a row in another table, the foreign key ensures the referenced row exists.

**Examples in LearnPath**:
- `Modules.LearningPathId` → references `LearningPaths.Id` (each module belongs to a path)
- `Progress.UserId` → references `Users.Id` (each progress record belongs to a user)
- `Progress.ModuleId` → references `Modules.Id` (each progress record is for a module)
- `ModuleDependencies.ModuleId` → references `Modules.Id`
- `ModuleDependencies.DependsOnModuleId` → references `Modules.Id`

If someone tries to delete a learning path that has modules, the database prevents it (or cascades the delete, depending on configuration).

#### EF Core Entities

The C# entities define the table structure. Annotations map properties to columns:
- `[Key]` marks primary key properties
- Property names often become column names (convention-based)
- `Id` property becomes the primary key column
- Navigation properties (like `LearningPath`, `User`, `Module`) create foreign key relationships

#### Migrations

**Migrations** are how the database schema evolves. When entity classes change (new property, new table), a migration is created and applied:
- Add-Migration creates a new migration file
- Update-Database applies the changes to SQL Server

The project has a `Migrations` folder (`backend/Migrations/`) containing files like:
- `20260601XXXXX_InitialCreate.cs` — initial schema
- Subsequent migrations for additions/changes

Migrations allow version-controlled database changes. The `LearningPathService.DeleteAsync` method mentions: "SQL Server has EnableRetryOnFailure() enabled, so a user-initiated transaction must be executed through the context's execution strategy."

#### DbContext

**DbContext** (`ApplicationDbContext`) is the main point of interaction with the database. It:
- Tracks changes to entities
- Provides `DbSet<T>` properties for each table
- Manages database connections
- Executes queries and saves changes

In LearnPath, `ApplicationDbContext` is registered in the DI container and injected into services. Services use it to:
- Query: `_context.LearningPaths.Include(...).Where(...).ToListAsync()`
- Save: `await _context.SaveChangesAsync()`
- Execute raw SQL: `await _context.Database.ExecuteSqlRawAsync(...)`
- Create transactions: `await _context.Database.BeginTransactionAsync()`

### AUTHENTICATION: ASP.NET Identity + JWT

**Authentication** verifies who the user is. **Authorization** determines what they can do. LearnPath uses two layers:

1. **ASP.NET Identity** — handles user storage, passwords, roles
2. **JWT (JSON Web Token)** — handles stateless authentication per request

#### ASP.NET Identity

**ASP.NET Identity** is the membership system for:
- User storage in the database
- Password hashing and verification
- Login/logout functionality
- Role management

**User model** (`backend/Entities/User.cs`) extends `IdentityUser`:
```csharp
public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsSuperAdmin { get; set; }
    // ...
}
```

**Identity stores**:
- `Users` table: usernames, emails, password hashes, security stamps
- `Roles` table: role names (e.g., "Admin", "Instructor", "Student")
- `UserRoles` table: many-to-many link between users and roles
- `Tokens` table: password reset tokens, email confirmation tokens

**Role-based access**: Users are assigned roles. The system checks roles for API authorization:
- `Admin` — full access, can manage learning paths, users, classrooms
- `Instructor` — can manage their own learning paths, modules, students
- (Student role exists but may have limited API access)

#### JWT (JSON Web Token)

**JWT** is a token-based authentication method. It's stateless — the server doesn't store session data.

**How JWT works in LearnPath**:

1. **Login**: User sends credentials (username/password) → server validates → if correct, JWT is generated
2. **Token storage**: Frontend stores the JWT (typically in local storage or memory)
3. **Each request**: Frontend includes JWT in the `Authorization` header: `Authorization: Bearer <token>`
4. **Token validation**: Backend middleware validates the token on every protected request
5. **User identification**: From the validated token, the system knows which user is making the request

**JWT components**:
- **Header**: Token type and signing algorithm (`alg: HS256`)
- **Payload**: Claims (user ID, role, expiration, etc.)
- **Signature**: Verifies the token is from the legitimate server

**Login flow**:
```
Frontend: POST /api/v1/auth/login { username, password }
↓
AuthController.Login()
↓
Validate credentials against ASP.NET Identity UserStore
↓
If valid: Generate JWT with user ID and role claims
↓
Return token to frontend: { token: "eyJhbGci..." }
↓
Frontend: Store token, add to Authorization header on subsequent requests
```

**Token validation middleware** checks:
- Token is not expired (`exp` claim)
- Token signature is valid (signed with server secret)
- Required claims are present (e.g., `nameidentifier` = user ID)
- If validation fails: request is rejected with 401 Unauthorized

#### Complete Authentication Flow

**Login → Token → Protected Request**:

```
Step 1: User logs in
-----------------------------------------
Frontend:                       Backend:
POST /api/v1/auth/login         ↓
{ username: "student1",        Validate username/password
  password: "pass123" }         against ASP.NET Identity

                                    ↓
                                    Credentials valid?
                                   /          \
                                  Yes          No
                                   ↓            ↓
                            Generate JWT    Return 401
                            Return token    { message: "Invalid credentials" }

Step 2: Frontend stores token
-----------------------------------------
Frontend keeps the JWT token
(e.g., in localStorage or state)

Step 3: Frontend makes API request
-----------------------------------------
GET /api/v1/paths/my
Authorization: Bearer eyJhbGci...↑

Step 4: Backend validates token
-----------------------------------------
JWT Middleware checks:
1. Is the token expired? (exp claim vs current time)
2. Is the signature valid? (signed with app secret key)
3. Does it have required claims? (nameidentifier for user ID)

↓
If valid: Extract user ID from token, proceed with request
↓
If invalid: Return 401 Unauthorized

Step 5: Controller action runs
-----------------------------------------
- User ID from token is available via: User.FindFirstValue(ClaimTypes.NameIdentifier)
- Role checks: User.IsInRole("Admin") or similar
- Action executes with proper user context

Step 6: Response returns data
-----------------------------------------
JSON data + HTTP 200 OK
```

#### Protected API Requests

When a request includes a JWT, the backend:
1. Extracts the token from the `Authorization` header
2. Validates the token (expiration, signature, claims)
3. If valid, maps the claims to the `User` object (via ClaimsPrincipal)
4. The `[Authorize]` attribute on controllers/actions allows/denies access based on role
5. The `UserId` property in controllers gets the current user's ID from claims:

```csharp
private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
```

This `UserId` is then passed to services which filter data by user (e.g., `_.Where(p => p.CreatedById == userId)`).

#### Authorization

**Authorization** answers "what can this user do?" beyond just "who are they?".

LearnPath uses role-based authorization:
- `[Authorize]` — requires any authenticated user
- `[Authorize(Roles = "Admin,Instructor")]` — requires specific roles
- `[AllowAnonymous]` — no authentication required

**Example** (LearningPathController):
```csharp
[ApiController]
[Route("api/v1/paths")]
[Authorize] // ALL endpoints require authentication EXCEPT...
{
    [HttpGet("public")]
    [AllowAnonymous] // ...this one is public
    public async Task<IActionResult> GetPublic() { ... }
    
    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost] // create path requires Admin or Instructor
    public async Task<IActionResult> Create() { ... }
    
    [Authorize(Roles = "Admin")] // delete requires Admin only
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete() { ... }
}
```

#### Interview Answers (Phase 3)

**Q: Why SQL Server?**
SQL Server is used as the relational database for LearnPath. It provides strong data integrity with foreign key relationships, supports complex queries via T-SQL, and integrates well with Entity Framework Core. It's a standard choice for .NET applications and handles the structured data (users, paths, modules) efficiently.

**Q: What is ASP.NET Identity?**
ASP.NET Identity is the membership system that handles user authentication. It stores user accounts in the database, handles password hashing (so passwords aren't stored as plain text), and manages roles (Admin, Instructor, etc.). It's the standard .NET way to handle user accounts and login functionality.

**Q: How does JWT work?**
JWT (JSON Web Token) is a token-based authentication method. When a user logs in, the server generates a signed token containing the user's ID and role. The frontend stores this token and includes it in the `Authorization: Bearer <token>` header on each API request. The backend validates the token on every request — if valid, the user is identified and authorized; if invalid/invalid, the request is rejected with 401 Unauthorized. This is stateless — the server doesn't need to store session data.

**Q: What happens during login?**
1. User sends username and password to the login endpoint
2. The backend validates credentials against the ASP.NET Identity UserStore (which checks the hashed password in the database)
3. If credentials are correct, the server generates a JWT containing the user's ID and role claims, signed with a secret key
4. The token is returned to the frontend
5. The frontend stores the token and includes it in the `Authorization` header on subsequent requests
6. On each subsequent request, the backend middleware validates the token before the controller action runs

**Q: Why relational relationships?**
Relational relationships (foreign keys) ensure data integrity. For example, a module must belong to a valid learning path, and progress records must reference valid users and modules. The database enforces these constraints, preventing orphaned records and ensuring consistent data. EF Core models these relationships through navigation properties.

**Q: How does authorization work?**
Authorization in LearnPath is role-based. Users are assigned roles (Admin, Instructor) during registration or by an admin. API controllers use `[Authorize(Roles = "...")]` attributes to restrict access. When a request includes a valid JWT, the system extracts the user's role from the token claims and checks if it matches the required role for the endpoint. Admin endpoints are only accessible to users with the Admin role, instructor endpoints to Instructor role, and so on.

---

## Phase 4 — Architecture and Frontend/Backend Integration

### Overall Architecture

LearnPath follows a **3-layer web API architecture**:

1. **Presentation Layer** (Frontend: React)
   - UI components and pages
   - API calls to backend
   - State management (URL-based + local state)
   - User interactions

2. **Application Layer** (Backend: ASP.NET Core Web API)
   - Controllers — handle HTTP requests
   - Services — business logic
   - DTOs — data transfer objects
   - Authentication/authorization middleware

3. **Data Layer** (Database: SQL Server via EF Core)
   - DbContext — database interaction
   - Entities — C# classes mapped to tables
   - Migrations — schema management

**Request flow** always goes: Frontend → Backend API → EF Core → SQL Server, and responses return along the same path in reverse.

### Backend Structure

The backend (`/learnPath/backend`) is organized by feature area:

**Folders and their purpose**:
- `Controllers/` — HTTP endpoint handlers
  - `LearningPathController.cs` — path/module management
  - `AuthController.cs` — login/register
  - `QuizController.cs` — quiz functionality
  - `ProgressController.cs` — progress tracking
  - Other controllers for community, classroom, etc.

- `Services/` — business logic layer
  - `LearningPathService.cs` — path and module operations
  - `AuthService.cs` — authentication logic
  - `ProgressService.cs` — progress updates
  - Other service folders (Quiz, Attempt, etc.)

- `Repositories/` — data access patterns (generic + specific)
  - `GenericRepository.cs` — base repository operations
  - `CommunityRepository.cs` — specialized queries

- `DTOs/` — data transfer objects organized by feature
  - `LearningPath/` — path-related DTOs
  - `Auth/` — authentication DTOs
  - `Progress/` — progress DTOs
  - Other DTO folders

- `Entities/` — database model classes
  - `LearningPath.cs`, `Module.cs`, `User.cs`, etc.
  - `ModuleDependency.cs` — graph structure

- `Algorithms/` — graph algorithms
  - `Graph/DagValidator.cs` — cycle detection, topological sort

- `Data/` — EF Core configuration
  - `ApplicationDbContext.cs` — main database context
  - Migrations files
  - Configuration classes (EntityTypeConfiguration)

- `Mappings/` — EF Core entity configurations

- `Interfaces/` — service contracts
  - `ILearningPathService.cs`, `IAuthService.cs`, etc.

### Frontend Structure

The frontend (`/learnPath/frontend`) is a React application:

**Key folders in `src/`**:
- `components/` — reusable UI components
  - `PathExplorer/` — learning path navigation
  - `ModuleCard/` — individual module display
  - `QuizAttempt/` — quiz-taking interface
  - Layout components (Header, Sidebar, etc.)

- `pages/` — page-level components (route-based)
  - Path dashboard, module viewer, quiz page, login, register

- `redux/` — state management (if used)
  - `authSlice.tsx` — user authentication state
  - Other slices for progress, paths, etc.

- `types/` — TypeScript type definitions
  - `path.types.ts` — learning path types
  - `user.types.ts` — user types
  - `quiz.types.ts` — quiz-related types
  - Other type files

- `utils/` — utility functions
  - `graphUtils.ts` — graph operations for the frontend
  - `tokenUtils.ts` — JWT token handling
  - `dateUtils.ts`, `validationUtils.ts`, etc.

- `services/` — API service helpers
  - Functions that wrap `fetch()` or `axios` to call backend APIs
  - `apiService.ts` or similar — base URL, request helpers

- `theme/` — styling (MUI, custom themes)
- `test/` — test files for components and logic

### React Components and Pages

**Key React features in LearnPath**:

1. **Routing** — Using `react-router-dom`
   - Routes map URLs to components
   - Example: `/paths/:id` → Path dashboard component
   - Public routes (login, register) vs protected routes (paths, modules)

2. **State Management**
   - **Local state**: React `useState` for component-level data
   - **URL state**: Route parameters pass data between pages
   - **Global state**: Redux store for auth user, current path, progress
   - The `authSlice` manages login status, user token, and role

3. **API Services**
   - Frontend calls backend APIs via `fetch()` or `axios`
   - API service functions handle:
     - Setting the base URL (`api/v1/...`)
     - Adding the Authorization header with JWT token
     - Error handling for 401/403 responses
     - Parsing JSON responses

   **Example API service pattern**:
   ```javascript
   // apiService.js (simplified)
   const API_BASE = "/api/v1";

   export const getPaths = async () => {
     const response = await fetch(`${API_BASE}/paths/my`, {
       headers: { Authorization: `Bearer ${getToken()}` },
     });
     if (!response.ok) throw new Error("Failed to fetch paths");
     return response.json();
   };
   ```

4. **Protected Routes**
   - Before rendering a protected page, check if user is logged in
   - If not logged in, redirect to login page
   - Token stored in localStorage or memory is used for API auth

### Frontend → Backend Communication

**How frontend sends requests to backend**:

1. **Login authentication**
   - User enters credentials → frontend calls `POST /api/v1/auth/login`
   - Backend validates and returns JWT token
   - Frontend stores the token

2. **Authenticated API calls**
   - Each API request includes: `Authorization: Bearer <token>`
   - Token comes from frontend state/localStorage
   - Backend middleware validates the token
   - If valid, the request proceeds with the authenticated user context
   - If invalid (expired, wrong token), return 401 Unauthorized

3. **Common request pattern**:
   ```
   Frontend component → API service function → fetch() → 
   Backend API endpoint → Service layer → EF Core → SQL Server →
   Response back through same path
   ```

**Example**: Student starts a module
```
1. Student clicks "Start Module" on React component
2. Component calls API service: startModule(moduleId)
3. API service: fetch(`${API_BASE}/modules/{moduleId}/start`, {
       method: "POST",
       headers: { Authorization: `Bearer ${token}` }
     })
4. Backend controller receives request
5. Service validates: user has progress for this module, it's unlocked
6. EF Core checks progress and module status
7. Response: { success: true, moduleIsUnlocked: true }
8. Frontend: enables module content, shows "In Progress" state
```

### Backend → Database Communication

**How backend talks to SQL Server**:

1. **DbContext injection**: Services receive `ApplicationDbContext` via constructor injection
2. **EF Core queries**: Use LINQ to construct SQL queries
   - `_context.LearningPaths.Where(p => p.IsPublished).ToListAsync()`
   - `.Include()` for eager loading related data
   - `.ThenInclude()` for multiple levels of related data
3. **EF Core commands**: Save changes, add entities, etc.
   - `await _context.SaveChangesAsync()`
   - `await _context.Modules.AddAsync(module)`
4. **Transactions**: For atomic operations
   - `await _context.Database.BeginTransactionAsync()`
   - Commit or rollback based on success/failure
5. **Raw SQL** (when needed): For complex operations not easily expressed in LINQ
   - `await _context.Database.ExecuteSqlRawAsync("...")`

**Concrete example** (Get learning path with modules):
```csharp
// In LearningPathService.GetByIdAsync:
var path = await _context.LearningPaths
    .Include(p => p.CreatedBy)
    .Include(p => p.Modules.Where(m => includeUnpublished || m.IsPublished))
        .ThenInclude(m => m.Dependencies)
    .Include(p => p.Modules)
        .ThenInclude(m => m.Progresses.Where(pr => pr.UserId == userId))
    .Include(p => p.Modules)
        .ThenInclude(m => m.Resources)
    .FirstOrDefaultAsync(p => p.Id == id);
```

This single LINQ query generates SQL that joins `LearningPaths`, `Modules`, `ModuleDependencies`, `Progresses`, and `Resources` tables, returning all needed data in one database round-trip.

### Authentication Flow Between Frontend and Backend

**Complete login → protected flow**:

```
1. Login Page
   ↓
   User enters username + password → clicks "Login"
   ↓
   Frontend: fetch("POST /api/v1/auth/login", { 
         body: { username, password },
         headers: { "Content-Type": "application/json" } 
       })
   ↓
   Backend: AuthController.Login()
   ↓
   Validate credentials against ASP.NET Identity Users table
   ↓
   If valid:
     → Generate JWT signed with server secret key
     → Return: { token: "eyJhbGci...eyJ0eXAi..." }
   ↓
   If invalid:
     → Return: { message: "Invalid credentials" }, status 401

2. Frontend stores token
   ↓
   (saves to localStorage or Redux authSlice)

3. Protected page access
   ↓
   Component mounts → check authentication status
   ↓
   If no token or invalid → redirect to /login
   ↓
   If token exists → make API calls with header

4. API calls (every request)
   ↓
   fetch(url, { 
         headers: { Authorization: `Bearer ${token}` } 
       })
   ↓
   Backend JWT middleware validates token
   ↓
   If valid → extract user ID, role from claims
   ↓
   Controller action executes with UserId and role context
   ↓
   Response returns data
```

### Important Workflows

**1. Student starts learning path**
```
Frontend: Browse public paths → Click "Join/Start"
↓
GET api/v1/paths/public → get available paths
↓
GET api/v1/paths/my → (if already joined) see enrolled path
↓
Path dashboard renders with modules
↓
Module unlocking checked DAG + sequential rules
↓
Student starts first unlocked module
```

**2. Student completes module**
```
Student finishes module content → clicks "Complete"
↓
Frontend: POST api/v1/progress/{moduleId}/complete
↓
Service: Create/Update Progress record IsCompleted=true
↓
EF Core: SaveChangesAsync()
↓
Re-evaluate module unlocking for all dependent modules
↓
Frontend: Module status updates, next modules may unlock
↓
UI reflects progress
```

**3. Instructor creates learning path**
```
Instructor: Clicks "Create Path" in admin UI
↓
Frontend: POST api/v1/paths (with path DTO)
↓
Controller: Requires Admin/Instructor role
↓
Service: Validate input, create LearningPath entity
↓
EF Core: AddAsync, SaveChangesAsync()
↓
Return created path ID
↓
Frontend: Navigate to new path, add modules
```

**4. Instructor adds module dependency**
```
Instructor: Sets module A depends on module B
↓
Frontend: POST api/v1/paths/{pathId}/dependencies
       Body: { ModuleId: A, DependsOnModuleId: B }
↓
Service: DagValidator.WouldCreateCycle() check
↓
If cycle → return error: "Would create cycle"
↓
If OK → add ModuleDependency record
↓
EF Core: AddAsync, SaveChangesAsync()
↓
Return: "Dependency added successfully"
```

### How the Major Pieces Work Together

**Complete picture of LearnPath architecture**:

```
FRONTEND (React)                     BACKEND (ASP.NET Core)             DATABASE (SQL Server)
         │                                   │                                 │
         ├── HTTP Requests (GET/POST etc.)──│                                 │
         │                                   │                                   │
         │   Auth: Login → JWT Token           │   DI: Inject ApplicationDbContext │
         │                                   │                                   │
         │                                   ├── Controllers receive HTTP ──┤
         │                                   │   └──→ Services (business logic)│
         │                                   │       └──→ EF Core DbContext ──┤
         │                                   │           └──→ SQL Server Queries│
         │                                   │                 │               │
         │                                   │                 ▼               │
         │                                   │         Results │               │
         │                                   │                 │               │
         │                                   │                 ▼               │
         │                                   │           Response JSON ──┤
         │                                   │                 │               │
         │                                   │                 ▼               │
         │                                   │       Return to Frontend ──┘
         │                                   │
         └── JWT Token in Authorization Header────┘
```

**Key integration points**:
- **JWT token** flows from backend login → frontend storage → every API request header
- **User ID** extracted from token claims → used to filter data (user's paths, their progress)
- **Role claims** → authorize/deny API endpoints
- **EF Core** → translates C# code to SQL → stores/retrieves all data
- **Graph algorithms** → validate dependencies → ensure valid learning sequences
- **DTOs** → shape data between layers → avoid exposing raw entities

### Interview Answers (Phase 4)

**Q: What is the overall architecture?**
LearnPath uses a 3-layer architecture: Frontend (React) → Backend API (ASP.NET Core) → Database (SQL Server via EF Core). Requests flow from frontend to backend to database and back. The backend follows controller → service → EF Core pattern, with JWT authentication flowing from backend login to frontend storage to every request's Authorization header.

**Q: How does React communicate with ASP.NET Core?**
React calls the backend APIs using `fetch()` or `axios`, including a JWT token in the `Authorization: Bearer <token>` header on each request. The token is obtained during login and stored in frontend state/localStorage. The backend validates this token on each protected request to identify the user and check their role/permissions.

**Q: How does the backend communicate with the database?**
The backend uses Entity Framework Core's `ApplicationDbContext` to interact with SQL Server. Services inject the DbContext and use LINQ queries (with `.Include()` for related data) to fetch data, and `SaveChangesAsync()` to persist changes. EF Core translates the LINQ code to SQL queries that the database executes.

**Q: How does authentication flow between frontend and backend?**
1. User logs in via `POST /api/v1/auth/login` with credentials
2. Backend validates against ASP.NET Identity and generates a JWT token
3. Frontend stores the token and includes it as `Authorization: Bearer <token>` on each API request
4. Backend JWT middleware validates the token on each request, extracts user ID and role from claims
5. Controller actions use the authenticated user context to filter data and check permissions

**Q: How are the major pieces integrated?**
The JWT token is the glue — it flows from backend login response → frontend storage → Authorization header on every request → backend validation → user identification → role-based access control. EF Core handles all database communication. React components call APIs and update UI based on responses. The graph dependency system ensures valid module sequencing. DTOs shape data between each layer.

---

## Phase 5 — Interview Questions Summary

### Likely Interview Questions (All Phases)

Based on the verified implementation:

**About the project**:
1. "What is your project about?" — Graph-based learning platform with structured journeys
2. "What problem does it solve?" — Provides ordered learning paths with prerequisites, progress tracking
3. "Walk me through your project" — Start to finish explanation
4. "What was your role?" — Fresher answer: designed modules, APIs, graph dependencies, integrated React frontend

**About technologies**:
5. "Why did you use EF Core?" — ORM for SQL Server, reduces boilerplate, handles migrations
6. "Why JWT?" — Stateless authentication, scales well, no server-side sessions
7. "Why ASP.NET Core?" — Performant web API framework, built-in auth/authorization
8. "Why React?" — Modern UI, component-based, good ecosystem, integrates with .NET APIs
9. "Why SQL Server?" — Relational data, strong integrity, standard .NET choice

**About the graph/DAG**:
10. "How does your DAG work?" — Modules have dependencies, cycle prevention, unlocking based on completed prerequisites
11. "How does progress work?" — Progress records track completion, re-evaluate unlocking after each completion
12. "What happens if a student tries to access a locked module?" → 403 Forbidden with message

**About APIs**:
13. "How does a request reach the database?" → Frontend → Controller → Service → EF Core → SQL Server
14. "Why do you use DTOs?" — Separate API shape from database entity, control data exposure
15. "What validation do you implement?" — DTO data annotations + service business rules (duplicate checks, cycle prevention, content requirements)

**About authentication**:
16. "What happens when a user logs in?" → Credentials validated → JWT generated → token returned → stored by frontend → included in Authorization header on subsequent requests
17. "How does authorization work?" → Role-based, JWT claims, `[Authorize(Roles = "...")]` attributes on controllers

**About specific features**:
18. "How do students move through learning paths?" → Modules unlocked via DAG dependencies + sequential ordering, progress tracking determines what's next
19. "What happens when a quiz is attempted?" → Quiz attempt recorded → answers saved → progress updated → result returned
20. "How are assignments managed?" → Classrooms, assignments linked to learning paths

Simple, honest answers based on actual code are better than invented technical details.

---
*Documentation complete across all phases.*