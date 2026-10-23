namespace LearnPath.API.Algorithms.Graph;

public static class DagValidator
{
    public static bool WouldCreateCycle(
        Dictionary<int, List<int>> adjacency, int from, int to)
    {
        var visited = new HashSet<int>();
        return Dfs(adjacency, to, from, visited);
    }

    private static bool Dfs(
        Dictionary<int, List<int>> adjacency,
        int current,
        int target,
        HashSet<int> visited)
    {
        if (current == target) return true;
        if (!visited.Add(current)) return false;

        if (!adjacency.TryGetValue(current, out var neighbors))
            return false;

        return neighbors.Any(n => Dfs(adjacency, n, target, visited));
    }

    /// <summary>
    /// Kahn's algorithm topological sort with deterministic tie-breaking.
    /// </summary>
    /// <remarks>
    /// Adjacency maps a node to the nodes that depend on it (children/edges
    /// pointing away from prerequisites), matching the DFS used by
    /// <see cref="WouldCreateCycle"/>. <paramref name="nodesInTieBreakOrder"/>
    /// must contain every node exactly once, already ordered by the desired
    /// tie-breaker (e.g. Module.Order, then Id). Among nodes that are
    /// simultaneously available, the algorithm always emits the earliest one
    /// in that sequence, so independent topics keep a stable, documented
    /// ordering.
    /// </remarks>
    public static bool TryTopologicalSort(
        Dictionary<int, List<int>> adjacency,
        IEnumerable<int> nodesInTieBreakOrder,
        out List<int> sorted)
    {
        var nodeList = nodesInTieBreakOrder.ToList();
        var inGraph = nodeList.ToHashSet();

        // Distinct edges keep in-degree accounting correct even if the
        // adjacency contains duplicate entries (composite PK prevents this
        // in the database, but the helper is used with in-memory graphs too).
        var edges = new Dictionary<int, HashSet<int>>();
        var inDegree = nodeList.ToDictionary(n => n, _ => 0);
        foreach (var (node, neighbors) in adjacency)
        {
            if (!inGraph.Contains(node) || neighbors.Count == 0) continue;
            var distinct = edges.TryGetValue(node, out var set)
                ? set
                : edges[node] = [];
            foreach (var neighbor in neighbors)
            {
                if (!inGraph.Contains(neighbor) || !distinct.Add(neighbor)) continue;
                inDegree[neighbor]++;
            }
        }

        var remaining = new List<int>(nodeList);
        var result = new List<int>(nodeList.Count);

        while (remaining.Count > 0)
        {
            // First remaining node (in tie-break order) whose prerequisites
            // are all emitted already.
            var pickedIdx = -1;
            for (var i = 0; i < remaining.Count; i++)
            {
                if (inDegree[remaining[i]] != 0) continue;
                pickedIdx = i;
                break;
            }

            if (pickedIdx < 0)
            {
                // A directed cycle exists: no valid ordering is possible, so
                // no partial (misleading) order is returned.
                sorted = [];
                return false;
            }

            var node = remaining[pickedIdx];
            remaining.RemoveAt(pickedIdx);
            result.Add(node);

            if (edges.TryGetValue(node, out var children))
                foreach (var child in children)
                    inDegree[child]--;
        }

        sorted = result;
        return true;
    }

    /// <summary>
    /// Topological sort without an explicit tie-break order. Nodes are
    /// iterated in dictionary encounter order. Throws when the graph
    /// contains a directed cycle.
    /// </summary>
    public static List<int> TopologicalSort(Dictionary<int, List<int>> adjacency)
    {
        var allNodes = adjacency.Keys
            .Concat(adjacency.Values.SelectMany(v => v))
            .Distinct()
            .ToList();

        if (!TryTopologicalSort(adjacency, allNodes, out var sorted))
            throw new InvalidOperationException("Cycle detected in learning path graph.");

        return sorted;
    }
}