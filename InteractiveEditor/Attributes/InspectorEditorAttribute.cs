using InteractiveEditor.Options;

namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorEditorAttribute(EditorKind kind) : Attribute
{
    public EditorKind Kind { get; } = kind;
}
