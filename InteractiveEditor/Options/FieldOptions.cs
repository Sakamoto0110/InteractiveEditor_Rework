using InteractiveEditor.Model;

namespace InteractiveEditor.Options;

public sealed class FieldOptions
{
    internal FieldOptions(FieldDescriptor member, bool hasMembers)
    {
        Member = member;
        Path = member.FullPath;
        Label = member.Name;
        HasMembers = hasMembers;
    }

    public string Path { get; }
    public FieldDescriptor? Member { get; }
    public bool HasMembers { get; }
    public bool IsGroup => HasMembers && Expandable;

    public string Label { get; set; }
    public string? Tooltip { get; set; }
    public string? Help { get; set; }
    public int Order { get; set; }
    public bool Ignored { get; set; }
    public bool Visible { get; set; } = true;
    public bool ReadOnly { get; set; }
    public EditorKind Editor { get; set; }
    public NumericRange? Range { get; set; }
    public double? ScrubMultiplier { get; set; }
    public bool Expandable { get; set; }
    public bool Collapsed { get; set; }

    public override string ToString() => Path;
}
