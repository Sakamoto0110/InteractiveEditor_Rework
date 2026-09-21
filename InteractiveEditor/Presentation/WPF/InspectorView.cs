using System;
using System.Collections.Generic;
using System.Text;

namespace InteractiveEditor.Presentation.WPF;

internal class InspectorView : Inspector,  Presentation.InspectorView 
{
    public InspectorView(System.Windows.Controls.Control host)
    {
        Host = host;
    }

    public   void AttatchToHost(object host)
    {
        throw new NotImplementedException();
    }
}
