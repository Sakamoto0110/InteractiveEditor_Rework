using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;

namespace InteractiveEditor;

// A field or a property of the object above it. Group or leaf is decided at run time (IsGroup),
// because Expandable can change after the Create.
public sealed class MemberNode : InspectorNode
{
    internal MemberNode(InspectorNode parent, MemberInfo member) : base(parent, member.Name)
    {
        Member = member;
    }

    public MemberInfo Member { get; }

    // The object this group held in every bound object when it was last seen. Only groups of a class
    // type keep it: a struct has no identity to compare.
    private readonly Dictionary<object, object?> Seen = new(ReferenceEqualityComparer.Instance);

    private bool Tracks => !ValueType.IsValueType && HasMembers;

    public override Type ValueType => Member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)Member).PropertyType;

    // Every bound object takes the value.
    public override void SetValue(object? value)
    {
        // A group is edited through its fields; the object behind it is never replaced from here.
        if (IsGroup)
            throw new InvalidOperationException($"'{Name}' is a group; set its fields instead.");

        ThrowIfCompromised();

        // With nothing bound, the write fails like any other null owner.
        if (Root.Instances.Count == 0)
            WriteTo(null, value);

        // A null owner in any of the objects stops the write before any of them changes.
        foreach (var instance in Root.Instances)
        {
            if (Parent!.Resolve(instance) == null)
                throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is null.");
        }

        try
        {
            foreach (var instance in Root.Instances)
                WriteTo(instance, value);

            OnWritten();
        }
        catch (TargetInvocationException e)
        {
            // The setter itself threw: the objects keep whatever it left, and the row shows why.
            OnBindFailed(FailureSeverity.WorkedAround, Unwrap(e), $"'{Path}' could not be written.",
                "Check the member's setter.", onWrite: true);
        }

        // A closed object replaced here was replaced by the inspector itself, not from outside: its
        // branch starts over from the new object.
        Record();

        foreach (var node in this)
            node.Record();

        Update(ValueSource.Write);
    }

    internal override void Record()
    {
        Seen.Clear();

        if (!Tracks)
            return;

        foreach (var instance in Root.Instances)
        {
            try
            {
                Seen[instance] = Resolve(instance);
            }
            catch (Exception)
            {
                // A getter that throws leaves nothing to compare; the read reports it.
            }
        }
    }

    internal override void CheckReplaced()
    {
        if (!Tracks)
            return;

        foreach (var instance in Root.Instances)
        {
            object? current;

            try
            {
                current = Resolve(instance);
            }
            catch (Exception)
            {
                continue;
            }

            if (!Seen.TryGetValue(instance, out var seen) || ReferenceEquals(seen, current))
            {
                Seen[instance] = current;
                continue;
            }

            // A closed object is a value: replacing it is an edit, not a swap.
            if (!IsGroup)
            {
                Seen[instance] = current;
                continue;
            }

            var replaced = new ObjectReplacedEventArgs(this, instance, seen, current);
            OnObjectReplaced(replaced);

            if (!replaced.Accepted)
            {
                Replaced = true;
                return;
            }

            Seen[instance] = current;
        }
    }

    internal override object? Resolve(object? instance)
    {
        var owner = Parent!.Resolve(instance);

        if (owner == null)
            return null;

        return Member switch
        {
            FieldInfo fi => fi.GetValue(owner),
            PropertyInfo { CanRead: true } pi => pi.GetValue(owner),
            _ => null
        };
    }

    internal override void WriteTo(object? instance, object? value)
    {
        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        var owner = Parent!.Resolve(instance)
            ?? throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is null.");

        switch (Member)
        {
            case FieldInfo fi:
                fi.SetValue(owner, value);
                break;
            case PropertyInfo { CanWrite: true } pi:
                pi.SetValue(owner, value);
                break;
            default:
                throw new InvalidOperationException($"'{Name}' is read-only.");
        }

        // A struct owner is a boxed copy, so it has to be written back into its own owner.
        if (owner.GetType().IsValueType)
            Parent.WriteTo(instance, owner);
    }
}
