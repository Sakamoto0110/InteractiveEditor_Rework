namespace InteractiveEditor.ImGui;

/// <summary>
/// A render loop that drives Dear ImGui. <see cref="Frame"/> is raised once per frame, between ImGui.NewFrame
/// and ImGui.Render, which is where attached views draw themselves.
/// </summary>
public interface IImGuiHost
{
    event Action? Frame;
}
