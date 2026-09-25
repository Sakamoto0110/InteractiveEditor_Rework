using InteractiveEditor.Model;

namespace InteractiveEditor;

public abstract class InspectorNode
{
    protected object? Instance;

    public virtual string Name => Descriptor?.Name ?? " -- ";
    public FieldDescriptor?  Descriptor { get; init; }
    public Inspector? Parent { get; internal set; }
    
    public abstract void bind<T>(T  instance);
     
    public virtual object? GetValue()
    {
        if (Descriptor == null)
            return Instance;

        return Descriptor.Accessors.Getter?.Invoke(Instance);
    }

    public abstract void SetValue(object? value);
}



 