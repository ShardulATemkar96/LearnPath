# Phase 2 — C# Intermediate (Interview Preparation)

Priority guide:
- `🔥 MUST KNOW` — expect to be asked, be able to answer instantly
- `🟡 SHOULD KNOW` — common follow-up, know the main idea
- `⚪ BASIC AWARENESS` — mention only if relevant

All LearnPath references below were verified from source. Concepts not found as a
significant project implementation are clearly marked as general.

---

## 1. Collections 🟡

### What is it?
Collections are data structures that hold multiple objects. The main ones are
`List<T>`, `Dictionary<K,V>`, `HashSet<T>`, plus interfaces like `IEnumerable<T>`,
`ICollection<T>`, `IList<T>` that describe what a collection can do.

### Why is it used?
Business data is plural (users, modules, posts). Collections model "many" items and
come with convenient methods (Add, Remove, LINQ queries).

### Interview Answer
"Collections store multiple values. In .NET the most common are `List`, `Dictionary`
for key-value pairs, and `HashSet` for unique items. I use them inside services a lot
— for example storing a list of completed module IDs as a `HashSet` so I can check
membership quickly."

### Simple Example
```csharp
var tags = new List<string> { "C#", "API" };      // ordered, indexed
var lookup = new Dictionary<string, int>();        // key → value
var unique = new HashSet<int>();                   // no duplicates
```

### How it works
Lists are variable-size arrays underneath (grow when needed). Dictionaries are
hash tables (fast lookup by key). HashSets use hashing for O(1) membership tests.
`foreach` works because every collection implements `IEnumerable<T>`.

### LearnPath Example
Services return `List<...>` DTOs, e.g.
`LearningPathService.GetAllPublicAsync()` returns
`Task<List<LearningPathResponseDto>>`.

### Common Mistake
- Not choosing the right collection: using `List` when you need unique items
(HashSet) or fast key lookup (Dictionary).

### Follow-Up Questions
Q: Which collection is fastest for lookup by value?
A: `HashSet` or `Dictionary` (hash-based, O(1)) vs linear search in `List`.
Q: Is `List` ordered? Dictionary?
A: `List` keeps insertion order and supports index; `Dictionary` does not guarantee
order.

### Remember
- List = ordered many; Dictionary = by key; HashSet = unique memberships.

---

## 2. Array vs List 🔥

### What is it?
`Array` = fixed-size, indexed storage. `List<T>` = dynamic array from `System.Collections.Generic` that grows automatically.

### Why is it used?
Arrays when the size is known/fixed and you need raw speed. Lists when you need to
add/remove items dynamically — which is true for almost all app code.

### Interview Answer
"An array has a fixed size decided at creation, so you can't add or remove items.
A `List` grows automatically and has methods like `Add`, `Remove`, and LINQ support,
which is why I use lists in services. Arrays are fine when the number of items never
changes."

### Simple Example
```csharp
string[] names = new string[3];       // fixed size = 3
var list = new List<string>();        // grows as needed
list.Add("C#");
```

### LearnPath Example
Generic usage in services has `List<ModuleResponseDto>` returns and `List<T>`
parameters everywhere (e.g. `Roles = roles` is `IList<string>` in
`AuthResponseDto`).

### Common Mistake
- Using arrays when the count is dynamic (throws or wastes memory).

### Follow-Up Questions
Q: Arrays vs List memory?
A: Array = one fixed block; List = resizable buffer (doubles capacity when full).

### Remember
- Fixed → Array; dynamic add/remove + LINQ → List.

---

## 3. Dictionary 🔥

### What is it?
A key-value collection: each key maps to one value. Lookup by key is very fast.
Keys must be unique; lookup O(1).

### Why is it used?
Real lookup tables: config values, counts per item, adjacency lists, caching.

### Interview Answer
"A `Dictionary` stores key-value pairs. You find values by key, and it's very fast —
near constant time. Keys must be unique. In LearnPath I build a dictionary of module
dependencies to run the cycle check, and `Program.cs` stores mapping of config keys
to values in a dictionary before binding them into configuration."

### Simple Example
```csharp
var depAdjacency = new Dictionary<int, List<int>>
{
    { 1, [2] },   // module 1 depends on module 2
    { 2, [] },
};
if (depAdjacency.TryGetValue(1, out var list))
    // list contains 2
```

