namespace InteractiveEditor.Events;

// Where a change of value came from, so a view can tell its own edits from the rest.
public enum ValueSource
{
    // Written through the inspector into the objects (SetValue, or Apply() for a pending value).
    Write,

    // Written through the inspector and held in the node until Apply(), with ViewToInstance off: the
    // objects did not change.
    Pending,

    // Reported by the object itself (INotifyPropertyChanged).
    Instance,

    // Found by a Refresh().
    Refresh,

    // Read again by Reload(), which also drops the pending values.
    Reload,

    // Set by a forced operation.
    Force,

    // The item chosen in a collection changed, and the rows below it read another one (P5.10).
    Selection,
}
