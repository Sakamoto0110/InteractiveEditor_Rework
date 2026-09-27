using System.Collections;
using System.Reflection;
using InteractiveEditor.Options;

namespace InteractiveEditor;

public class InspectorNode : IEnumerable<InspectorNode>
{
    private readonly List<InspectorNode> Children = [];

    internal InspectorNode(InspectorNode? parent, MemberInfo? member)
    {
        Parent = parent;
        Member = member;
        Path = member == null ? string.Empty
            : parent?.Member == null ? member.Name
            : $"{parent.Path}.{member.Name}";
    }

    public InspectorNode? Parent { get; }
    public MemberInfo? Member { get; }
    public string Path { get; }
    public virtual string Name => Member?.Name ?? " -- ";

    public Type? ValueType => Member switch
    {
        FieldInfo fi => fi.FieldType,
        PropertyInfo pi => pi.PropertyType,
        _ => null
    };

    // Options, in layers: reflection < attributes < whatever the caller sets afterwards.
    public string Label { get; set; } = string.Empty;
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

    public bool HasMembers => Children.Count > 0;
    public bool IsGroup => HasMembers && Expandable;

    // A path relative to this node ("Moo.MooY"); chaining works too: node["Moo"]["MooY"].
    public InspectorNode this[string path]
    {
        get
        {
            var node = this;

            foreach (var name in path.Split('.'))
            {
                node = node.Children.FirstOrDefault(c => c.Name == name)
                    ?? throw new KeyNotFoundException($"'{Name}' has no field at path '{path}'.");
            }

            return node;
        }
    }

    public virtual object? GetValue()
    {
        var owner = Parent?.GetValue();

        if (owner == null)
            return null;

        return Member switch
        {
            FieldInfo fi => fi.GetValue(owner),
            PropertyInfo { CanRead: true } pi => pi.GetValue(owner),
            _ => null
        };
    }

    public virtual void SetValue(object? value)
    {
        // A group is edited through its fields; the object behind it is never replaced from here.
        if (IsGroup)
            throw new InvalidOperationException($"'{Name}' is a group; set its fields instead.");

        Write(value);
    }

    // The write itself. A struct owner comes back through here too, even when it is a group.
    internal virtual void Write(object? value)
    {
        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        var owner = Parent?.GetValue()
            ?? throw new InvalidOperationException($"Cannot set '{Name}': '{Parent?.Name}' is null.");

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
            Parent!.Write(owner);
    }

    // What a view shows: ignored nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows
    {
        get
        {
            foreach (var child in Children.Where(c => !c.Ignored).OrderBy(c => c.Order))
            {
                yield return child;

                if (child.IsGroup)
                {
                    foreach (var node in child.Rows)
                        yield return node;
                }
            }
        }
    }

    // The whole tree below this node, as discovered: ignored nodes and the insides of closed groups too.
    public IEnumerator<InspectorNode> GetEnumerator()
    {
        return Descendants().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal InspectorNode Add(MemberInfo member)
    {
        var child = new InspectorNode(this, member);
        Children.Add(child);
        return child;
    }

    internal IEnumerable<InspectorNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;

            foreach (var node in child.Descendants())
                yield return node;
        }
    }
}



 