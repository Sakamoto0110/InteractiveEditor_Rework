using InteractiveEditor.Model;

namespace InteractiveEditor.Binding;

public class Fieldset : InspectorNode
{
    
    public override void bind<T>(T instance)
    {
        Instance = instance;
    }
    

    public override object? GetValue()
    {
        if (Instance == null)
            return null;

        return Descriptor?.Accessors.Getter?.Invoke(Instance);
    }

    public override void SetValue(object? value)
    {
        if (Instance == null)
            return;

        Descriptor?.Accessors.Setter?.Invoke(Instance, value);
    }
}