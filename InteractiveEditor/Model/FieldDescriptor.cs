using System.Reflection;

namespace InteractiveEditor.Model;

public class FieldDescriptor
{
    // The member's own type, as declared (a Nullable<T> member keeps Nullable<T>).
    public Type Type { get; init; }
    // The type the member was discovered on, i.e. the type of the owner instance.
    public Type OwnerType { get; init; }
    public string Name { get; init; }
    public string Path { get; init; }
    public string FullPath { get; init; }
    public MemberTypes MemberType { get; init; }

    public FieldAccessors Accessors { get; init; }
    public Func<object?, object?> OwnerGetter { get; init; }

    public class FieldAccessors
    {
        public Func<object?, object?>? Getter;
        public Action<object?, object?>? Setter;
    }
}