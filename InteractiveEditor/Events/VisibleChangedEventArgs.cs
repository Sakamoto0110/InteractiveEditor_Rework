namespace InteractiveEditor.Events;

// The rule of a node (VisibleWhen) gave another answer at a read, so the row shows or hides now
// (P6.2); the view reads Visible and lays the rows out again. The source is the read that found it.
public sealed class VisibleChangedEventArgs(InspectorNode node, ValueSource source) : InspectorEventArgs(node.Inspector)
{
    public InspectorNode Node { get; } = node;
    public ValueSource Source { get; } = source;
}