### LearnPath Example
`Program.cs:54-65` — `envMapping` dictionary maps config section keys to env values.
`LearningPathService.AddDependencyAsync` — `.GroupBy(...).ToDictionary(...)` builds the
adjacency dictionary for `DagValidator`.
`ProgressService` uses `.ToDictionaryAsync(p => p.ModuleId, ...)` to look up completion
dates by module id.

### How it works
Internally a hash table: the key's hash code decides a slot, so lookup does not scan
all items.

### Common Mistake
- Adding a duplicate key (throws `ArgumentException`) — check with `ContainsKey`.
- Forgetting `TryGetValue` is the safe lookup pattern.

### Follow-Up Questions
Q: Keys must be unique — what if you try duplicates?
A: `Add` throws `ArgumentException`; the indexer `dict[k]=v` also throws on a missing
key with `dict[k]` read.

### Remember
- Dictionary = key→value, unique keys, O(1) lookup, `TryGetValue` is your friend.

---

## 4. HashSet 🔥

### What is it?
A collection that stores unique items with no order. Membership check (Contains) is
very fast.

### Why is it used?
When you need to know "is X in this set" or remove duplicates.

### Interview Answer
"A `HashSet` stores unique items in no particular order, and checking if an item
exists is very fast. I use it in `LearningPathService` to build the set of completed
module IDs, then ask `completedModuleIds.Contains(id)` for each module — constant
time instead of scanning a list."

### Simple Example
```csharp
var completedIds = completionList.Select(m => m.Id).ToHashSet();
bool done = completedIds.Contains(42);   // O(1)
```

### LearnPath Example
`LearningPathService.GetByIdAsync` — `var completedModuleIds = path.Modules....

.Select(m => m.Id).ToHashSet();` then `.Contains(dId)` in the unlock check.

### How it works
Hash-based storage: each item's hash code places it in a slot, so Contains averages
O(1).

### Common Mistake
- Expecting order (it has none) or index access (not supported).

### Follow-Up Questions
Q: HashSet vs List for Contains?
A: HashSet is O(1); List is O(n) linear scan.

### Remember
- HashSet = unique + fast Contains, no order.

---

## 5. IEnumerable 🔥

### What is it?
The most basic interface for anything you can iterate with `foreach`: collections,
LINQ results, lazy sequences. It exposes `GetEnumerator()`.

### Why is it used?
It lets you write code that works with ANY sequence (List, array, query result)
without caring about the concrete type.

### Interview Answer
"`IEnumerable<T>` is the base contract for anything you can loop over. Because all
collections and LINQ results implement it, I can write methods that accept
`IEnumerable<T>` and they work with lists, arrays, or queries interchangeably. A key
point: it is lazy — with LINQ, nothing runs until I actually iterate or call
`ToList`."

### Simple Example
```csharp
public int CountActive(IEnumerable<Module> modules)
    => modules.Count(m => m.IsPublished);
```

### LearnPath Example
`IGenericRepository<T>` (`backend/Interfaces/Repositories/`) returns
`Task<IEnumerable<T>>` from `GetAllAsync`/`FindAsync` — controllers/services treat
results generically.

### How it works
`foreach` calls `GetEnumerator()`, then repeatedly calls `MoveNext()` and reads
`Current` until the sequence ends. LINQ operators return `IEnumerable` that compute
items on demand (deferred execution).

### Common Mistake
- Returning `IEnumerable` from DB queries then iterating twice — each iteration may
re-run the query. Call `.ToList()` once when you need a materialized copy.

### Follow-Up Questions
Q: What is deferred/lazy execution?
A: Query runs when iterated, not when defined. `ToList()` forces it to run now.

### Remember
- IEnumerable = "can be looped", lazy. Materialize with ToList when needed.

---

## 6. ICollection 🟡

### What is it?
Extends `IEnumerable<T>` (so it is still loopable) and adds
`Count`, `Add`, `Remove`, `Contains` — i.e. "a modifiable, countable collection".

