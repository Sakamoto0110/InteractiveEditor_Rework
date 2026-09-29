namespace InteractiveEditor.Events;

public sealed class BindEventArgs(Inspector inspector, IReadOnlyList<object> instances) : InspectorEventArgs(inspector)
{
    // The objects that came into the bind, or left it.
    public IReadOnlyList<object> Instances { get; } = instances;
}
