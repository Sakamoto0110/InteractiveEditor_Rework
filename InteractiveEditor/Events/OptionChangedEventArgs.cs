namespace InteractiveEditor.Events;

// An option changed, so a view shows it again (P7.8): one of a node, or one of the inspector's own
// options, with Node null. Option is the name of the property, as in nameof(InspectorNode.Label).
public sealed class OptionChangedEventArgs(Inspector inspector, InspectorNode? node, string option) : InspectorEventArgs(inspector)
{
    public InspectorNode? Node { get; } = node;
    public string Option { get; } = option;
}
