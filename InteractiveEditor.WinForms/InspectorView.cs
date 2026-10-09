using InteractiveEditor.Presentation;

namespace InteractiveEditor.WinForms;

public class InspectorView : IInspectorView
{
    protected System.Windows.Forms.Control Host;

    public InspectorView(Inspector inspector, System.Windows.Forms.Control host)
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
