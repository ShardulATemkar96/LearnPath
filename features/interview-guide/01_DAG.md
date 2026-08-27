# 01 – DAG (Directed Acyclic Graph) in Learning Paths

> Verified against `backend/Algorithms/Graph/DagValidator.cs`, `backend/Services/LearningPath/LearningPathService.cs`, `backend/Entities/Module.cs`, `backend/Entities/ModuleDependency.cs`, `backend/Controllers/LearningPathController.cs`, `frontend/src/utils/graphUtils.ts`, `frontend/src/components/learningPath/PathGraph/PathGraph.tsx`.

---

## 1. What Is It?

A graph is a set of **nodes** connected by **edges**. A **DAG** is:

- **Directed** — each edge has a direction (A → B means "A comes before B").
- **Acyclic** — there is **no closed loop**. If you follow the arrows you can never return to a start node.

In LearnPath the graph represents a **learning path**:
- **Node** = a module (a lesson/video/article/quiz).
- **Directed edge** = a *prerequisite*: module X **depends on** module Y, so Y must be finished before X is unlocked.

The whole structure is guaranteed to be a DAG because cycles are rejected when dependencies are created.

---

## 2. Why Is It Used?

A learning path has a natural "must learn this first" ordering. We need to:

1. **Unlock modules in the right order** — a student can only open a module when all prerequisites are completed.
2. **Prevent impossible situations** — e.g. "Module A requires Module B" AND "Module B requires Module A". That would be a *cycle* and no one could ever start.

So LearnPath models learning paths as a DAG and **enforces** it before allowing a dependency to be added.

Without it, we could get stuck paths (nobody can progress) and the learning path feature would break.

---

## 3. How Is It Used in LearnPath?

### Data model
- `backend/Entities/Module.cs` — each module has `Dependencies` (the modules it needs first) and `Dependents` (the modules that need it).
- `backend/Entities/ModuleDependency.cs` — join table:
  - `ModuleId` = the module that has the requirement
  - `DependsOnModuleId` = the prerequisite module

So a row `(ModuleId = 5, DependsOnModuleId = 2)` means "module 5 depends on module 2".

### Cycle detection — `backend/Algorithms/Graph/DagValidator.cs`
- `WouldCreateCycle(adjacency, from, to)` — returns `true` if adding edge `from → to` would create a cycle.
- `TopologicalSort(adjacency)` — produces a valid learning order; throws `InvalidOperationException("Cycle detected...")` if a cycle exists.

### Where it is enforced — `backend/Services/LearningPath/LearningPathService.cs`
- `AddDependencyAsync(int pathId, AddDependencyDto dto, string userId)`:
  1. loads only the dependencies that belong to this path,
  2. builds an **adjacency dictionary** (`ModuleId → list of DependsOnModuleId`),
  3. calls `DagValidator.WouldCreateCycle(...)` with the new edge,
  4. if it would create a cycle → throws `ArgumentException("Adding this dependency would create a cycle.")`,
  5. otherwise rejects duplicates and saves the new `ModuleDependency`.
- `RemoveDependencyAsync(...)` removes an edge.
- Ownership is checked first via `GetOwnedPathAsync` — only the path owner can add/remove dependencies.

### Unlock logic (also in `LearningPathService`)
- `GetByIdAsync` and `GetModuleContentAsync` compute:
  - `completedModuleIds` = modules where the user's `Progress.IsCompleted == true`
  - `isUnlocked = all dependency ids are in completedModuleIds`
  - `GetModuleContentAsync` throws `UnauthorizedAccessException("Complete all prerequisite modules first.")` when the module is locked.

---

## 4. How It Works Internally

### Adding a dependency (the moment DAG validation runs)

```
Instructor UI → POST /api/v1/paths/{pathId}/dependencies  { moduleId, dependsOnModuleId }
   → LearningPathController.AddDependency
   → LearningPathService.AddDependencyAsync
       → GetOwnedPathAsync (ownership check)
       → load existing deps of the path
       → build adjacency:  { ModuleId : [dependsOn...] }
       → DagValidator.WouldCreateCycle(adjacency, moduleId, dependsOnModuleId)
            if true  → 400 "Adding this dependency would create a cycle."
            if false →  save ModuleDependency + DEPENDENCY_ADDED audit
   → response → UI refreshes the graph
```

### Student view (unlocking)

