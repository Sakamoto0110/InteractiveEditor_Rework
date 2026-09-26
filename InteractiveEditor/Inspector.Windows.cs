namespace InteractiveEditor;

public partial class Inspector
{
    public static Inspector Create<T>(System.Windows.Forms.Control host)
    {
        var inspector = new Presentation.WF.InspectorView(host);

         

        return inspector;
    }

    public static Inspector Create<T>(System.Windows.Controls.Control host)
    {
        var inspector = new Presentation.WPF.InspectorView(host);

         

        return inspector;
    }
}
