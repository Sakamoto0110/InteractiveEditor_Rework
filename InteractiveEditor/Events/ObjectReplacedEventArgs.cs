namespace InteractiveEditor.Events;

// A group's object was replaced outside the inspector. Unless a subscriber accepts the new object,
// the group's branch is disabled until a Rebind.
public sealed class ObjectReplacedEventArgs(InspectorNode node, object instance, object? previous, object? current)
    : InspectorEventArgs(node.Inspector)
{
    public InspectorNode Node { get; } = node;

    // The bound object in which the group's object changed.
    public object Instance { get; } = instance;

    public object? Previous { get; } = previous;
    public object? Current { get; } = current;

    // Set to follow the new object: the branch stays enabled.
    public bool Accepted { get; set; }
}
