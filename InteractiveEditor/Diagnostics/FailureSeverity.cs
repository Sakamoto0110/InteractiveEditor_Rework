namespace InteractiveEditor.Diagnostics;

// How bad a failure inside the inspector was. Only a fatal one reaches the caller; the others are
// reported by event, and the inspector goes on.
public enum FailureSeverity
{
    // An automatic fallback, with no ambiguity: the result is the right one.
    Recovered,

    // A semi-automatic fallback: nothing broke, but the result may not be the right one. A
    // subscriber can fix the state and mark the failure as handled.
    WorkedAround,

    // No fallback: the node that failed is left out, and the rest goes on.
    Critical,

    // The inspector cannot go on, and the exception is rethrown.
    Fatal,
}
