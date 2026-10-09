namespace InteractiveEditor.ImGui;

// A render loop that drives Dear ImGui. Frame is raised once per frame, between ImGui.NewFrame and
// ImGui.Render, which is where the views attached to it draw themselves.
public interface IImGuiHost
{
    event Action? Frame;
}
