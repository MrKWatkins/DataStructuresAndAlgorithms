using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace MrKWatkins.DataStructuresAndAlgorithms.Tests;

public sealed class TopologicalOrderExtensionsTests
{
    [Fact]
    public void TopologicalOrder_returns_the_expected_order()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn));

    [Fact]
    public void TopologicalOrder_returns_the_expected_order_using_an_equality_comparer()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, new NodeEqualityComparer()));
        

    [Fact]
    public void TopologicalOrder_returns_the_expected_order_using_a_key_selector()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, node => node.Id));

    [Fact]
    public void TopologicalOrder_returns_the_expected_order_using_a_key_selector_and_an_equality_comparer()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, node => node.Id.ToString(), StringComparer.OrdinalIgnoreCase));

    private static void AssertTopologicalOrder([NotNull] [InstantHandle] Func<IEnumerable<Node>, IEnumerable<Node>> performTopologicalOrder)
    {
        var graph = BuildTestGraph();
        var ordered = performTopologicalOrder(graph).ToList();

        ordered.Should().BeEquivalentTo(graph);

        var positions = ordered.Select((node, index) => (node, index)).ToDictionary(t => t.node, t => t.index);
        foreach (var node in graph)
        foreach (var dependency in node)
        {
            positions[node].Should().BeGreaterThan(positions[dependency], "items should come after the items they depend on.");
        }
    }

    [Fact]
    public void TopologicalOrder_throws_if_dependentOnSelector_returns_null()
        => Assert.Throws<ArgumentException>(() => BuildTestGraph().TopologicalOrder(_ => null).ToList())
            .ParamName.Should().Be("dependentOnSelector");

    [Fact]
    public void TopologicalOrder_throws_if_key_selector_returns_null()
        => Assert.Throws<ArgumentException>(() => BuildTestGraph().TopologicalOrder(node => node.DependentOn, _ => (string) null).ToList())
            .ParamName.Should().Be("keySelector");

    [Fact]
    public void TopologicalOrder_throws_for_immediate_cycle()
    {
        var node = new Node(1);
        node.Add(node);

        var exception = Assert.Throws<CycleException>(() => new [] { node }.TopologicalOrder(n => n.DependentOn).ToList());
        exception.Message.Should().Be($"Cyclic dependency found.{Environment.NewLine}Cycle: 1 -> 1");
        exception.Cycle.Should().BeEquivalentTo([node, node]);
    }

    [Fact]
    public void TopologicalOrder_throws_for_larger_cycle()
    {
        var nodes = new[] { new Node(1), new Node(2), new Node(3) };  
        nodes[0].Add(nodes[1]);
        nodes[1].Add(nodes[2]);
        nodes[2].Add(nodes[0]);

        var exception = Assert.Throws<CycleException>(() => nodes.TopologicalOrder(n => n.DependentOn).ToList());
        exception.Message.Should().Be($"Cyclic dependency found.{Environment.NewLine}Cycle: 1 -> 2 -> 3 -> 1");
        exception.Cycle.Should().BeEquivalentTo([nodes[0], nodes[1], nodes[2], nodes[0]]);
    }

    [Pure]
    [NotNull]
    [ItemNotNull]
    private static IReadOnlyCollection<Node> BuildTestGraph()
    {
        // Example taken from https://en.wikipedia.org/wiki/Topological_sorting#Examples.
        var nodes = new[] { new Node(2), new Node(3), new Node(5), new Node(7), new Node(8), new Node(9), new Node(10), new Node(11) }.ToDictionary(n => n.Id);
        nodes[5].Add(nodes[11]);
        nodes[7].Add(nodes[11], nodes[8]);
        nodes[3].Add(nodes[8], nodes[10]);
        nodes[11].Add(nodes[2], nodes[9], nodes[10]);
        nodes[8].Add(nodes[9]);
        return nodes.Values;
    }

    private sealed class Node(int id) : IEnumerable<Node>
    {
        public int Id { get; } = id;

        [NotNull]
        public List<Node> DependentOn { get; } = new();

        public void Add([NotNull] params Node[] dependentOn) => DependentOn.AddRange(dependentOn);

        public IEnumerator<Node> GetEnumerator() => DependentOn.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Id.ToString();
    }

    private sealed class NodeEqualityComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node x, Node y) => x?.Id == y?.Id;

        public int GetHashCode(Node node) => node.Id.GetHashCode();
    }
}