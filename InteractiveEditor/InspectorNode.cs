using System.Collections;
using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
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

    // The inspector this node belongs to.
    public Inspector Inspector => Root.Owner;

    private protected RootNode Root { get; }

    #region Options

    // Options, in layers: reflection < attributes < whatever the caller sets afterwards.
    public string Label { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public string? Help { get; set; }
    public int Order { get; set; }
    public bool Ignored { get; set; }

    // Visible and ReadOnly are read through the parents: hiding or locking a node takes its whole
    // branch along, and a child cannot be opened while something above it stays closed.
    public bool Visible
    {
        get => field && Parent?.Visible != false;
        set;
    } = true;

    public bool ReadOnly
    {
        get => field || Parent?.ReadOnly == true;
        set;
    }

    public EditorKind Editor { get; set; }
    public NumericRange? Range { get; set; }
    public double? ScrubMultiplier { get; set; }
    public bool Expandable { get; set; }
    public bool Collapsed { get; set; }

    #endregion

    #region State

    // What the bound objects held here at the last read, one value per object. ValueChanged reports
    // a change against it.
    private object?[] Known = [];

    // Whether the failure on the node came from a write: a read that works does not clear it.
    private bool FailedOnWrite;

    // The last failure reading or writing this node, or null once it works again; the view shows it
    // on the row.
    public InspectorFailureEventArgs? Failure { get; private set; }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;
    public event EventHandler<InspectorFailureEventArgs>? BindFailed;

    #endregion

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

    // The value in the first bound object, or null when nothing is bound.
    public object? GetValue()
    {
        var values = ReadValues();
        return values.Length > 0 ? values[0] : null;
    }

    // One value per bound object, in the order they were bound.
    public IReadOnlyList<object?> GetValues() => ReadValues();

    // True when the bound objects do not all hold the same value here.
    public bool IsMixed => ReadValues().Distinct().Skip(1).Any();

    public abstract void SetValue(object? value);

    // What a view shows: ignored and hidden nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows
    {
        get
        {
            foreach (var child in Children.Where(c => !c.Ignored && c.Visible).OrderBy(c => c.Order))
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

    // Reads the node again and reports a change against the last read.
    internal void Update(ValueSource source)
    {
        var values = ReadValues();

        if (values.SequenceEqual(Known))
            return;

        Known = values;
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(this, source));
    }

    // The objects changed (bind, unbind): the node starts over from what they hold, without an event.
    internal void Reset()
    {
        Failure = null;
        FailedOnWrite = false;
        Known = ReadValues();
    }

    private protected void OnWritten()
    {
        Failure = null;
        FailedOnWrite = false;
    }

    // A weak spot failed on this node: the inspector goes on, and the row shows the failure.
    private protected void OnBindFailed(FailureSeverity severity, Exception exception, string message,
        string suggestion, bool onWrite)
    {
        var failure = new InspectorFailureEventArgs(Inspector, Path, severity, exception, message, suggestion);
        Failure = failure;
        FailedOnWrite = onWrite;
        BindFailed?.Invoke(this, failure);
    }

    // The exception a getter or a setter threw, out of the reflection's wrapper.
    private protected static Exception Unwrap(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
    }

    // One value per bound object. A getter that throws does not stop the read: the node reports it,
    // reads null there, and the next read tries again.
    private object?[] ReadValues()
    {
        var values = new object?[Root.Instances.Count];
        Exception? error = null;

        for (var i = 0; i < values.Length; i++)
        {
            try
            {
                values[i] = Resolve(Root.Instances[i]);
            }
            catch (Exception e)
            {
                error ??= Unwrap(e);
            }
        }

        if (error != null)
        {
            OnBindFailed(FailureSeverity.Recovered, error, $"'{Path}' could not be read.",
                "Check the member's getter; the next read tries again.", onWrite: false);
        }
        else if (!FailedOnWrite)
        {
            Failure = null;
        }

        return values;
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



 