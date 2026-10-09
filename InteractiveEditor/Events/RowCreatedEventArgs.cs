namespace InteractiveEditor.Events;

// A view finished a node's row (P7.4), placed and showing its value, with every control it made for
// it: a row has several, so this is the moment to handle them together. The editor is null for a
// group or a header, and the panel is null for anything but a group.
public sealed class RowCreatedEventArgs<TControl>(InspectorNode node, TControl label, TControl help, TControl? editor, TControl? panel)
    : InspectorEventArgs(node.Inspector) where TControl : class
{
    public InspectorNode Node { get; } = node;
    public string Path => Node.Path;
    public TControl Label { get; } = label;
    public TControl Help { get; } = help;
    public TControl? Editor { get; } = editor;
    public TControl? Panel { get; } = panel;
}
