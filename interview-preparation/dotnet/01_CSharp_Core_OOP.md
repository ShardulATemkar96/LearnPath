# Phase 1 — C# Core + OOP (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — good to mention, never spend conference time here

LearnPath facts used below are verified from source. Anything not found in
the project is clearly labelled as a general concept.

---

## 1. What is C#? 🔥

### What is it?
C# is a modern, object-oriented, statically-typed programming language created by
Microsoft, mainly used to build .NET applications (Web APIs, desktop, games, cloud).

### Why is it used?
It gives strong type safety, a large standard library, cross-platform support via
.NET, and first-class tooling (Visual Studio / VS Code). It is the primary language
for building ASP.NET Core backends.

### Interview Answer
"C# is a modern object-oriented programming language developed by Microsoft. It is
statically typed, meaning the compiler checks types before the code runs, which
catches many bugs early. I used it to build the whole LearnPath backend — the
controllers, services, and entities are all written in C#."

### How it works
C# source (.cs) is compiled by the C# compiler to Intermediate Language (IL), which
the .NET runtime (CLR) compiles to machine code at run time (JIT). The compiler is
the first safety gate that catches type errors before the program runs.

### LearnPath Example
Every backend file ends in `.cs` — e.g. `backend/Program.cs`, `backend/Controllers/AuthController.cs`,
`backend/Entities/User.cs`.

### Common Mistake
- Saying C# is the same as .NET. C# is the *language*; .NET is the *runtime/platform*.
- Saying C# only works on Windows — it is cross-platform today.

### Follow-Up Questions
Q: Is C# compiled or interpreted?
A: Compiled to IL first, then JIT-compiled to machine code at run time. There is also
ahead-of-time (Native AOT) option in modern .NET.

Q: What is the entry point of a C# program?
A: The `Main` method (or top-level statements, which is what `Program.cs` uses in
ASP.NET Core).

### Remember
- C# = language. .NET = platform. Compiler checks types before run time.

---

## 2. Value types vs reference types 🔥

### What is it?
Value types store their data directly; reference types store a *reference* (address)
to data stored elsewhere (heap). `int`, `bool`, `double`, `struct`, `enum` are value
types. `class`, `string`, `array`, `interface` instances are reference types.

### Why is it used?
It decides how memory is handled: value types live on the stack, reference types on
the heap and are managed by the garbage collector. It also affects how data behaves
when copied.

### Interview Answer
"Value types store the actual value directly, and reference types store an address
that points to the object. The key practical difference is assignment: a value type
is copied, a reference type shares the same object. So changing a copied reference
changes the original, but changing a copied value type does not."

### Simple Example
```csharp
int a = 5;
int b = a;   // b gets a COPY → changing b does not change a

var list1 = new List<int> { 1, 2, 3 };
var list2 = list1;        // list2 points to the SAME object
list2.Add(99);            // list1 now also has 99
```

### How it works
Value types (structs, primitives) are usually allocated on the stack and copied by
value. Reference types are allocated on the managed heap; the stack/field holds only
the reference. Garbage collection only watches the heap.

### LearnPath Example
DTOs like `LoginRequestDto` and entities like `Module` are classes (reference types).
IDs like `int Id` and flags like `IsPublished`, `Status`/`Difficulty` fields are value
types. In `LearningPathService.CreateAsync`, `path.IsPublic = dto.IsPublic` copies the
bool value (value type), while the `path` object itself is a reference.

### Common Mistake
- Thinking `string` is a value type — it is a reference type (but immutable).
- Not realizing two class variables can point to the same object.

### Follow-Up Questions
Q: Where are value and reference types stored?
A: Value types are typically on the stack (can be on heap inside a class), reference
types on the heap.

Q: Is `string` value or reference type?
A: Reference type, but immutable — every change creates a new string.

### Remember
- Copy = value copies value; class copies the reference, not the object.

---

## 3. Variables and data types 🟡

### What is it?
Variables store data and have a type that the compiler enforces. Common built-in
types: `int`, `long`, `double`, `decimal`, `bool`, `char`, `string`, `DateTime`.

### Why is it used?
Static typing prevents many bugs before runtime and makes code self-documenting.

