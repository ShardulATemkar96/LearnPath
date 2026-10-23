using FluentAssertions;
using LearnPath.API.Algorithms.Graph;
using Xunit;

namespace LearnPath.Tests.Algorithms;

public class DagValidatorTests
{
    [Fact]
    public void WouldCreateCycle_EmptyGraph_ReturnsFalse()
    {
        var adjacency = new Dictionary<int, List<int>>();
        DagValidator.WouldCreateCycle(adjacency, 1, 2).Should().BeFalse();
    }

    [Fact]
    public void WouldCreateCycle_NoCycle_ReturnsFalse()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
        };

        DagValidator.WouldCreateCycle(adjacency, 3, 4).Should().BeFalse();
    }

    [Fact]
    public void WouldCreateCycle_DirectCycle_ReturnsTrue()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
        };

        DagValidator.WouldCreateCycle(adjacency, 2, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_IndirectCycle_ReturnsTrue()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
        };

        DagValidator.WouldCreateCycle(adjacency, 3, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_SelfLoop_ReturnsTrue()
    {
        var adjacency = new Dictionary<int, List<int>>();
        DagValidator.WouldCreateCycle(adjacency, 1, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_LargeGraph_NoCycle_ReturnsFalse()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] }, { 2, [3] }, { 3, [4] }, { 4, [5] },
        };

        DagValidator.WouldCreateCycle(adjacency, 5, 6).Should().BeFalse();
    }

    [Fact]
    public void TopologicalSort_LinearChain_ReturnsCorrectOrder()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
            { 3, [] },
        };

        var result = DagValidator.TopologicalSort(adjacency);

        result.Should().NotBeEmpty();
        result.IndexOf(1).Should().BeLessThan(result.IndexOf(2));
        result.IndexOf(2).Should().BeLessThan(result.IndexOf(3));
    }

    [Fact]
    public void TopologicalSort_EmptyGraph_ReturnsEmpty()
    {
        var adjacency = new Dictionary<int, List<int>>();
        var result = DagValidator.TopologicalSort(adjacency);

        result.Should().BeEmpty();
    }

    [Fact]
    public void TopologicalSort_GraphWithCycle_ThrowsInvalidOperation()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
            { 3, [1] },
        };

        var act = () => DagValidator.TopologicalSort(adjacency);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cycle detected*");
    }

    [Fact]
    public void TopologicalSort_BranchedGraph_AllNodesPresent()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [3] },
            { 2, [3] },
            { 3, [] },
        };

        var result = DagValidator.TopologicalSort(adjacency);

        result.Should().Contain([1, 2, 3]);
        result.IndexOf(3).Should()
              .BeGreaterThan(result.IndexOf(1))
              .And.BeGreaterThan(result.IndexOf(2));
    }

    /* ── TryTopologicalSort (Kahn's, tie-break order) ────── */

    [Fact]
    public void TryTopologicalSort_AcyclicChain_PlacesPrerequisitesFirst()
    {
        // Children map: 1 must be completed before 2, 2 before 3.
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
        };

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 3, 2, 1 }, out var sorted);

        ok.Should().BeTrue();
        sorted.Should().Equal([1, 2, 3]);
    }

    [Fact]
    public void TryTopologicalSort_IndependentNodes_UsesTieBreakOrder()
    {
        var adjacency = new Dictionary<int, List<int>>();

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 7, 3, 9 }, out var sorted);

        ok.Should().BeTrue();
        sorted.Should().Equal([7, 3, 9]);
    }

    [Fact]
    public void TryTopologicalSort_BranchedGraph_TieBreakChoosesEarliestAvailable()
    {
        // 1 and 2 are both roots; 4 depends on 2, 3 depends on 1.
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [3] },
            { 2, [4] },
        };

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 2, 1, 3, 4 }, out var sorted);

        ok.Should().BeTrue();
        sorted[0].Should().Be(2);
        sorted.IndexOf(4).Should().BeGreaterThan(sorted.IndexOf(2));
        sorted.IndexOf(3).Should().BeGreaterThan(sorted.IndexOf(1));
    }

    [Fact]
    public void TryTopologicalSort_LongCycle_ReturnsFalseAndNoOrder()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [2] },
            { 2, [3] },
            { 3, [1] },
        };

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 1, 2, 3 }, out var sorted);

        ok.Should().BeFalse();
        sorted.Should().BeEmpty();
    }

    [Fact]
    public void TryTopologicalSort_SelfLoop_ReturnsFalse()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [1] },
        };

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 1 }, out var sorted);

        ok.Should().BeFalse();
        sorted.Should().BeEmpty();
    }

    [Fact]
    public void TryTopologicalSort_EmptyGraph_ReturnsTrueAndEmptyOrder()
    {
        var adjacency = new Dictionary<int, List<int>>();

        var ok = DagValidator.TryTopologicalSort(
            adjacency, Array.Empty<int>(), out var sorted);

        ok.Should().BeTrue();
        sorted.Should().BeEmpty();
    }

    [Fact]
    public void TryTopologicalSort_DuplicateEdges_DoNotCorruptInDegree()
    {
        var adjacency = new Dictionary<int, List<int>>
        {
            { 1, [3, 3, 3] },
            { 2, [3] },
        };

        var ok = DagValidator.TryTopologicalSort(
            adjacency, new[] { 1, 2, 3 }, out var sorted);

        ok.Should().BeTrue();
        sorted.Should().Contain([1, 2, 3]);
        sorted.IndexOf(3).Should().Be(2);
    }
}
