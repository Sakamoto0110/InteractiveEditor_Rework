using System;
using System.Collections.Generic;
using System.Text;

namespace InteractiveEditor.Presentation.WF;

internal class InspectorView : Inspector, Presentation.InspectorView
{
    public InspectorView(System.Windows.Forms.Control host)
    {
        Host = host;
        
    }
    public   void AttatchToHost(object host)
    {
        throw new NotImplementedException();
    }
}
