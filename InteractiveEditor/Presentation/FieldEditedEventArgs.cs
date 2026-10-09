using InteractiveEditor.Binding;

namespace InteractiveEditor.Presentation;

public sealed class FieldEditedEventArgs(Fieldset field, Exception? error) : EventArgs
{
    public Fieldset Field { get; } = field;
    // Set when parsing or SetValue failed; the view then shows the error and the current value again.
    public Exception? Error { get; } = error;
}
