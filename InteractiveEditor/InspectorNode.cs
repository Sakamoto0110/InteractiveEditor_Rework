using InteractiveEditor.Model;
using InteractiveEditor.Options;

namespace InteractiveEditor;

public abstract class InspectorNode
{
    public virtual string Name => Descriptor?.Name ?? " -- ";
    public FieldDescriptor?  Descriptor { get; init; }
    public FieldOptions? Options { get; init; }
    public Inspector? Parent { get; internal set; }

    public virtual object? GetValue()
    {
        var owner = Parent?.GetValue();

        if (owner == null)
            return null;

        return Descriptor?.Accessors.Getter?.Invoke(owner);
    }

    public virtual void SetValue(object? value)
    {
        if (Options?.ReadOnly == true)
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



 