using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MrKWatkins.DataStructuresAndAlgorithms
{
    public sealed class CycleException : Exception
    {
        internal CycleException([NotNull] string message, [NotNull] [ItemNotNull] [InstantHandle] IEnumerable<object> cycle)
            : base(message)
        {
            Cycle = cycle.ToList();
        }

        public override string Message => $"{base.Message}{Environment.NewLine}Cycle: {string.Join(" -> ", Cycle)}";

        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<object> Cycle { get; }
    }
}