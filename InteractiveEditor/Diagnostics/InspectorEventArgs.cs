namespace InteractiveEditor.Diagnostics;

public class InspectorEventArgs(Inspector inspector) : EventArgs
{
    // The inspector that raised the event; its Id tells it apart from the others.
    public Inspector Inspector { get; } = inspector;
}
