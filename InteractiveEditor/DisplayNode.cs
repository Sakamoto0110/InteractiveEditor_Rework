using InteractiveEditor.Options;

namespace InteractiveEditor;

// A value shown by hand (P1.6): no member behind it, only a getter, read like a member is: a Refresh()
// or a Reload() reads it again, and a getter that throws is reported on the row.
public sealed class DisplayNode : InspectorNode
{
    private readonly Func<object?> read;

    internal DisplayNode(InspectorNode parent, string name, Func<object?> read) : base(parent, name)
    {
        this.read = read;
        Label = name;
        Editor = EditorKind.Display;
        ReadOnly = true;
    }

    public override void SetValue(object? value)
    {
        throw new InvalidOperationException($"'{Name}' only shows a value; there is nothing to write to.");
    }

    // The same value for every bound object: the getter does not look at them.
    internal override object? Resolve(object? instance) => read();

    internal override void WriteTo(object? instance, object? value)
    {
        throw new InvalidOperationException($"'{Name}' only shows a value; there is nothing to write to.");
    }
}
