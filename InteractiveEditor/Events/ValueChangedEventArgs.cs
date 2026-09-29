namespace InteractiveEditor.Events;

public sealed class ValueChangedEventArgs(InspectorNode node, ValueSource source) : InspectorEventArgs(node.Inspector)
{
    public InspectorNode Node { get; } = node;
    public ValueSource Source { get; } = source;
}