### Interview Answer
"A variable is a named storage for a value with a fixed type. C# is strongly typed,
so I declare the type or use `var` and let the compiler infer it. Numeric types like
`int` or `decimal`, `bool`, `string`, and `DateTime` cover most needs. For money, C#
provides `decimal`, which avoids floating point errors."

### Simple Example
```csharp
int count = 5;
decimal price = 19.99m;      // 'm' suffix = decimal
bool isPublic = true;
string title = "C# Basics";
DateTime now = DateTime.UtcNow;
var id = 42;                  // compiler infers int
```

### How it works
`var` is not untyped — the compiler infers the type at compile time and treats it as
that exact type. Runtime type checks still enforce correctness.

### LearnPath Example
`Module` (`backend/Entities/Module.cs`) uses many: `int Order`, `bool IsDraft`,
`ModuleDifficulty Difficulty` (enum — value type), `DateTime? ArchivedAt`
(nullable), `string? ContentUrl`.

### Common Mistake
- Using `double` for money — `decimal` is correct for financial values.
- Thinking `var` is dynamic — it is strongly inferred.

### Keep
- `var` = compiler infer, still static. Use `decimal` for money.

---

## 4. Classes and objects 🔥

### What is it?
A class is a blueprint/template describing the shape of an object (its data and
behavior). An object is an instance created from that blueprint using `new`.

### Why is it used?
It lets you model real-world things (a LearningPath, a User) as reusable units of
data + logic, which is the heart of OOP.

### Interview Answer
"A class is a blueprint that defines the properties and methods an object will have.
An object is an actual instance of that blueprint created with `new`. In LearnPath
the `User`, `Module`, and `LearningPath` classes are blueprints, and each row loaded
from the database becomes a separate object."

### Simple Example
```csharp
class LearningPath
{
    public int Id { get; set; }
    public string Title { get; set; }
}

var path = new LearningPath { Title = "C# for Beginners" }; // object
```

### LearnPath Example
Entities in `backend/Entities/` are plain classes (e.g. `LearningPath.cs`,
`Module.cs`). EF Core maps each class to a database table and each object to a row.

### Common Mistake
- Confusing class (blueprint) with object (instance).
- Forgetting `new` and trying to use a class type directly as if it had data.

### Follow-Up Questions
Q: Can a class have no constructor?
A: Yes — the compiler gives a default parameterless constructor automatically.

### Remember
- Class = blueprint, object = `new` instance of that blueprint.

---

## 5. Constructors 🟡

### What is it?
A special method with the same name as the class, called automatically when an object
is created, used to set initial values / required dependencies.

### Why is it used?
To guarantee an object is valid from the moment it is created, and (in DI) to receive
dependencies.

### Interview Answer
"A constructor runs automatically when an object is created. Its job is to put the
object into a valid starting state. In LearnPath, the most common use is constructor
injection — controllers and services receive their dependencies through the
constructor, like `AuthController` receiving `IAuthService`."

### Simple Example
```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)  // constructor injection
    {
        _authService = authService;
    }
}
```

### LearnPath Example
`backend/Controllers/AuthController.cs:16-19` — constructor takes `IAuthService`.
`backend/Middleware/ExceptionMiddleware.cs` — constructor takes `RequestDelegate` and
`ILogger`. The DI container creates these and passes the instances in.

### Common Mistake
- Not understanding that the DI container calls the constructor; you rarely write
`new` for services you receive.
- Forgetting to assign fields, leaving them null.

### Follow-Up Questions
Q: What happens if a class has no constructor?
A: Compiler adds a default parameterless one.

Q: Can two constructors exist?
A: Yes — through overloading (different parameter lists).

### Remember
- "Constructor = required setup at birth." In this project, it is DI injection.

---

## 6. Access modifiers 🔥

### What is it?
Keywords that control who can see/use a member: `public`, `private`, `protected`,
`internal`.

### Why is it used?
To enforce encapsulation — expose a clean public API while hiding internals so other
code cannot break the object's state.

### Interview Answer
"Access modifiers control visibility. `public` is visible everywhere, `private` only
inside the same class, `protected` is visible in the class and its subclasses, and
`internal` is visible within the same assembly. I use `private readonly` fields to
hide dependencies and `public` for the methods other code should call."

