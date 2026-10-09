namespace InteractiveEditor.WinForms;

// Makes the WinForms view of an inspector (P7.2, P7.7). An extension, so the Inspector stays the same in
// both targets, and everything of the view stays in its folder; the WPF one has a name of its own.
public static class WinFormsViewFactory
{
    public static WinFormsInspectorView CreateWinFormsView(this Inspector inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        return new WinFormsInspectorView(inspector);
    }
}
