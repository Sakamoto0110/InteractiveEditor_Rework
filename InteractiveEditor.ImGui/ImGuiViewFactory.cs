namespace InteractiveEditor.ImGui;

// Makes the Dear ImGui view of an inspector (P7.2): the pair of CreateWinFormsView and CreateWpfView, with
// a name of its own, so a file that imports several adapters has no ambiguity.
public static class ImGuiViewFactory
{
    public static ImGuiInspectorView CreateImGuiView(this Inspector inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        return new ImGuiInspectorView(inspector);
    }
}