### Simple Example
```csharp
public class Calculator
{
    private int _total;          // hidden
    public void Add(int n)       // visible
    {
        _total += n;
    }
    public int Total => _total;
}
```

### LearnPath Example
`backend/Services/LearningPath/LearningPathService.cs` — public service methods, plus
`private` helpers `GetOwnedPathAsync`, `GetModuleAsync`, `MapToResponse`.
`backend/Repositories/GenericRepository.cs` — `protected readonly` context/dbSet so
subclasses can use them.

### Common Mistake
- Making everything `public` "just in case" — breaks encapsulation.
- Confusing `protected` (class + subclasses) with `public`.

### Follow-Up Questions
Q: Difference between `protected` and `internal`?
A: `protected` = current class + derived classes; `internal` = everything in same
assembly.

### Remember
- `private` hides, `public` exposes, `protected` extends to children.

---

## 7. Properties 🔥

### What is it?
Properties look like public fields but are methods underneath (`get`/`set` accessors)
that let you control how values are read or written. `{ get; set; }` is the common
auto-implemented form.

### Why is it used?
It gives the safety of methods (validation, computed values) with the readability of
fields, and supports `init`, `private set`, computed getters, etc.

### Interview Answer
"A property is a field with controlled access. Instead of exposing `_title` directly,
the class exposes `Title` with `get` and `set` accessors. If the logic is simple I use
auto-properties — `{ get; set; }` — which is what my entities and DTOs use. I can also
add logic, like computing a field or rejecting a bad value on set."

### Simple Example
```csharp
public class UserProfile
{
    public string FirstName { get; set; } = string.Empty;   // auto-property
    public string FullName => $"{FirstName} {LastName}";     // computed getter
    public string LastName { get; set; } = string.Empty;
}
```

### LearnPath Example
All entities and DTOs use auto-properties, e.g. `User.cs`, `LoginRequestDto.cs`,
`Module.cs` (`public int Id { get; set; }`).

### Common Mistake
- Saying "property is a variable" — it is a method pair (accessors) underneath.

### Follow-Up Questions
Q: `get` vs `set` meaning?
A: `get` reads (returns the value), `set` writes (assigns via `value`).

Q: What does `=>` in a property mean?
A: Expression-bodied computed property — returns the expression each time.

### Remember
- Auto-property = `{ get; set; }`; a property can contain logic.

---

## 8. Encapsulation 🔥

### What is it?
Encapsulation = data hiding. The object's internal state is kept private and is only
changed through public methods/properties, protecting the object from invalid states.

### Why is it used?
It hides complexity, prevents misuse, and keeps related data + behaviour together
(data + methods in one class).

### Interview Answer
"Encapsulation means hiding the internal details of a class and only exposing what is
necessary through public members. It is the theory behind using `private` fields and
public methods — the outside world cannot corrupt the object's state. In my services
and middleware, dependencies are `private readonly` fields, and the rest of the app
only calls the public methods."

### Simple Example
```csharp
public class Account
{
    private decimal _balance;
    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Must be positive.");
        _balance += amount;
    }
}
```

### LearnPath Example
`ApplicationDbContext.GuardAuditLogImmutability()` (`backend/Data/ApplicationDbContext.cs`)
is a `private` method invoked inside `SaveChanges` — it prevents audit logs from being
edited. That logic is encapsulated inside the context. Services also hide `_context`
and helper logic (`private` methods) from controllers.

### Common Mistake
- Breaking encapsulation by making fields public or using everything `public`.

### Follow-Up Questions
Q: How is it related to access modifiers?
A: Access modifiers are the tool that implements encapsulation.

### Remember
- Encapsulation = "protect the state, expose the API." Access modifiers implement it.

---

## 9. Inheritance 🔥

### What is it?
A class can inherit members (properties/methods) from a base class using `:`, reusing
parent behaviour and extending it. C# supports single class inheritance (one base).

### Why is it used?
To remove duplication ("is-a" relationship) and to let derived classes share common
behaviour while adding their own.

### Interview Answer
"Inheritance lets a class reuse members from a parent class using the `:` symbol.
C# supports single inheritance — a class can have one base class but many interfaces.
In LearnPath, `User` inherits from ASP.NET Core Identity's `IdentityUser`, so we get
username, password hashing, and security features for free, and I add custom fields
on top."

