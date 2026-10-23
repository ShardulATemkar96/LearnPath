using FluentAssertions;
using LearnPath.API.Algorithms.Graph;
using Xunit;

namespace LearnPath.Tests.Algorithms;

public class ModuleUnlockRulesTests
{
    /* ── OrderModules ────────────────────────────────────── */

    [Fact]
    public void OrderModules_AcyclicGraph_DependenciesPrecedeDependents()
    {
        // Module with Order 5 depends on module with Order 9. Topological
        // order must still emit the prerequisite first.
        var nodes = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 2, order: 5, [1]),
            ModuleUnlockRules.Module(id: 1, order: 9),
        };

        var (ordered, valid) = ModuleUnlockRules.OrderModules(nodes);

        valid.Should().BeTrue();
        ordered.Select(n => n.Id).Should().Equal([1, 2]); // prereq before dependent
    }

    [Fact]
    public void OrderModules_IndependentTopics_UsesOrderThenIdTieBreak()
    {
        var nodes = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 3, order: 1),
            ModuleUnlockRules.Module(id: 2, order: 1),
            ModuleUnlockRules.Module(id: 1, order: 0),
        };

        var (ordered, valid) = ModuleUnlockRules.OrderModules(nodes);

        valid.Should().BeTrue();
        ordered.Select(n => n.Id).Should().Equal([1, 2, 3]);
    }

    [Fact]
    public void OrderModules_CyclicGraph_FallsBackToOrderAndFlagsInvalid()
    {
        var nodes = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1, [2]),
            ModuleUnlockRules.Module(id: 2, order: 2, [1]),
            ModuleUnlockRules.Module(id: 3, order: 3),
        };

        var (ordered, valid) = ModuleUnlockRules.OrderModules(nodes);

        valid.Should().BeFalse();
        // Fallback: deterministic (Order, Id) sequence.
        ordered.Select(n => n.Id).Should().Equal([1, 2, 3]);
    }

    /* ── IsUnlocked ──────────────────────────────────────── */

    [Fact]
    public void IsUnlocked_TopicalModuleWithIncompletePrerequisite_RemainsLocked()
    {
        var modules = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1),
            ModuleUnlockRules.Module(id: 2, order: 2, [1]),
            ModuleUnlockRules.Module(id: 3, order: 3),
        };
        var (ordered, _) = ModuleUnlockRules.OrderModules(modules);

        ModuleUnlockRules.IsUnlocked(ordered, 2, new HashSet<int>())
            .Should().BeFalse("the prerequisite has not been completed");

        ModuleUnlockRules.IsUnlocked(ordered, 2, new HashSet<int> { 1 })
            .Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_AllPrerequisitesRequired_MultiPrerequisiteModule()
    {
        var modules = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1),
            ModuleUnlockRules.Module(id: 2, order: 2),
            ModuleUnlockRules.Module(id: 3, order: 3, [1, 2]),
        };
        var (ordered, _) = ModuleUnlockRules.OrderModules(modules);

        ModuleUnlockRules.IsUnlocked(ordered, 3, new HashSet<int> { 1 })
            .Should().BeFalse("only one of two prerequisites is done");
        ModuleUnlockRules.IsUnlocked(ordered, 3, new HashSet<int> { 1, 2 })
            .Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_DependencyFreeModule_FollowsSequentialRule()
    {
        var modules = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1),
            ModuleUnlockRules.Module(id: 2, order: 2),
            ModuleUnlockRules.Module(id: 3, order: 3),
        };
        var (ordered, _) = ModuleUnlockRules.OrderModules(modules);


        ModuleUnlockRules.IsUnlocked(ordered, 1, new HashSet<int>())
            .Should().BeTrue("the first module has no predecessor");

        ModuleUnlockRules.IsUnlocked(ordered, 2, new HashSet<int>())
            .Should().BeFalse("module 2 needs module 1 completed first");
        ModuleUnlockRules.IsUnlocked(ordered, 2, new HashSet<int> { 1 })
            .Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_CompletedModuleStaysUnlocked()
    {
        var modules = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1),
            ModuleUnlockRules.Module(id: 2, order: 2, [1]),
        };
        var (ordered, _) = ModuleUnlockRules.OrderModules(modules);

        ModuleUnlockRules.IsUnlocked(ordered, 1, new HashSet<int> { 1 })
            .Should().BeTrue();
        ModuleUnlockRules.IsUnlocked(ordered, 2, new HashSet<int> { 1 })
            .Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_UnknownModule_ReturnsFalse()
    {
        var modules = new List<ModuleUnlockRules.ModuleNode>
        {
            ModuleUnlockRules.Module(id: 1, order: 1),
        };
        var (ordered, _) = ModuleUnlockRules.OrderModules(modules);

        ModuleUnlockRules.IsUnlocked(ordered, 99, new HashSet<int> { 1 })
            .Should().BeFalse();
    }
}
