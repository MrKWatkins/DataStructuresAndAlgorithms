namespace MrKWatkins.DataStructuresAndAlgorithms;

/// <summary>Indicates that the dependencies contain a cycle.</summary>
public sealed class CycleException : Exception
{
    internal CycleException(string message, [InstantHandle] IEnumerable<object> cycle)
        : base(message)
    {
        Cycle = cycle.ToList();
    }

    /// <summary>Gets the error message, including the path through the cycle.</summary>
    public override string Message => $"{base.Message}{Environment.NewLine}Cycle: {string.Join(" -> ", Cycle)}";

    /// <summary>Gets the path that encountered the cycle, with the repeated item at the end.</summary>
    public IReadOnlyList<object> Cycle { get; }
}
