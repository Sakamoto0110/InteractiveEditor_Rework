using InteractiveEditor.Events;

namespace InteractiveEditor.Diagnostics;

// A failure at a weak spot: what happened, why, what to do about it, and where.
public sealed class InspectorFailureEventArgs(
    Inspector inspector,
    string path,
    FailureSeverity severity,
    Exception exception,
    string message,
    string? suggestion) : InspectorEventArgs(inspector)
{
    // The node's path ("Moo.MooX"); empty for the inspector as a whole.
    public string Path { get; } = path;
    public FailureSeverity Severity { get; } = severity;
    public Exception Exception { get; } = exception;
    public string Message { get; } = message;
    public string Reason => Exception.Message;
    public string? Suggestion { get; } = suggestion;

    // Set by a subscriber that fixed the state; a worked-around failure then no longer counts as
    // left to its fallback.
    public bool Handled { get; set; }
}
