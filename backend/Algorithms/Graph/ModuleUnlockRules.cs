namespace LearnPath.API.Algorithms.Graph;

/// <summary>
/// Single source of truth for module unlocking and ordering, shared by
/// LearningPathService, AttemptService, and ProgressService.
/// </summary>
/// <remarks>
/// Unlocking semantics:
/// <list type="bullet">
/// <item>A module with prerequisites unlocks only when ALL prerequisite
/// modules are completed (DAG rule, based on the user's completion records).</item>
/// <item>A module without prerequisites keeps the sequential rule: it unlocks
/// when the previous module in <c>Module.Order</c> is completed; the module
/// with the lowest order has no predecessor and is always unlocked.</item>
/// </list>
/// Ordering: <see cref="DagValidator.TryTopologicalSort"/> produces a valid
/// topological order using <c>Module.Order</c> (ties broken by <c>Id</c>) as
/// the deterministic tie-breaker for independent topics. If the stored graph
/// is invalid (a cycle), the fallback order is the plain (<c>Order</c>,
/// <c>Id</c>) sequence and <c>graphIsValid</c> is <c>false</c>.
/// </remarks>
public static class ModuleUnlockRules
{
    public sealed class ModuleNode
    {
        public required int Id { get; init; }
        public required int Order { get; init; }
        public IReadOnlyList<int> PrerequisiteIds { get; init; } = [];
    }

    /// <summary>Convenience builder for tests and services.</summary>
    public static ModuleNode Module(int id, int order, IEnumerable<int>? prerequisiteIds = null) =>
        new()
        {
            Id = id,
            Order = order,
            PrerequisiteIds = prerequisiteIds?.ToList() ?? [],
        };

    /// <summary>
    /// Projects the nodes into the path's documented ordering: a valid
    /// topological order when the prerequisite graph is acyclic, otherwise
    /// the plain (Order, Id) sequence. Dependencies always appear before the
    /// modules that depend on them when the graph is valid.
    /// </summary>
    public static (List<ModuleNode> Ordered, bool GraphIsValid) OrderModules(
        IEnumerable<ModuleNode> modules)
    {
        // (Order, Id): deterministic ordering for otherwise independent topics.
        var nodes = modules
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id)
            .ToList();

        // Children map: prerequisite -> modules that depend on it.
        var children = new Dictionary<int, List<int>>();
        foreach (var node in nodes)
        {
            foreach (var prereq in node.PrerequisiteIds.Distinct())
            {
                if (!children.TryGetValue(prereq, out var dependents))
                    children[prereq] = dependents = [];
                dependents.Add(node.Id);
            }
        }

        if (DagValidator.TryTopologicalSort(children, nodes.Select(n => n.Id), out var orderedIds))
        {
            var byId = nodes.ToDictionary(n => n.Id);
            return (orderedIds.Select(id => byId[id]).ToList(), true);
        }

        return (nodes, false);
    }

    /// <summary>Applies the approved unlocking semantics to a target module.</summary>
    /// <param name="orderedModules">Path modules in their documented ordering.</param>
    /// <param name="targetModuleId">Module whose locked/unlocked state is queried.</param>
    /// <param name="completedModuleIds">Modules the user has completed.</param>
    public static bool IsUnlocked(
        IReadOnlyList<ModuleNode> orderedModules,
        int targetModuleId,
        IReadOnlySet<int> completedModuleIds)
    {
        var idx = -1;
        ModuleNode? node = null;
        for (var i = 0; i < orderedModules.Count; i++)
        {
            if (orderedModules[i].Id != targetModuleId) continue;
            idx = i;
            node = orderedModules[i];
            break;
        }

        if (node is null) return false;

        // DAG rule: every prerequisite must be completed.
        if (node.PrerequisiteIds.Count > 0)
            return node.PrerequisiteIds.All(completedModuleIds.Contains);

        // Sequential rule for dependency-free modules: the previous module
        // must be completed. The first module in the path is always unlocked.
        if (idx <= 0) return true;
        return completedModuleIds.Contains(orderedModules[idx - 1].Id);
    }
}