### Simple Example
```csharp
public class User : IdentityUser   // User IS-A IdentityUser
{
    public string FirstName { get; set; }
}
```

### LearnPath Example
`backend/Entities/User.cs` — `public class User : IdentityUser`.
`backend/Data/ApplicationDbContext.cs` — `: IdentityDbContext<User>`.
Also `RegisterRequestValidator : AbstractValidator<RegisterRequestDto>` inherits the
base FluentValidation class.

### Common Mistake
- Using inheritance for "has-a" relationships (use composition: a field of that type).

### Follow-Up Questions
Q: Why does `User` inherit `IdentityUser`?
A: To get Identity's built-in login/password/role management for free and extend it
with custom profile fields.

### Remember
- Inheritance = "is-a" relationship, one base class allowed in C#.

---

## 10. Polymorphism 🔥

### What is it?
"Many forms" — the same method call behaves differently depending on the actual
object type. Achieved via method overriding (runtime) and via interfaces.

### Why is it used?
It lets you write code against abstractions (base class/interface), and the correct
implementation runs automatically for the concrete object.

### Interview Answer
"Polymorphism means the same name can have different behaviours depending on the
actual object. In C# the main forms are method overriding with `virtual`/`override`
and programming against interfaces. For example, my controllers depend on interfaces
like `IAuthService`, and because of polymorphism, the real `AuthService` runs at
runtime — the controller never needs to know which implementation it is."

### Simple Example
```csharp
public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
// Two different implementations can be swapped without changing the caller.
```

### LearnPath Example
Runtime polymorphism: middleware `ExceptionMiddleware` matches different exception
types in a switch expression (`ArgumentException` → 400, `UnauthorizedAccessException`
→ 403, etc.) — same "handle exception" idea, different behaviour per type.
Interface-based polymorphism: `AddScoped<IAuthService, AuthService>()` in `Program.cs`
means code depending on `IAuthService` gets `AuthService` at runtime.

### Common Mistake
- Describing only one kind of polymorphism (compile-time vs runtime).

### Follow-Up Questions
Q: Two types of polymorphism in C#?
A: Compile-time (overloading) and runtime (overriding/interfaces).

### Remember
- Polymorphism = one name, many behaviours; code against interfaces.

---

## 11. Abstraction 🔥

### What is it?
Abstraction = simplifying complexity by exposing only essential details and hiding
implementation. Interfaces and abstract classes are its main tools.

### Why is it used?
The caller works with "what it does" (contract), not "how it's done". This enables
swapping implementations, mocking in tests, and low coupling.

### Interview Answer
"Abstraction means showing only what matters and hiding how it works. An interface is
the purest form: it lists what operations exist without any implementation. In
LearnPath, controllers depend on `IAuthService`, `IUserService` etc. — the interfaces
are the contract, and the concrete services are the implementation hidden behind
them."

### Simple Example
```csharp
public interface IUserService
{
    Task<UserProfileResponseDto> GetProfileAsync(string userId);
}
```

### LearnPath Example
`backend/Interfaces/Services/` contains `IAuthService`, `IUserService`,
`ILearningPathService` etc. Each service class (`backend/Services/...`) implements
its interface. DI wires them together.

### Common Mistake
- Using abstraction/interfaces when there will always be only one implementation and
no need to mock/test — over-engineering.

### Remember
- Abstraction = hide "how", expose "what". Interfaces are the main tool.

---

## 12. Interface 🔥

### What is it?
A contract that declares members (methods/properties) with no implementation. A class
implements it by providing the actual code.

### Why is it used?
To define capabilities that implementations must provide, enabling polymorphism,
testability, and decoupling. A class can implement many interfaces.

### Interview Answer
"An interface is a contract: it declares what a class must do, but not how. It only
contains signatures, no implementation. A class can implement multiple interfaces.
In LearnPath, `IAuthService` declares `RegisterAsync`, `LoginAsync`, etc., and
`AuthService` implements them. Everything else depends on the interface, which makes
testing easy — I can swap in a fake implementation."

### Simple Example
```csharp
public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}

public class AuthService : IAuthService
{
    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto) { ... }
}
```

