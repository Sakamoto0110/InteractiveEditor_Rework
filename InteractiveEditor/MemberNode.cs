using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Model;

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

        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        // With nothing bound, the write fails like any other null owner.
        if (Root.Instances.Count == 0)
            WriteTo(null, value);

        // A null owner in any of the objects stops the write before any of them changes.
        foreach (var instance in Root.Instances)
        {
            if (Parent!.Resolve(instance) == null)
                throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is null.");
        }

        if (!TryPrepare(value, out var prepared))
            return;

        try
        {
            foreach (var instance in Root.Instances)
                WriteTo(instance, prepared);

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

        if (HasMembers)
            Root.Owner.Rewire();

        UpdateAffected(ValueSource.Write);
    }

    // Turns what came in into what the member takes: text through the text rules and the parser, a
    // number into the member's number type, then the value rules and the range. A value of an
    // unrelated type is a mistake of the caller, and throws; anything else that fails here (text that
    // does not convert, a number too big, a rule that throws) is reported on the node, and nothing is
    // written.
    private bool TryPrepare(object? value, out object? prepared)
    {
        var culture = Inspector.Options.CultureInUse;
        prepared = null;

        if (value is not string)
            ValueConverter.CheckFits(value, ValueType, Path);

        try
        {
            if (value is string text)
            {
                foreach (var rule in TextRules)
                    text = rule.Apply(text);

                if (!ValueConverter.TryParse(text, ValueType, culture, out value))
                {
                    OnBindFailed(FailureSeverity.Recovered, new FormatException($"'{text}' is not a {ValueType.Name}."),
                        $"'{Path}' could not take the text '{text}'.",
                        $"Type a {ValueType.Name} in the format of the culture {culture.Name}.", onWrite: true);
                    return false;
                }
            }
            else
            {
                value = ValueConverter.Convert(value, ValueType, culture);
            }

            foreach (var rule in ValueRules)
                value = rule.Apply(value);

            if (Range is { } range)
                value = ValueConverter.Clamp(value, range.Min, range.Max);

            prepared = ValueConverter.Convert(value, ValueType, culture);
            return true;
        }
        catch (Exception e)
        {
            OnBindFailed(FailureSeverity.Recovered, Unwrap(e), $"'{Path}' could not take the value.",
                "Check the node's rules, and the value against the member's type.", onWrite: true);
            return false;
        }
    }

    internal override void Record()
    {
        Seen.Clear();

        foreach (var instance in Root.Instances)
            Record(instance);
    }

    internal override void Record(object instance)
    {
        if (!Tracks)
            return;

        try
        {
            Seen[instance] = Resolve(instance);
        }
        catch (Exception)
        {
            // A getter that throws leaves nothing to compare; the read reports it.
            Seen.Remove(instance);
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

            // A closed object is a value: replacing it is an edit, not a swap. An open group's object
            // is kept only when a subscriber accepts the new one.
            if (IsGroup)
            {
                var replaced = new ObjectReplacedEventArgs(this, instance, seen, current);
                OnObjectReplaced(replaced);

                if (!replaced.Accepted)
                {
                    Replaced = true;
                    return;
                }
            }

            // Either way, the branch starts over from the new object in this instance.
            Seen[instance] = current;

            foreach (var node in this)
                node.Record(instance);
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

        // Marked while the setter runs, so the owner's notification of this write, and of a struct
        // written back into it, is not taken for a change made by the object.
        Writing = true;

        try
        {
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
        }
        finally
        {
            Writing = false;
        }

        // A struct owner is a boxed copy, so it has to be written back into its own owner.
        if (owner.GetType().IsValueType)
            Parent.WriteTo(instance, owner);
    }
}
