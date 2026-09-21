using InteractiveEditor.Model;
using InteractiveEditor.Presentation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace InteractiveEditor;

internal   class Inspector
{
    protected object? Host;
    protected Type? Target;



    InspectorViewModel ViewModel;
    List<FieldDescriptor> Fields;

    private bool IsTypeBound = false;

    protected Inspector() {}

    public static Inspector Create()
    {
        return new Inspector();
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

   
    public static void LazyBind<T>(Inspector inspector)
    {
        if (inspector.IsTypeBound) throw new InvalidOperationException("Inspector is already bound to a type.");

        inspector.Target = typeof(T);
        // Bind the fields of the type T to the inspector's view model
        inspector.IsTypeBound = true;
        
    }



















}



 