### Why is it used?
It describes collections you can add to and remove from, without promising order or
indexing (that's `IList`).

### Interview Answer
"`ICollection<T>` is IEnumerable plus basic modification and size operations:
Count, Add, Remove, Clear, Contains. It is the interface EF Core uses for
navigation properties — that's exactly what you see in my entities."

### Simple Example
```csharp
public ICollection<Module> Modules { get; set; } = [];
// gives Count, Add, Remove, foreach
```

### LearnPath Example
Every entity navigation property is typed `ICollection<T>` and initialized to `[]`
so it is never null — e.g. `LearningPath.Modules`,
`User.CreatedPaths`, `Module.Resources` (`backend/Entities/*.cs`).

### Common Mistake
- Confusing it with `IList` — ICollection has no indexer.

### Remember
- ICollection = countable + add/remove, no index. EF Core navigation property type.

---

## 7. IList 🟡

### What is it?
Extends `ICollection<T>` and adds indexed access: `this[int]`, `IndexOf`, `Insert`.
Concrete types: `List<T>`.

### Why is it used?
When you need position-based access, not just add/remove.

### Interview Answer
"`IList<T>` adds index-based access to ICollection, so I can read or replace items by
position. `List<T>` is the typical implementation. In `JwtTokenGenerator` the roles
come in as an `IList<string>` and I just need to enumerate them into claims."

### Simple Example
```csharp
IList<string> roles;       // roles[0], roles.Add(...), foreach
```

### LearnPath Example
`JwtTokenGenerator.GenerateAccessToken(User user, IList<string> roles)` — accepts any
list of roles and maps each to a claim with `.Select(r => new Claim(...))`.

### Common Mistake
- Using IList when you don't need indexing — ICollection/IEnumerable is more flexible.

### Remember
- IList = indexed collection. List<T> is the implementation.

---

## 8. Generics 🔥

### What is it?
Code written with a placeholder type (`T`) that becomes concrete when used:
`List<T>`, `ApiResponse<T>`, `GenericRepository<T>`.

### Why is it used?
To reuse the same logic for different types with full compile-time type safety —
avoiding duplication and casts.

### Interview Answer
"Generics let me write one class or method that works with any type, specified later.
`ApiResponse<T>` wraps the result of any operation — the same class serves auth,
users, and posts responses. Type safety is preserved — the compiler knows what `T`
is at each usage."

### Simple Example
```csharp
public class ApiResponse<T>
{
    public T? Data { get; set; }
}
var ok = ApiResponse<AuthResponseDto>.Ok(result);
var post = ApiResponse<PostDto>.Ok(postDto);
```

### LearnPath Example
`backend/Common/ApiResponse.cs` — generic envelope used by every endpoint.
`backend/Repositories/GenericRepository.cs` + `IGenericRepository<T>` — one repository
for any entity (`SaveChangesAsync`, `GetAllAsync`, `FindAsync`).

### How it works
The compiler generates specialized code per `T` (or shares one with
reference types), and enforces T's constraints (`where T : class`) at compile time.

### Common Mistake
- Casting to unbox when generics already give you the right type — no casts needed.

### Follow-Up Questions
Q: What is a generic constraint?
A: `where T : class`, `where T : new()` limits which types are allowed.

### Remember
- Generics = "type later". Safe, reusable, no casts. `ApiResponse<T>` proves it.

---

## 9. Delegates 🔥

### What is it?
A delegate is a type that holds a reference to a method — a "function pointer" with
type safety. Built-ins: `Func<...>` (returns value), `Action<...>` (returns void),
`Predicate<T>` (returns bool).

### Why is it used?
To pass behavior as data: callbacks, LINQ predicates, event handlers.

### Interview Answer
"A delegate is a reference to a method, so I can pass a method around like a value.
The built-in ones are `Func` for methods that return something, `Action` for methods
that return void, and `Predicate` for methods returning bool. You use them without
knowing it — every LINQ lambda like `m => m.IsPublished` is a delegate."

### Simple Example
```csharp
Func<int, int, int> add = (a, b) => a + b;   // not used much directly, but:
// the underlying type of every LINQ lambda
```

### LearnPath Example
No custom delegates are defined in the project, but delegates are everywhere for
real: LINQ lambdas, `ConcurrentDictionary.AddOrUpdate(key, addFactory, updateFactory)`
in `RateLimitingMiddleware` take `Func` delegates as arguments.

### Common Mistake
- Thinking lambdas and delegates are different things — a lambda IS the concise
syntax that creates a delegate/expression.

### Follow-Up Questions
Q: Difference between delegate, lambda, Func?
A: Delegate = type. Func/Action = pre-defined delegate types. Lambda = inline method
syntax that becomes one.

### Remember
- Delegate = method as a value. Func/Action are the ready-made deleget types.

---

## 10. Events ⚪

### What is it?
An event is a special delegate for notifications: a class *publishes* an event, other
code *subscribes* with `+=` and reacts.

### Why is it used?
To let one part of the app react when something happens in another part, without the
publisher knowing who is listening.

### Interview Answer
"Events are built on delegates: a class declares an event, and other classes
subscribe with `+=` to be notified when it fires. They're used for
publisher/subscriber scenarios — publish a notification, subscribers react."

### LearnPath Example
Not used as a significant pattern in the LearnPath backend — there is no custom
event-driven code verified in the source. State it as a general concept and mention
that ASP.NET Core instead handles cross-cutting notifications through middleware and
services.

### Simple Example
```csharp
public event EventHandler? PathCreated;
PathCreated?.Invoke(this, EventArgs.Empty);  // fire
```

### Common Mistake
- Confusing events with delegates — events are a constrained form (only += / -= from
outside).

### Remember
- Events = publish/subscribe notification built on delegates. Not a big LearnPath
pattern.

---

## 11. Lambda expressions 🔥

### What is it?
An inline anonymous method: `(x) => x * 2`. The left is parameters, the right is the
body.

### Why is it used?
For concise, readable code — especially with LINQ — instead of writing full methods.

### Interview Answer
"A lambda is a shorthand for writing a method inline. `m => m.IsPublished` means
'take m and return whether it is published'. Lambdas are the syntax I write every day
inside LINQ queries and validators."

### Simple Example
```csharp
var published = paths.Where(p => p.IsPublished && p.IsPublic).ToList();
//                    └─ lambda parameter ─┘         └─ body ─┘
```

### LearnPath Example
Everywhere: `LearningPathService` filters with `.Where(p => p.IsPublished ...)`,
FluentValidation rules use `RuleFor(x => x.Email)`, `Program.cs` builds env mappings
with `.Where(kvp => kvp.Value is not null)`.

### How it works
The compiler turns the lambda into a delegate or expression tree. With EF Core,
`Where(p => ...)` becomes an expression tree that EF translates into SQL.

### Common Mistake
- Fear of lambdas — practice reading them: "item arrow result".

### Follow-Up Questions
Q: Lambda vs expression tree?
A: Delegate lambda runs as C# code; expression tree stores the operations as data, so
EF Core can translate it to SQL.

### Remember
- Lambda = inline method syntax. Read as "input → result".

---

## 12. LINQ 🔥

### What is it?
Language Integrated Query — querying collections with readable operators
(`Where`, `Select`, `OrderBy`, `GroupBy`, `First`, `Any`...) instead of loops.

### Why is it used?
Shorter, declarative, safer code vs manual `foreach` + `if` + counters.

### Interview Answer
"LINQ lets me query collections in a declarative way. Instead of writing loops and
conditions, I chain methods like `.Where()`, `.Select()`, `.OrderBy()`. With EF Core
the same LINQ is translated into SQL, so the query runs in the database, not in
memory."

### Simple Example
```csharp
var publicPaths = await _context.LearningPaths
    .Where(p => p.IsPublished && p.IsPublic)
    .OrderBy(p => p.Title)
    .Select(p => new { p.Id, p.Title })
    .ToListAsync();
```

### LearnPath Example
Every service uses LINQ against EF Core: `LearningPathService.GetByIdAsync`,
`AnalyticsService` `.GroupBy(p => p.Module.ContentType)`,
`NotificationService` `.OrderByDescending(n => n.CreatedAt)`.

### How it works
Two kinds: LINQ to Objects (runs in memory) and LINQ to Entities (EF Core turns the
LINQ expression tree into SQL). Lazy by default — executed when enumerated.

### Common Mistake
- Using LINQ in a way that triggers N+1 or in-memory filtering of a whole table.
- Forgetting the async versions in EF Core (`ToListAsync`, not `ToList`).

### Follow-Up Questions
Q: LINQ to Objects vs LINQ to Entities?
A: Objects = in memory over collections. Entities = translated to SQL by EF Core.

### Remember
- LINQ = declarative queries. Same syntax for memory and (via EF) database.

---

## 13. LINQ Where / Select / OrderBy / GroupBy 🔥

### What is it?
- `Where` — filter (keep items that match a condition).
- `Select` — project (transform each item into another shape).
- `OrderBy` / `OrderByDescending` — sort.
- `GroupBy` — group items by a key.

### Why is it used?
They are the four most-used query verbs; knowing them = knowing LINQ.

### Interview Answer
"`Where` filters, `Select` transforms, `OrderBy` sorts, `GroupBy` groups. I use them
constantly — for example `Where` to filter public paths, `Select` to map entities to
DTOs, `OrderBy` to sort modules, `GroupBy` to count module completions by type."

### Simple Example
```csharp
var q = _context.Modules
    .Where(m => m.LearningPathId == pathId)      // filter
    .OrderBy(m => m.Order)                        // sort
    .Select(m => new ModuleDto { ... });          // transform

var byType = _context.Progresses
    .GroupBy(p => p.Module.ContentType)           // group
    .Select(g => new { Type = g.Key, Count = g.Count() });
```

### LearnPath Example
`LearningPathService.SearchModulesAsync` — builds a query with optional `Where`s then
`.OrderBy(m => m.Order).ToListAsync()`.
`AnalyticsService` — `.GroupBy(p => p.Module.ContentType)` for stats.
`NotificationService` — `.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt)`.

### Common Mistake
- Reversing the mapping: Select does mapping, not filtering.

### Follow-Up Questions
Q: Order of chaining matters?
A: Yes — `Where` first to reduce data, then `Select`; GroupBy changes the shape to
key + groups.

### Remember
- Where=filter, Select=map, OrderBy=sort, GroupBy=group. Order matters.

---

## 14. First vs FirstOrDefault 🔥

### What is it?
`First` returns the first match and THROWS if there are none.
`FirstOrDefault` returns the first match or the default (`null` for classes) if none
exist.

### Why is it used?
Choice depends on whether "no match" is an expected situation or a bug.

### Interview Answer
"`First` throws if there is no match; `FirstOrDefault` returns null instead. If not
finding the item is a valid scenario — like a user's vote on a post — I use
`FirstOrDefault` and check for null. If it should always exist, `First` (or
`FirstOrDefault` + throw `KeyNotFoundException`) is clearer. Deferred execution means
both stop at the first match."

### Simple Example
```csharp
var module = path.Modules.FirstOrDefault(m => m.Id == moduleId)
    ?? throw new KeyNotFoundException("Module not found.");
```

### LearnPath Example
The codebase uses the safe pattern everywhere: `LearningPathService` does
`.FirstOrDefaultAsync(p => p.Id == id) ?? throw new KeyNotFoundException(...)`.
`CommunityService` uses `FirstOrDefaultAsync` for optional votes.

### Common Mistake
- Using `First` on possibly-empty data without a try/catch → crashes endpoints.
- Calling `.FirstOrDefault()` then using the result without a null check.

### Follow-Up Questions
Q: When to use First vs Single?
A: First = first in order (or query order); Single = exactly one result, throws if
0 or >1.

### Remember
- Expected missing → FirstOrDefault + null-check. Must exist → First/throw.

---

## 15. Single vs SingleOrDefault 🟡

### What is it?
`Single` returns THE only match and throws if there are 0 OR more than 1.
`SingleOrDefault` returns null when there are 0, throws when more than 1.

### Why is it used?
When the data must contain exactly one match by rule (e.g. one user per email).

### Interview Answer
"`Single` demands exactly one result and throws for zero or multiple matches.
`SingleOrDefault` tolerates zero (returns null) but still throws for multiple.
It's a strict validator for uniqueness. If I merely need the first item, `First` is
the right call, not `Single`."

### Simple Example
```csharp
var uniqueUser = users.Single(u => u.Email == email); // throws if 0 or 2+
```

### LearnPath Example
Not used heavily in the project (verified). The codebase prefers
`FirstOrDefaultAsync(...) ?? throw` since records are fetched by primary keys which
are unique. General concept otherwise.

### Common Mistake
- Using `Single` in `First` scenarios → throws on non-unique data that is fine to
skip.

### Follow-Up Questions
Q: Why would Single throw more than First?
A: Single enforces uniqueness (throws if >1); First is happy with the first one.

### Remember
- Single = uniqueness guard. First = first match. Pick by the data rule.

---

## 16. Any vs Count 🔥

### What is it?
`Any()` returns bool — does at least one item match? `Count()` returns the number.
`Enumerable.Count` vs `.Count()` — `Count` property is O(1) for `ICollection`; LINQ
`Count()` may enumerate.

### Why is it used?
For "is there at least one?" you want `Any` — it stops early. Counting when you only
need yes/no wastes work.

### Interview Answer
"`Any()` answers 'is there at least one matching item' and only needs to find the
first match, so it's faster than `Count() > 0`. For EF Core, `AnyAsync` becomes `EXISTS`
in SQL, which is very efficient. I use `Any` for existence checks like 'does this
title already exist'."

### Simple Example
```csharp
if (await _context.Modules.AnyAsync(m => m.LearningPathId == pathId && m.Title == title))
    throw new ArgumentException("Duplicate module title.");
```

### LearnPath Example
`LearningPathService.AddModuleAsync` duplicates checks with `.AnyAsync(...)`.
`DagValidator` uses `.Any(n => Dfs(...))`. Real counts use `CountAsync` in
`AdminService` / `AnalyticsService` (they need the number, not yes/no).

### How it works
`Any` short-circuits at the first match; `Count` must visit all items. `ICollection.Count`
property is O(1).

### Common Mistake
- Writing `.Count() > 0` when `Any()` is the right, faster call.

### Follow-Up Questions
Q: `Count` property vs `Count()` method?
A: Property = O(1) for List/Dictionary; extension method `Count()` may enumerate
LINQ results.

### Remember
- Need yes/no → Any. Need the number → Count.

---

## 17. async / await 🔥

### What is it?
`async` marks a method that can suspend without blocking the thread.
`await` pauses the method until a `Task` completes without freezing the thread.
Return types: `Task` (void), `Task<T>` (value), `ValueTask<T>`.

### Why is it used?
For I/O (database calls, HTTP, file reads) — the thread returns to the thread pool
while waiting, so the server handles many requests on few threads (scalability).

### Interview Answer
"`async/await` lets a method pause at an I/O call without holding a thread. When I
`await` a database query, the thread goes back to the thread pool until the result
arrives, which lets the server serve thousands of concurrent requests with far fewer
threads. It is standard in ASP.NET Core — every service method is `async Task`."

### Simple Example
```csharp
public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
{
    var user = await _userManager.FindByEmailAsync(dto.Email); // thread freed here
    return await BuildAuthResponseAsync(user);
}
```

### LearnPath Example
`AuthService.LoginAsync`, `LearningPathService.GetByIdAsync`, controllers all use
`async Task<IActionResult>`. The convention: methods named `*Async` returning `Task`.

### How it works
At `await`, the method returns an incomplete `Task`, the thread frees up. When the
I/O completes, a thread continues the method from the same point. No thread sleeps —
this is the key scalability win.

### Common Mistake
- **Async void** (fire and forget) in ASP.NET controllers — exceptions crash the app.
- Blocking with `.Result`/`.Wait()` on async calls → deadlocks under sync contexts.
- Forgetting `Await` means the exceptions happen on the task, not at the call site.

### Follow-Up Questions
Q: When should I use async vs sync?
A: I/O-bound work → async. CPU-bound work → keep sync (or Task.Run only if attacking
UI, which is rare in ASP.NET).

### Remember
- async/await = don't block threads during I/O. Database + HTTP = async.

---

## 18. Task 🔥

### What is it?
`Task` represents ongoing work. `Task<T>` carries a result when finished, e.g.
`Task<AuthResponseDto>`.

### Why is it used?
It's the return type of async methods and the unit the runtime schedules — the
"promise" of a future value.

### Interview Answer
"A `Task` represents asynchronous work — like an IOU for a result. `Task<T>` carries
a value when done, and `Task` alone just signals completion. Methods that return them
are `async` and `await`ed at the call site."

### Simple Example
```csharp
Task<int> downloadAsync() => ...;         // promise of an int
int length = await downloadAsync();       // wait for the value
```

### LearnPath Example
Every service and controller method returns `Task` or `Task<T>`:
`Task<AuthResponseDto>`, `Task<IActionResult>`, `Task<List<...>>`.

### How it works
A task is a state machine; when awaited to completion on the current thread, or
the continuation is scheduled. The compiler generates this behind `async`.

### Common Mistake
- Confusing `Task` with a thread — a task is not a thread; it's scheduled work that
often runs without its own thread during I/O waits.

### Follow-Up Questions
Q: Task vs ValueTask?
A: ValueTask avoids a heap allocation for very fast/rarely-async calls; Task is the
general choice.

### Remember
- Task = promise of work/result. Behavior of async in ASP.NET.

---

## 19. Synchronous vs asynchronous programming 🔥

### What is it?
Synch = one thing at a time; a blocking call waits until it finishes (thread sits).
Async = a call suspends at I/O waits, the thread is freed, execution resumes later.

### Why is it used?
Server scenarios must handle many concurrent requests — async lets them with few
threads.

### Interview Answer
"In synchronous code a method runs start to finish and a blocking call keeps the
thread idle. In async code, at an `await` the method yields and the thread goes back
to the pool; when the I/O finishes, it resumes. The result is identical behaviour but
much better server throughput. In ASP.NET Core, endpoints and services are async by
convention."

### Simple Example
```csharp
// sync (blocks a thread during DB wait)
User u = _db.Users.First(...);
// async (frees thread during DB wait)
User u = await _db.Users.FirstAsync(...);
```

### LearnPath Example
All database access is async: `.ToListAsync()`, `.FirstOrDefaultAsync()`,
`SaveChangesAsync()`, awaited inside async services.

### How it works
The thread pool has a limited set of threads; async waits don't consume them. Waiting
on an I/O requires a system-level overlapped operation to signal the completion.

### Common Mistake
- Blocking async with `.Result`/`.Wait()` — deadlock risk (in classic ASP.NET /
libraries with sync context) and wasted threads.

### Follow-Up Questions
Q: Does async make a single request faster?
A: Not necessarily — it improves throughput under concurrency, not single-call latency.

### Remember
- Sync blocks threads; async frees threads at I/O waits. Server throughput is the
win.

---

## 20. Nullable types 🟡

### What is it?
Syntax: `string?`, `int?`, `DateTime?`. `?` on a value type (int?, DateTime?) makes it
nullable to use null. `?` on a reference type enables nullable analysis so the
compiler warns about potential null usage.

### Why is it used?
Data legitimately lacks values — a user without an avatar, an unpublished path.

### Interview Answer
"A nullable type can hold an actual value or null. For value types like `int` or
`DateTime`, the `?` makes them nullable, since normally they can't be null. For
reference types, `string?` is a hint to the compiler to warn me if I might dereference
null. In entities I use it for optional fields."

### Simple Example
```csharp
public string? AvatarUrl { get; set; }     // optional — may be null
public DateTime? ArchivedAt { get; set; }  // only set when archived
```

### LearnPath Example
`Module.cs` — `string? ContentUrl`, `int? EstimatedDurationMinutes`,
`DateTime? ArchivedAt`. `User.cs` — `string? AvatarUrl`, `string? Bio`.

### How it works
For value types `int?` is `Nullable<int>`: it holds the value + a HasValue flag.
For reference types nullability is mostly a compile-time analysis tool.

### Common Mistake
- Calling `.Value` on a nullable without checking `HasValue` → `InvalidOperationException`.
- Using `string?` on a comparison and confusing default string.

### Follow-Up Questions
Q: How to safely read a nullable?
A: `?.`, `??`, `HasValue` check, `.GetValueOrDefault()`.

### Remember
- `int?` = integer that may be null. Optional columns → nullable in entities.,

---

## 21. string vs StringBuilder 🟡

### What is it?
`string` is immutable — every change creates a new object. `StringBuilder` is a
mutable buffer for building strings chunk by chunk.

### Why is it used?
Many concatenations in a loop with `string` create tons of garbage. StringBuilder
avoids repeated allocations.

### Interview Answer
"Strings are immutable — each concatenation creates a whole new string. If I'm
building text in a loop, that's wasteful, so `StringBuilder` is the right tool: it's
a mutable buffer I keep appending to. For a few fixed concatenations, normal `+` or
interpolation is clearer and fine."

### Simple Example
```csharp
var sb = new StringBuilder();
foreach (var module in modules)
    sb.AppendLine(module.Title);
var result = sb.ToString();   // one final string
```

### LearnPath Example
Not used significantly in the project (verified) — messages are short and use
interpolation like `$"User '{user.Email}' logged in."`. General concept:
interpolation for simple cases, StringBuilder for heavy loops.

### Common Mistake
- Using `+` inside a loop obliviously — O(n²) copying. Use StringBuilder.
- Using StringBuilder for tiny fixed strings — overkill.

### Follow-Up Questions
Q: When NOT to use StringBuilder?
A: Small fixed concatenations — interpolation `$"..."` is cleaner and just as fast.

### Remember
- string immutable, StringBuilder = reusable buffer for heavy building.

---

## 22. ref / out / in 🟡

### What is it?
Argument modifiers: `ref` passes by reference (read/write), `out` passes by reference
and must be assigned by the method, `in` passes by reference read-only (for
performance, avoids copying big structs).

### Why is it used?
`out` for methods that produce extra results; `ref` to modify the caller's variable;
`in` for performance without mutation.

### Interview Answer
"`ref` passes an argument by reference so the method can read and write the caller's
variable. `out` also passes by reference and the method must assign it — the classic
example is `TryGetValue(key, out var value)`. `in` passes by reference but
read-only, mainly to avoid copying large structs."

### Simple Example
```csharp
if (dict.TryGetValue(moduleId, out var completedAt))   // out
    use(completedAt);

using var rng = RandomNumberGenerator.Create();        // using, not ref/out
```

### LearnPath Example
`out` appears via framework patterns: `TryGetValue(..., out var ...)` in
`DagValidator` and `ProgressService`. The project doesn't define its own `ref`/`in`
argument methods (verified). `using var` (disposal) is used for `RNG` in
`JwtTokenGenerator` and for transactions/execution strategy in
`LearningPathService.DeleteAsync`.

### Common Mistake
- Using `ref` when `out` fits better — `out` does not require pre-initialization.
- Not knowing `out` variables can be declared inline (`out var x`).

### Follow-Up Questions
Q: `ref` vs `out` difference?
A: `ref` needs the variable initialized before; `out` does not, and the method must
assign it.

### Remember
- TryGetValue/out = results; ref = modify in place; in = read-only pass.

---

## 23. Basic memory / garbage collection understanding 🟡

### What is it?
The CLR manages memory: heap (objects) + a Garbage Collector (GC) that automatically
reclaims unreachable objects in generations (0/1/2).

### Why is it used?
You rarely free memory manually — the GC tracks references and reclaims what is no
longer reachable, preventing leaks and double-free bugs.

### Interview Answer
"The garbage collector automatically frees objects that are no longer referenced,
so I generally don't manage memory by hand. It works by generations — short-lived
objects are collected most often. For things like database connections or files, I
handle release with `using` / `await using`, which guarantees Dispose is called."

### Simple Example
```csharp
using var rng = RandomNumberGenerator.Create();   // deterministic cleanup
// rng.Dispose() called automatically at scope end
```

### LearnPath Example
`JwtTokenGenerator` (`using var rng`), `LearningPathService.DeleteAsync`
(`await using var transaction` + execution strategy) — proper disposal of non-memory
resources.

### How it works
Every allocation lands in Gen0; when Gen0 fills, GC scans roots (static fields, locals)
and keeps referenced objects, moves survivors to Gen1/Gen2. Unreachable objects are
freed.

### Common Mistake
- Calling `GC.Collect()` manually — let the runtime decide.
- Forgetting disposal of connections/files — use `using`.

### Follow-Up Questions
Q: Can I force GC? 
A: `GC.Collect()` exists, but it is a tuning tool rarely needed — the runtime handles
it.

### Remember
- GC reclaims unreachable memory automatically. `using` for resources like DB/stream.

---

## Final Phase 2 "remember by heart" checklist

1. List grows; Array fixed. Dictionary = fast lookup by key; HashSet = unique + fast
   Contains; both hash-based.
2. ICollection = countable editable; IList adds index; IEnumerable = loopable + lazy.
3. Generics (`ApiResponse<T>`, `GenericRepository<T>`) = reuse + type safety.
4. Lambda = inline method. LINQ core verbs: Where (filter), Select (map), OrderBy
   (sort), GroupBy (group).
5. FirstOrDefault = null when missing; First = throw when missing; Single = exactly
   one; Any() for existence, Count() for the number.
6. async + await for I/O; Task = the promise; never block async with .Result.
7. `string?`/`int?` for optional data; StringBuilder for loops of strings.
8. out for results (TryGetValue); using/await using guarantees cleanup; GC handles
   memory.

Next: `03_DotNet_Fundamentals.md` — say "Proceed to next phase" when ready.