namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorTooltipAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}
