using InteractiveEditor.Binding;
using InteractiveEditor.Model;
using InteractiveEditor.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InteractiveEditor;

public class Inspector : IEnumerable<Fieldset>
{
    protected object? Host;
    protected Type? Target;



    
    List<FieldDescriptor> Fields;

    private bool IsTypeBound = false;

    protected Inspector() {}
    private IReadOnlyList<Fieldset> Children;
    public static Inspector Create<T>( )
    {
        // Non hosted
        var inspector = new Inspector();

        inspector.Children = ReflectionDiscovery.ResolveFor<T>().Select(f => new Fieldset() { Descriptor = f }).ToList();
        return inspector;
    }

    public void bind<T>(T instance)
    {
        if (IsTypeBound)
            throw new InvalidOperationException("Inspector is already bound to a type.");
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        Target = typeof(T);
        foreach (var child in Children)
        {
            child.Bind(instance);
        }
        IsTypeBound = true;
    }

    public Fieldset this[string fieldName]
    {
        get
        {
            if (Children == null)
                throw new InvalidOperationException("Inspector is not initialized.");
            if(!Children.Any(c => c.Descriptor.Name == fieldName))
                throw new KeyNotFoundException($"Field '{fieldName}' not found in inspector.");
            return Children.First(c => c.Descriptor.Name == fieldName);
        }
    }



    public static Inspector Create<T>(System.Windows.Forms.Control host)
    {
        // WF
        return new Presentation.WF.InspectorView(host) ;
    }

    public static Inspector Create<T>(System.Windows.Controls.Control host)
    {
        // WPF
        return new Presentation.WPF.InspectorView(host);
    }

    public IEnumerator<Fieldset> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Children).GetEnumerator();
    }
}



 