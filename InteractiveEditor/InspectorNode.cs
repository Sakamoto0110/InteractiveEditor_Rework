using InteractiveEditor.Model;
using InteractiveEditor.Options;

namespace InteractiveEditor;

public abstract class InspectorNode
{
    public virtual string Name => Descriptor?.Name ?? " -- ";
    public string Path => Descriptor?.FullPath ?? string.Empty;
    public FieldDescriptor?  Descriptor { get; init; }
    public Inspector? Parent { get; internal set; }

    // Options, in layers: reflection < attributes < whatever the caller sets afterwards.
    public string Label { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public string? Help { get; set; }
    public int Order { get; set; }
    public bool Ignored { get; set; }
    public bool Visible { get; set; } = true;
    public bool ReadOnly { get; set; }
    public EditorKind Editor { get; set; }
    public NumericRange? Range { get; set; }
    public double? ScrubMultiplier { get; set; }
    public bool Expandable { get; set; }
    public bool Collapsed { get; set; }

    public virtual bool IsGroup => false;

    // A path relative to this node ("Moo.MooY"); chaining works too: node["Moo"]["MooY"].
    public InspectorNode this[string path]
    {
        get
        {
            InspectorNode node = this;

            foreach (var name in path.Split('.'))
            {
                node = (node as Inspector)?.Child(name)
                    ?? throw new KeyNotFoundException($"'{Name}' has no field at path '{path}'.");
            }

            return node;
        }
    }

    public virtual object? GetValue()
    {
        var owner = Parent?.GetValue();

        if (owner == null)
            return null;

        return Descriptor?.Accessors.Getter?.Invoke(owner);
    }

    public virtual void SetValue(object? value)
    {
        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        var owner = Parent?.GetValue()
            ?? throw new InvalidOperationException($"Cannot set '{Name}': '{Parent?.Name}' is null.");

        var setter = Descriptor?.Accessors.Setter
            ?? throw new InvalidOperationException($"'{Name}' is read-only.");

        setter(owner, value);

        // A struct owner is a boxed copy, so it has to be written back into its own owner.
        if (owner.GetType().IsValueType)
            Parent!.SetValue(owner);
    }
}



 