using System.Collections;

namespace MrKWatkins.DataStructuresAndAlgorithms.Tests;

public sealed class TopologicalOrderExtensionsTests
{
    [Test]
    public void TopologicalOrder_returns_the_expected_order()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn));

    [Test]
    public void TopologicalOrder_returns_the_expected_order_using_an_equality_comparer()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, new NodeEqualityComparer()));


    [Test]
    public void TopologicalOrder_returns_the_expected_order_using_a_key_selector()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, node => node.Id));

    [Test]
    public void TopologicalOrder_returns_the_expected_order_using_a_key_selector_and_an_equality_comparer()
        => AssertTopologicalOrder(graph => graph.TopologicalOrder(node => node.DependentOn, node => node.Id.ToString(), StringComparer.OrdinalIgnoreCase));

    private static void AssertTopologicalOrder([InstantHandle] Func<IEnumerable<Node>, IEnumerable<Node>> performTopologicalOrder)
    {
        var graph = BuildTestGraph();
        var ordered = performTopologicalOrder(graph).ToList();

        ordered.Count.Should().Equal(graph.Count);
        foreach (var node in graph)
        {
            ordered.Should().Contain(node);
        }

        var positions = ordered.Select((node, index) => (node, index)).ToDictionary(t => t.node, t => t.index);
        foreach (var node in graph)
            foreach (var dependency in node)
            {
                positions[node].Should().BeGreaterThan(positions[dependency]);
            }
    }

    [Test]
    public void TopologicalOrder_throws_if_dependentOnSelector_returns_null()
        => AssertThat.Invoking(() => BuildTestGraph().TopologicalOrder(_ => null!).ToList()).Should().Throw<ArgumentException>().Exception
            .ParamName.Should().Equal("dependentOnSelector");

    [Test]
    public void TopologicalOrder_throws_if_key_selector_returns_null()
        => AssertThat.Invoking(() => BuildTestGraph().TopologicalOrder(node => node.DependentOn, _ => (string)null!).ToList()).Should().Throw<ArgumentException>().Exception
            .ParamName.Should().Equal("keySelector");

    [Test]
    public void TopologicalOrder_throws_for_immediate_cycle()
    {
        var node = new Node(1);
        node.Add(node);

        var exception = AssertThat.Invoking(() => new[] { node }.TopologicalOrder(n => n.DependentOn).ToList()).Should().Throw<CycleException>().Exception;
        exception.Message.Should().Equal($"Cyclic dependency found.{Environment.NewLine}Cycle: 1 -> 1");
        exception.Cycle.Should().SequenceEqual([node, node]);
    }

    [Test]
    public void TopologicalOrder_throws_for_larger_cycle()
    {
        var nodes = new[] { new Node(1), new Node(2), new Node(3) };
        nodes[0].Add(nodes[1]);
        nodes[1].Add(nodes[2]);
        nodes[2].Add(nodes[0]);

        var exception = AssertThat.Invoking(() => nodes.TopologicalOrder(n => n.DependentOn).ToList()).Should().Throw<CycleException>().Exception;
        exception.Message.Should().Equal($"Cyclic dependency found.{Environment.NewLine}Cycle: 1 -> 2 -> 3 -> 1");
        exception.Cycle.Should().SequenceEqual([nodes[0], nodes[1], nodes[2], nodes[0]]);
    }

    [Pure]
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

        public List<Node> DependentOn { get; } = new();

        public void Add(params Node[] dependentOn) => DependentOn.AddRange(dependentOn);

        public IEnumerator<Node> GetEnumerator() => DependentOn.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Id.ToString();
    }

    private sealed class NodeEqualityComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node? x, Node? y) => x?.Id == y?.Id;

        public int GetHashCode(Node node) => node.Id.GetHashCode();
    }
}