```
Student opens path detail
   → GET /api/v1/paths/{id}
   → LearningPathService.GetByIdAsync
       → for each module: isUnlocked = deps.All(d => completedIds.Contains(d))
   → frontend PathGraph renders modules green(completed)/purple(unlocked)/grey(locked)
Student clicks a locked module
   → GET /api/v1/paths/{pathId}/modules/{moduleId}
   → if not unlocked → UnauthorizedAccessException (locked)
   → else module content returned
```

---

## 5. Important Internal Logic

### DFS cycle check — `DagValidator.WouldCreateCycle`
```
WouldCreateCycle(adjacency, from, to):
   return Dfs(adjacency, to, from, visited-set)
Dfs(current, target):
   if current == target → true (we reached the start again → cycle)
   if current already visited → false
   for each neighbor → recurse
```
It asks: *"Starting from the prerequisite node `to`, can I reach `from` following existing edges?"* If yes, adding `from → to` closes a loop, so it is rejected. This is exactly a **DFS reachability check** to prevent cycles.

### Topological sort — `DagValidator.TopologicalSort`
Uses DFS with an `inStack` set to detect cycles and produce a valid order (prerequisites before dependents). It is part of the DAG logic, though the runtime path-unlock check is the simpler "all deps completed" rule.

### Locked/unlocked computation
Unlocking is not a stored column — it is **computed per request** from `Progress` rows + `ModuleDependency` rows.

### Prevent invalid dependencies
- Cycle check (`WouldCreateCycle`).
- Duplicate check: the same `(ModuleId, DependsOnModuleId)` pair is rejected with "Dependency already exists."
- **Delete guard:** `DeleteModuleAsync` checks `ModuleDependencies` where `DependsOnModuleId == moduleId` → if other modules depend on the module being deleted, it refuses with the names of the dependents.

### Frontend layout — `frontend/src/utils/graphUtils.ts`
- `assignLevels` implements a **Kahn-style (in-degree) level assignment** to place nodes into columns (level 0 = no prerequisites, then level 1, 2, …).
- `getUnlockedModules` mirrors the backend rule: `deps.every(dId => completedIds.has(dId))`.
- `PathGraph` renders an SVG: each node is a box, each dependency is a bezier arrow. Color meaning: green completed, purple unlocked, grey locked.

---

## 6. Frontend Side

Path: `LearningPathDetailPage` → `pathService.getById` → `redux` (path slice) → `PathGraph` component → state → SVG.

- `frontend/src/services/pathService.ts`
  - `getById(id, includeUnpublished)`
  - `addDependency(pathId, moduleId, dependsOnModuleId)` → POST
  - `removeDependency(...)` → DELETE
- `frontend/src/utils/graphUtils.ts` — `assignLevels` (level layout), `getEdges`, `getUnlockedModules`.
- `frontend/src/components/learningPath/PathGraph/PathGraph.tsx` — draws nodes/edges, colors by completed/unlocked/locked.
- When the instructor adds a dependency that would create a cycle, the backend returns 400; the UI shows the error message and the graph is unchanged.

---

## 7. Backend Side

- **Controller:** `backend/Controllers/LearningPathController.cs`
  - `POST /api/v1/paths/{pathId}/dependencies` → `AddDependency`
  - `DELETE /api/v1/paths/{pathId}/dependencies/{moduleId}/{dependsOnModuleId}` → `RemoveDependency`
  - `GET /api/v1/paths/{id}` → path detail with `Modules` + `Dependencies`
  - `GET /api/v1/paths/{pathId}/modules/{moduleId}` → module content (enforces unlock)
- **Service:** `LearningPathService` (`AddDependencyAsync`, `RemoveDependencyAsync`, `GetByIdAsync`, `GetModuleContentAsync`, `DeleteModuleAsync`).
- **Algorithm:** `DagValidator` (`WouldCreateCycle`, `TopologicalSort`).
- **Entity:** `Module`, `ModuleDependency`.

---

## 8. Database Side

- **`Modules`** table — node data (id, title, order, content, etc.).
- **`ModuleDependencies`** table — edge data (two FK columns `ModuleId`, `DependsOnModuleId`, both pointing to `Modules`).
- Relationships: a module has many dependencies and many dependents (many-to-many on same table).
- Operations:
  - **Create:** new Module rows; new ModuleDependency rows (only after DAG validation).
  - **Read:** joins for path detail and unlock calculation.
  - **Delete guard:** `Module` delete blocked while any ModuleDependency references it.