### LearnPath Example
`backend/Interfaces/Services/IAuthService.cs`, `ILearnPathService`, plus
`IGenericRepository<T>` in `backend/Interfaces/Repositories/`. EF Core also uses the
interface `IEntityTypeConfiguration<T>` for entity configs
(`backend/Configurations/LearningPathConfiguration.cs`).

### Common Mistake
- Putting implementation/fields inside an interface (they are signatures only).

### Follow-Up Questions
Q: Can an interface have properties?
A: Yes — but they are declarations; the implementing class provides the
implementation.

Q: Can a class implement multiple interfaces?
A: Yes, comma-separated in the class declaration.

### Remember
- Interface = contract, no implementation, multiple allowed.

---

## 13. Abstract class 🔥

### What is it?
A class that cannot be instantiated directly, meant only to be inherited. It can
contain implemented methods AND abstract (unimplemented) methods.

### Why is it used?
To share common logic among related classes while forcing subclasses to provide
specific parts.

### Interview Answer
"An abstract class is a base class you cannot instantiate directly — it is meant to
be inherited. Unlike an interface, it can contain real implemented code, plus abstract
methods that subclasses must implement. So it shares common logic and leaves the
specific parts to the child class."

### Simple Example
```csharp
public abstract class Validator<T>   // cannot be `new`'d
{
    public void ValidateAndThrow(T value) { ... }   // shared logic
    protected abstract void Validate(T value);       // child must implement
}
```

### LearnPath Example
LearnPath does not define its own abstract class, but it *inherits* from one:
`RegisterRequestValidator : AbstractValidator<RegisterRequestDto>` (FluentValidation)
in `backend/Validators/Auth/RegisterRequestValidator.cs`. The abstract base provides
the `RuleFor`/`WithMessage` API; the subclass declares the actual validation rules.

### Common Mistake
- Trying to instantiate an abstract class — compile error.
- Making a class abstract when only one subclass will ever exist.

### Follow-Up Questions
Q: Does an abstract class need to have abstract methods?
A: No — it may have only concrete members, but it still cannot be instantiated.

### Remember
- Abstract class = cannot be `new`'d, can mix implemented + abstract members.

---

## 14. Interface vs abstract class 🔥

### What is it?
Both define contracts, but: interface = pure contract (no implementation, one class
can implement many); abstract class = shared base with possible implementation (single
inheritance).

### Why is it used?
The interviewer wants to know if you understand *when* to pick each.

### Interview Answer
"The difference is: an interface is a pure contract with no implementation, and a
class can implement multiple interfaces. An abstract class is a base class that can
have shared implementation and is limited to single inheritance. Interfaces are great
for capabilities and decoupling, abstract classes are great for sharing code between
closely related classes. In my project I heavily use interfaces for services, and I
use FluentValidation's abstract `AbstractValidator` when I need inherited rule
building behaviour."

### Simple Example
```csharp
public interface IDrive { void Drive(); }   // pure contract
public abstract class Vehicle                  // shared base with implementation
{
    public void StartEngine() { ... }          // shared code
}
```

### LearnPath Example
Interface: `IAuthService` (multiple services each implement one). Abstract class:
FluentValidation's `AbstractValidator<T>` — validators inherit it and add rules.

### Common Mistake
- Saying "interfaces are contracts, abstract classes can have methods" without saying
*multiple inheritance* and *shared implementation* are the deciding factors.

### Follow-Up Questions
Q: When to use abstract class vs interface?
A: Shared code among related classes → abstract class. Capability/contract that many
unrelated classes implement → interface.

Q: Can an abstract class implement an interface?
A: Yes.

### Remember
- Interface = many-to-one, no code. Abstract class = one-to-one hierarchy, may have code.

---

## 15. Method overloading 🟡

### What is it?
Multiple methods with the same name in one class but different parameters (count or
types). The compiler picks the right one from the call.

### Why is it used?
For convenience — one logical operation with different input forms (also known as
compile-time polymorphism).

### Interview Answer
"Method overloading is two or more methods with the same name in one class that
differ in parameter count or types. The compiler decides which one to call. It is a
form of compile-time polymorphism."

### Simple Example
```csharp
public int Add(int a, int b) => a + b;
public int Add(int a, int b, int c) => a + b + c;
```

