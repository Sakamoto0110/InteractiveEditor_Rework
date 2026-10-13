using System.Reflection;
using PixieInspector.Options.Policies;

namespace PixieInspector;

// A value added by hand with its own reader and writer (AddField): no member behind it, read and written
// as a member is, so it takes the options, the rules, the events and several bound objects like any other.
// The reader and the writer get the object the node sits in, in each bound object: the bound object itself
// under the root. It is how a host shows what reflection cannot see, such as the fields a native
// descriptor lists. With no writer, the node is read-only.
public sealed class FieldNode : MemberNode
{
    private readonly Func<object, object?> read;
    private readonly Action<object, object?>? write;
    private readonly Type valueType;

    internal FieldNode(InspectorNode parent, string name, Type valueType, Func<object, object?> read, Action<object, object?>? write)
        : base(parent, name, byName: false)
    {
        this.valueType = valueType;
        this.read = read;
        this.write = write;
        Editor = ReflectionPolicy.EditorFor(this);
    }

    public override Type ValueType => valueType;

    private protected override bool FixedReadOnly => write == null;

    internal override object? Resolve(object? instance)
    {
        var owner = Parent!.Resolve(instance);
        return owner == null ? null : read(owner);
    }

    // The writer gets the owner itself, so a struct owner would only take the value into a copy: the
    // object the field sits in has to be a class.
    internal override void WriteTo(object? instance, object? value)
    {
        if (Locked is { } locked)
            throw new InvalidOperationException(locked);

        var owner = Parent!.Resolve(instance)
            ?? throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is null.");

        if (owner.GetType().IsValueType)
            throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is a struct, and a field added by hand writes only into a class.");

        Writing = true;

        try
        {
            write!(owner, value);
        }
        catch (Exception e)
        {
            // Wrapped as reflection wraps what a setter throws, so the write reports it on the node.
            throw new TargetInvocationException(e);
        }
        finally
        {
            Writing = false;
        }
    }
}
