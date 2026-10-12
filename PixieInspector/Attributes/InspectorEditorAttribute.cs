using PixieInspector.Options;

namespace PixieInspector.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorEditorAttribute(EditorKind kind) : Attribute
{
    public EditorKind Kind { get; } = kind;
}
