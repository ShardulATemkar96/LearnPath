namespace LearnPath.API.Algorithms.Graph;

public static class DagValidator
{
    public static bool WouldCreateCycle(
        Dictionary<int, List<int>> adjacency,
        int from,
        int to)
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

    public static List<int> TopologicalSort(Dictionary<int, List<int>> adjacency)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        var allNodes = adjacency.Keys
            .Concat(adjacency.Values.SelectMany(v => v))
            .Distinct();

        foreach (var node in allNodes)
            if (!visited.Contains(node))
                TopoDfs(adjacency, node, visited, stack, new HashSet<int>());

        return stack.ToList();
    }

    private static void TopoDfs(
        Dictionary<int, List<int>> adjacency,
        int node,
        HashSet<int> visited,
        Stack<int> stack,
        HashSet<int> inStack)
    {
        visited.Add(node);
        inStack.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (inStack.Contains(neighbor))
                    throw new InvalidOperationException("Cycle detected in learning path graph.");
                if (!visited.Contains(neighbor))
                    TopoDfs(adjacency, neighbor, visited, stack, inStack);
            }
        }

        inStack.Remove(node);
        stack.Push(node);
    }
}