---

## 9. Security / Authorization

- Only the path **owner** (and Admins, via `[Authorize]` roles in some controllers) can add/remove dependencies — enforced by `GetOwnedPathAsync` (`path.CreatedById != userId → UnauthorizedAccessException`).
- Reading course content is authenticated; unlocking is enforced server-side (a student cannot fetch a locked module's content by manually calling the API).

---

## 10. Simple Real Example

An instructor creates a Java learning path with modules:
- M1 (Java basics)
- M2 (OOP)
- M3 (Inheritance)
- M4 (Collections)

They want M4 to require M2 and M3. They POST `{ moduleId: 4, dependsOnModuleId: 2 }` then `{ moduleId: 4, dependsOnModuleId: 3 }`. Each time, `AddDependencyAsync` builds the adjacency and runs `WouldCreateCycle`. Since there is no path from M2 or M3 back to M4, both edges are saved.

A student completes M1, M2, M3. On the path page, M4's `isUnlocked` becomes true because both dependencies are in the completed set. If they tried to open M4 early, `GetModuleContentAsync` would throw "Complete all prerequisite modules first."

If the instructor tried to add `{ moduleId: 2, dependsOnModuleId: 4 }` right now, `WouldCreateCycle` would find that M4 already depends on M2, so it rejects the request — the graph stays a DAG.

---

## 11. Interview Answer

"In my LearnPath project, learning paths are modeled as a directed acyclic graph. Each module is a node, and each prerequisite is a directed edge stored in a `ModuleDependency` table. The backend must guarantee the graph has no cycles, because a cycle would make a path impossible to complete. I wrote a `DagValidator` class with a `WouldCreateCycle` method that uses DFS: before saving a dependency, I build an adjacency map of existing edges and check whether following the arrows from the prerequisite can reach the module that's getting the new requirement — if yes, that would create a cycle and I reject it with an error. Unlocking works by checking whether all of a module's dependencies are in the student's completed set. On the frontend, `graphUtils` computes levels with an in-degree-based layout and the `PathGraph` component renders it as an SVG with arrows; locked, unlocked, and completed modules are colored differently."

---

## 12. Follow-Up Questions

1. **Why not just use a simple ordered list instead of a graph?**
   An ordered list can only express one linear sequence. With prerequisites, modules can have multiple parents (e.g. Inheritance requires both OOP and Basics) — that needs a DAG.

2. **Where is the cycle detection?**
   In `backend/Algorithms/Graph/DagValidator.cs` → `WouldCreateCycle`, called from `LearningPathService.AddDependencyAsync`.

3. **What exactly does the DFS check?**
   Starting from the prerequisite node, it tries to reach the node that is getting the new edge, following existing edges. Reaching it means a loop would be created.

4. **Can a student open a locked module by calling the API directly?**
   No. `GetModuleContentAsync` recomputes the unlock state server-side and throws an exception if prerequisites are not completed.

5. **Is the unlocked state stored in the database?**
   No — it is computed on each request from the `Progress` and `ModuleDependency` rows.

6. **What happens if you try to delete a module others depend on?**
   `DeleteModuleAsync` checks dependencies pointing at it and blocks the delete, listing the dependent modules.

7. **What are the limitations?**
   The current visual layout is a simple level-based grid; very complex graphs can get crowded. Also there is no "draft" topo ordering used at runtime for unlocks — it is a per-node check. That is fine and simple.

8. **How would you improve it?**
   I could add a full graph validation when publishing a path (re-run topological sort), show shortest/optimal learning order, or use a proper graph library instead of custom SVG for big graphs.

---

## 13. Functionality

- Learning path modeled as modules (nodes) + prerequisite edges (`ModuleDependency`).
- Cycle prevention at dependency creation via `DagValidator.WouldCreateCycle` (DFS reachability).
- Duplicate dependency rejection.
- Module deletion guard when other modules depend on it.
- Server-side unlock enforcement (all prerequisites must be completed).
- Path detail returns modules + dependencies for graph rendering.
- `DagValidator.TopologicalSort` utility (DFS with in-stack cycle detection).
- Frontend level-based layout (`graphUtils.assignLevels`) and SVG `PathGraph` with locked/unlocked/completed states.