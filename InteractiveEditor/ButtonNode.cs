using InteractiveEditor.Diagnostics;
using InteractiveEditor.Options;

namespace InteractiveEditor;

// A button added by hand (P1.6): no member behind it, only an action to run when it is pressed.
public sealed class ButtonNode : InspectorNode
{
    private readonly Action press;

    internal ButtonNode(InspectorNode parent, string name, string text, Action press) : base(parent, name)
    {
        this.press = press;
        Text = text;
        Label = name;
        Editor = EditorKind.Button;
    }

    // What the button says; the row's label is apart from it.
    public string Text { get; set; }

    // Runs the action. A read-only button, or one in a disabled branch, is pressed by mistake, and
    // throws; an action that throws does not bring the inspector down, and the row shows why.
    public void Press()
    {
        ThrowIfCompromised();

        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        try
        {
            press();
            OnWritten();
        }
        catch (Exception e)
        {
            OnBindFailed(FailureSeverity.WorkedAround, e, $"'{Path}' could not run its action.",
                "Check the action given to AddButton.", onWrite: true);
        }
    }

    public override void SetValue(object? value)
    {
        throw new InvalidOperationException($"'{Name}' is a button; call Press().");
    }

    internal override object? Resolve(object? instance) => null;

    internal override void WriteTo(object? instance, object? value)
    {
        throw new InvalidOperationException($"'{Name}' is a button; call Press().");
    }
}
