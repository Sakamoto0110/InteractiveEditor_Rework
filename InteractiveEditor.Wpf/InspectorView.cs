using InteractiveEditor.Presentation;

namespace InteractiveEditor.Wpf;

public class InspectorView : IInspectorView
{
    protected System.Windows.Controls.Control Host;

    public InspectorView(Inspector inspector, System.Windows.Controls.Control host)
    {
        Inspector = inspector;
        Host = host;
    }

    public Inspector Inspector { get; }

    public void AttachToHost(object host)
    {
        throw new NotImplementedException();
    }
}
