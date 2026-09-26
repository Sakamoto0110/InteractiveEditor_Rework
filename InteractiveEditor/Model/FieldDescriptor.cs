using System.Reflection;

namespace InteractiveEditor.Model;

public class FieldDescriptor
{
    public required Type Type { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string FullPath { get; init; }
    public required MemberTypes MemberType { get; init; }
    public required MemberInfo MemberInfo { get; init; }
    public required Type FieldType { get; init; }

    public required FieldAccessors Accessors { get; init; }
    public required Func<object?, object?> OwnerGetter { get; init; }

    public class FieldAccessors
    {
        public Func<object?, object?>? Getter;
        public Action<object?, object?>? Setter;
    }
}