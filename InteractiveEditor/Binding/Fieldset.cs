using InteractiveEditor.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace InteractiveEditor.Binding;

public class Fieldset
{
    public object? Instance { get; set; }
    public FieldDescriptor Descriptor { get; set; }
    public void Bind(  object? instance)
    {
        Instance = instance;
    }
    public object? GetValue() 
    {
        return Descriptor.Accessors.Getter?.Invoke(Instance);
    }
    public void SetValue(object? value)
    {
        Descriptor.Accessors.Setter?.Invoke(Instance, value);
    }
}