### LearnPath Example
Not a significant custom pattern in the codebase, but note `Program.cs` uses the
same underlying idea with method groups like `RoleSeeder.SeedAsync(...)` vs
`AdminSeeder.SeedAsync(...)` (same name, different types across classes). Be honest:
state it's mostly a general concept in your project.

### How it works
Overloads must differ in parameter list only — the return type alone cannot
distinguish them.

### Common Mistake
- Believing return type can differentiate overloads — it cannot.

### Remember
- Overloading = different parameters, compile-time choice, return type not counted.

---

## 16. Method overriding 🟡

### What is it?
A derived class re-implements a method inherited from its base (using `override` over
a `virtual` base method). This is runtime polymorphism.

### Why is it used?
To replace or extend behaviour of the inherited method with the child's own logic.

### Interview Answer
"Method overriding is when a derived class replaces an inherited method's
implementation. The base method must be declared `virtual` and the child declares
`override`. At runtime, the child's version is called even through a base-type
reference. It is runtime polymorphism."

### Simple Example
```csharp
public class ValidationResult { }
```

### LearnPath Example
`backend/Data/ApplicationDbContext.cs` overrides `SaveChanges`, `SaveChangesAsync`,
and `OnModelCreating` (marked `protected override`). EF Core migration classes override
`Up` and `Down` (`backend/Migrations/20260615050934_InitialCreate.cs`).

### Common Mistake
- Forgetting the base method must be `virtual` (or `abstract`) to be overridable.

### Remember
- Overriding = same signature, new body, needs `virtual` base.

---

## 17. virtual / override / new 🔥

### What is it?
`virtual` allows a derived class to override a method. `override` replaces the parent
implementation. `new` hides the parent method without true polymorphism (a caution sign).

### Why is it used?
`virtual` is the "permission" for runtime polymorphism; `override` is the act of
changing the behaviour.

### Interview Answer
"`virtual` marks a method as overridable, `override` re-implements it in the child
class, and `new` merely hides the base method so it does not participate in true
polymorphism. The rule I follow: if I need extensibility, mark the base `virtual` and
use `override` in the child."

### Simple Example
```csharp
public class DbContext
{
    public virtual int SaveChanges() => 0;
}
public class ApplicationDbContext : DbContext
{
    public override int SaveChanges() => Guard();  // replaces base logic
}
```

