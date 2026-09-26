namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorHelpAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}
