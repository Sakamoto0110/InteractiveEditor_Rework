using System.Collections;
using InteractiveEditor.Options;

namespace InteractiveEditor;

// A node of the tree. The getter is the same for every kind of node, and each kind decides what
// writing means: a member writes, the root refuses.
public abstract class InspectorNode : IEnumerable<InspectorNode>, IDisposable
{
    private readonly List<InspectorNode> Children = [];

    private protected InspectorNode(InspectorNode? parent, string name)
    {
        Parent = parent;
        Name = name;
        Root = parent?.Root ?? (RootNode)this;
        Path = parent == null ? string.Empty
            : parent.Parent == null ? name
            : $"{parent.Path}.{name}";
    }

    public InspectorNode? Parent { get; }
    public string Name { get; }
    public string Path { get; }
    public virtual Type? ValueType => null;

    private protected RootNode Root { get; }

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

    public object? GetValue() => Resolve(Root.Instance);

    public abstract void SetValue(object? value);

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

    // Nodes will hold resources later (images, files); disposing a node disposes its branch.
    public virtual void Dispose()
    {
        foreach (var child in Children)
            child.Dispose();
    }

    // This node's value inside a bound object; null when the object, or anything above the node, is null.
    internal abstract object? Resolve(object? instance);

    // Writes into a bound object. A struct owner comes back up through here, even when it is a group.
    internal abstract void WriteTo(object? instance, object? value);

    internal T Add<T>(T child) where T : InspectorNode
    {
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



 