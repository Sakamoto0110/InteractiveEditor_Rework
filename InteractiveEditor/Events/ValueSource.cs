namespace InteractiveEditor.Events;

// Where a change of value came from, so a view can tell its own edits from the rest.
public enum ValueSource
{
    // Written through the inspector (SetValue).
    Write,

    // Reported by the object itself (INotifyPropertyChanged).
    Instance,

    // Found by a Refresh().
    Refresh,

    // Set by a forced operation.
    Force,
}
