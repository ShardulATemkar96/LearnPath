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
}
