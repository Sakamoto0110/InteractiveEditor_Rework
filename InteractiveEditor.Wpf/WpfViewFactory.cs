namespace InteractiveEditor.Wpf;

// Makes the WPF view of an inspector (P7.2): the pair of CreateWinFormsView, with a name of its own, so a
// file that imports both has no ambiguity.
public static class WpfViewFactory
{
    public static WpfInspectorView CreateWpfView(this Inspector inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        return new WpfInspectorView(inspector);
    }
}
