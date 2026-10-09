namespace InteractiveEditor.Diagnostics;

// What went wrong while an inspector was created, kept for whoever checks after the Create.
public sealed class InspectorReport
{
    private readonly List<InspectorFailureEventArgs> Entries = [];

    public IReadOnlyList<InspectorFailureEventArgs> Failures => Entries;

    public int ErrorCount => Entries.Count;

    // Worked-around failures nobody handled: their fallback is what stands.
    public int UnhandledErrorCount => Entries.Count(f => f.Severity == FailureSeverity.WorkedAround && !f.Handled);

    // Failures with no fallback: each one left a node out.
    public int CriticalErrorCount => Entries.Count(f => f.Severity == FailureSeverity.Critical);

    internal void Add(InspectorFailureEventArgs failure)
    {
        Entries.Add(failure);
    }
}
