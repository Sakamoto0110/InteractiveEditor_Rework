namespace InteractiveEditor.Avalonia;

// Makes the Avalonia view of an inspector (P7.2): the pair of CreateWinFormsView and CreateWpfView, with a
// name of its own, so a file that imports more than one has no ambiguity.
public static class AvaloniaViewFactory
{
    public static AvaloniaInspectorView CreateAvaloniaView(this Inspector inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        return new AvaloniaInspectorView(inspector);
    }
}