### LearnPath Example
`ApplicationDbContext.SaveChanges` is `override` (base `SaveChanges` is virtual in
EF Core's `DbContext`). Migrations override `Up`/`Down`.

### Common Mistake
- Using `new` when you meant `override` — silently breaks polymorphic calls.

### Remember
- virtual = permission, override = replacement, new = hiding (avoid).

---

## 18. static 🔥

### What is it?
`static` members belong to the type, not to any instance. `static` methods/fields are
shared across all instances; they are accessed via the class name.

### Why is it used?
For utility methods and values that do not depend on an object's state.

### Interview Answer
"A `static` member belongs to the class, not to any instance — I call it through the
class name without creating an object. Static methods are for pure utilities; static
fields share one value across all instances. For example, my `ApiResponse<T>` exposes
`Ok` and `Fail` as static factory methods."

### Simple Example
```csharp
var response = ApiResponse<AuthResponseDto>.Ok(result, "Login successful.");
```

### LearnPath Example
`backend/Common/ApiResponse.cs` — `public static ApiResponse<T> Ok(...)` /
`Fail(...)`. `backend/Algorithms/Graph/DagValidator.cs` — `public static class`
with static helper methods. `backend/Data/Seeders/RoleSeeder.cs` — static `SeedAsync`.

### How it works
One memory slot per static member per type; no `this` available inside — static
methods cannot access instance members.

### Common Mistake
- Saying `static` means the value never changes — it means it is shared per type,
not immutable.

### Follow-Up Questions
Q: Can a static method access instance fields?
A: No — no instance exists.

### Remember
- static = per type, not per object. Good for utilities and shared constants.

---

## 19. sealed 🟡

### What is it?
`sealed` prevents a class from being inherited (or a member from being overridden).

### Why is it used?
To stop further extension — protects behavior and gives runtime/performance benefits.

### Interview Answer
"`sealed` means the class cannot be used as a base class — no one can inherit it. I
would seal a class when I never want its behaviour extended, which also lets the JIT
optimize calls."

### Simple Example
```csharp
public sealed class JwtSettings { ... }  // cannot be inherited
```

### LearnPath Example
Not used in the LearnPath backend (verified by search). State it as a general concept:
sealing protects your class from incorrect or unnecessary inheritance.

### Common Mistake
- Overusing `sealed` early, making code harder to test or extend later.

### Remember
- sealed = "don't extend me". Not found in LearnPath; still know the keyword.

---

## 20. const vs readonly 🟡

### What is it?
`const` = compile-time constant (value burned into IL, must be a literal, effectively
static). `readonly` = runtime constant (assigned once — field initializer or
constructor — then never changed). Fields can be `static readonly`.

### Why is it used?
`const` for true constants (max retries); `readonly` for values you know only at
runtime (injected dependencies).

### Interview Answer
"`const` is a compile-time constant — the compiler substitutes the value, so it must
be a literal. `readonly` is a runtime constant — assigned in the constructor or field
initializer, then never changed. The practical one I use daily is `private readonly`
for injected dependencies."

### Simple Example
```csharp
private const int MAX_REQUESTS = 50;                 // compile-time literal
private readonly RequestDelegate _next;               // set once in constructor
private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);  // runtime value
```

### LearnPath Example
`backend/Middleware/RateLimitingMiddleware.cs` — `private const int MAX_REQUESTS = 50;`
and `private static readonly TimeSpan WINDOW = ...`. Constructor-injected
`private readonly RequestDelegate _next;` everywhere in middleware and services.

### Common Mistake
- Trying to use `const` with `new` or non-literal values (compile error) — use
`static readonly` instead.

### Follow-Up Questions
Q: When is a `readonly` field set?
A: Field initializer or constructor; after that it cannot be reassigned.

### Remember
- const = literal at compile time; readonly = once at runtime (constructor).

---

## 21. Exception handling 🔥

### What is it?
Exceptions are runtime errors represented as objects (`ArgumentException`,
`UnauthorizedAccessException`, etc.). Handling = catching, reacting, and responding
without crashing the whole app.

### Why is it used?
Failures are expected in real apps (bad input, DB down, invalid token). You want the
user to get a useful error and the app to keep running.

### Interview Answer
"An exception is an object thrown when something goes wrong at runtime. We catch them
to respond gracefully — return a proper HTTP error — instead of crashing. The key
idea is to throw precise exception types and centralize the handling so controllers
stay clean."

### Simple Example
```csharp
if (path.CreatedById != userId)
    throw new UnauthorizedAccessException("You do not own this learning path.");
```

### LearnPath Example
Services throw typed exceptions: `ArgumentException`, `UnauthorizedAccessException`,
`KeyNotFoundException`, `InvalidOperationException` (e.g. `LearningPathService`
ownership checks). The centralized `ExceptionMiddleware`
(`backend/Middleware/ExceptionMiddleware.cs`) catches everything and maps each type to
a status code and a JSON `ApiResponse` message.

### How it works
```text
throw → bubbles up the call stack → caught by try/catch or by ExceptionMiddleware →
mapped to HTTP status + JSON body
```

### Common Mistake
- Catching `Exception` everywhere instead of letting a global handler do it.
- Swallowing exceptions (`catch { /* nothing */ }`) — hides bugs.

### Follow-Up Questions
Q: What does `throw` do?
A: Creates/raises the exception and stops normal flow; it travels up until caught or
the process stops.

### Remember
- Throw specific types, handle once globally. Never swallow silently.

---

## 22. try / catch / finally 🔥

### What is it?
`try` wraps code that may throw. `catch` handles a specific exception type. `finally`
always runs — with or without an exception — usually for cleanup.

### Why is it used?
To do something about the error (catch) and guarantee cleanup (finally).

### Interview Answer
"`try` contains risky code. `catch` runs when a matching exception is thrown and lets
me react. `finally` always runs whether there was an error or not, so it is perfect
for cleanup. In my API, `AuthController` catches `UnauthorizedAccessException` on
login and returns 401, and the global middleware is the last safety net."

### Simple Example
```csharp
try
{
    var result = await _authService.LoginAsync(dto);
    return Ok(result);
}
catch (UnauthorizedAccessException ex)
{
    return Unauthorized(ApiResponse<object>.Fail(ex.Message));
}
```

### LearnPath Example
`backend/Controllers/AuthController.cs:32-42` — try/catch on `Login` and `Refresh`
returns `Unauthorized`. `ExceptionMiddleware` uses try/catch around the whole
pipeline. The project uses `await using var transaction` / `using` instead of manual
`finally` for disposal.

### Common Mistake
- Not knowing `finally` runs even if `catch` is present or a `return` happened.
- Catching too narrowly so unexpected errors still crash the request.

### Follow-Up Questions
Q: Can `finally` execute without `catch`?
A: Yes — `try/finally` is valid; cleanup still runs.

Q: Does `finally` run if `return` is inside `try`?
A: Yes, `finally` always runs before control leaves the method.

### Remember
- finally = guaranteed cleanup. One global catch + optional local catches.

---

## 23. throw vs "throws" (Java comparison) ⚪

### What is it?
C# has `throw` (raise an exception) and `throw;` (re-throw preserving stack).
C# has NO `throws` keyword like Java — the method signature never declares exceptions.

### Why is it used?
It changes how you design error handling: C# relies on documentation/global handling
rather than compile-time "throws" declarations.

### Interview Answer
"C# has `throw` to raise an exception, and inside a catch I can use plain `throw;` to
rethrow original exception and preserve the stack trace. C# does not have Java's
`throws` clause — methods don't declare what they throw. That's why a global exception
handler like my `ExceptionMiddleware` is so important in .NET."

### What is it? (first answer)
Structure the answer as the direct answer above.

### Common Mistake
- Saying "throws" or `throw new Exception()` for everything — the base `Exception`
should be avoided; throw specific types.

### Remember
- `throw` = raise/rethrow; no `throws` in C#; use a global handler.

---

## 24. Important C# interview basics 🔥

One-line quickies asked very often at fresher level.

- `var` is still strongly typed — compiler infers the type.
- `string` is a reference type but immutable.
- `decimal` for money vs `double` for scientific/normal math.
- `??` = null-coalescing (use default if null); `?.` = null-conditional (safe call).
- `nameof(x)` gives "x" as string (used in Swagger XML / config errors).
- Expression-bodied members: `=>` shorthand for one-line methods/properties.
- Tuple and collection expressions exist: `= []` for empty collections (used all over
  entities: `public ICollection<Module> Modules { get; set; } = [];`).
- Records would be ideal for immutable DTOs, but LearnPath uses plain classes.
- `switch` expressions (`ex switch { ArgumentException => ..., _ => ... }`) are used
  in `ExceptionMiddleware`.
- Foreach, `if/else`, `switch`, loops are basic but always worth a 5-second mention as
  "language constructs I write daily".

### Follow-Up Questions
Q: What is `null` and why is it dangerous?
A: `null` means "no value/reference". Calling a member on null throws
`NullReferenceException`, so we check with `?.` / `??` and nullable annotations like
`string?`.

Q: What does `string?` mean?
A: Nullable reference type — the compiler flags usage that may be null, helping avoid
NREs.

Q: Why are entities' collection fields initialized to `[]`?
A: To avoid null collections when no data exists yet — e.g.
`path.Modules` is an empty list, never null (`Entities/LearningPath.cs`).

### Remember
- nameof, `??`, `?.`, `[]` initializers, switch expressions — quick badges used in code.

---

## Final Phase 1 "remember by heart" checklist

1. C# = language; .NET = platform. Starts with `Main`/top-level statements.
2. Value types copy by value; reference types share the object.
3. Class = blueprint; object = `new` instance; constructor = required setup/DI.
4. Pillars of OOP: Encapsulation, Inheritance, Polymorphism, Abstraction — be ready to
   name all four.
5. Interface = pure contract (implement many); abstract class = shared base (inherit
   one).
6. Overloading = params change (compile-time); overriding = same signature + `virtual`
   base, new implementation (runtime).
7. static = per type; sealed = no inheritance; const = literal at compile time;
   readonly = set once at runtime.
8. throw specific exceptions; handle them once in `ExceptionMiddleware`.

Next: `02_CSharp_Intermediate.md` — say "Proceed to next phase" when ready.