using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MrKWatkins.DataStructuresAndAlgorithms
{
    public static class TopologicalOrderExtensions
    {
        [Pure]
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> TopologicalOrder<T>([NotNull] [ItemNotNull] this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> dependentOnSelector)
            => source.TopologicalOrder(dependentOnSelector, item => item);
        
        [Pure]
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> TopologicalOrder<T>([NotNull] [ItemNotNull] this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> dependentOnSelector, [NotNull] IEqualityComparer<T> comparer)
            => source.TopologicalOrder(dependentOnSelector, item => item, comparer);
        
        [Pure]
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> TopologicalOrder<T, TKey>([NotNull] [ItemNotNull] this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> dependentOnSelector, [NotNull] Func<T, TKey> keySelector)
            => source.TopologicalOrder(dependentOnSelector, keySelector, EqualityComparer<TKey>.Default);
        
        [Pure]
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<T> TopologicalOrder<T, TKey>([NotNull] [ItemNotNull] this IEnumerable<T> source, [NotNull] Func<T, IEnumerable<T>> dependentOnSelector, [NotNull] Func<T, TKey> keySelector, [NotNull] IEqualityComparer<TKey> keyComparer)
        {
            // Use a stack to keep a track of the path we're currently taking through the graph. This isn't necessary for the algorithm
            // itself but it useful if we hit a cycle as we can then include details of the cycle in the exception we throw.
            var stack = new Stack<T>();
            
            // Keep track of the nodes we've visited. An entry means we've visited it. If the value stored against the node is true then
            // we are currently processing that node and nodes dependent on it. false means we've seen it already but it's not in the 
            // current path through the graph.
            var visited = new Dictionary<TKey, bool>(keyComparer);

            return source.SelectMany(item => Visit(item, dependentOnSelector, keySelector, stack, visited));
        }

        [Pure]
        [NotNull]
        private static IEnumerable<T> Visit<T, TKey>([NotNull] T node, [NotNull] Func<T, IEnumerable<T>> dependentOnSelector, [NotNull] Func<T, TKey> keySelector, [NotNull] Stack<T> stack, [NotNull] Dictionary<TKey, bool> visited)
        {
            // Store the current node in the stack.
            stack.Push(node);

            var key = keySelector(node);
            if (ReferenceEquals(key, null))
            {
                throw new ArgumentException($"Value returned null for {node}.", nameof(keySelector));
            }

            // Have we already visited this node?
            if (visited.TryGetValue(key, out var inProcess))
            {
                // Yes we have. Are we currently processing that node or any node dependent on it?
                if (inProcess)
                {
                    // Yes we are. That means we have a cycle, as this node is dependent upon itself.
                    throw new CycleException("Cyclic dependency found.", stack.Cast<object>().Reverse());
                }
                
                // If we reach here we have visited the node already so don't need to do anything further.
            }
            else
            {
                // Not yet visited this node. Mark it as currently being processed.
                visited[key] = true;

                // Find all the nodes that depend on this one.
                var nodesDependentOn = dependentOnSelector(node) ?? throw new ArgumentException($"Value returned null for {node}.", nameof(dependentOnSelector));

                // Topologically order those nodes and return them.
                foreach (var nodeDependentOn in nodesDependentOn.SelectMany(c => Visit(c, dependentOnSelector, keySelector, stack, visited)))
                {
                    yield return nodeDependentOn;
                }

                // We have now returned all nodes dependent on this one so we are safe to return this one.
                yield return node;

                // Mark it as having been visited, but we're not currently processing it or those that depend on it.
                visited[key] = false;
            }

            // Pop the node from the stack.
            stack.Pop();
        }
    }
}
