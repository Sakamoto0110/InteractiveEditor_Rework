using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Model;
using InteractiveEditor.Options;
using InteractiveEditor.Options.Policies;

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
        get => field || FixedReadOnly || Parent?.ReadOnly == true;
        set;
    }

    // In an inspector with no type, the bind can choose the editor while nobody else did (P1.14);
    // setting it takes that over.
    public EditorKind Editor
    {
        get;
        set
        {
            field = value;
            EditorFromBind = false;
        }
    }
    public NumericRange? Range { get; set; }
    public double? ScrubMultiplier { get; set; }
    public bool Expandable { get; set; }
    public bool Collapsed { get; set; }

    // Applied in this order to what is written: the text rules before the text is converted, the
    // value rules after it.
    public List<TextRule> TextRules { get; } = [];
    public List<ValueRule> ValueRules { get; } = [];

    #endregion

    #region State

    // Read-only whatever the options say; only a member of an inspector with no type is (P1.14).
    private protected virtual bool FixedReadOnly => false;

    // Whether the editor is the one a bind chose, and so another bind can choose again.
    private protected bool EditorFromBind { get; set; }

    // What the view holds for the bound objects, one value per object: what each held here at the last
    // read the binder control let through, or an empty value after a ForceClear(). ValueChanged reports
    // a change against it.
    private object?[] Known = [];

    // A value written through the inspector while ViewToInstance is off, held until Apply() writes it
    // or Reload() drops it.
    private protected object? Pending { get; private set; }

    public bool HasPendingValue { get; private set; }

    // What the view shows: the pending value, or what the first bound object held at the last read.
    // Unlike GetValue(), it does not read the object, so it follows the binder control.
    public object? ViewValue => ViewValueAt(0);

    // Whether the failure on the node came from a write: a read that works does not clear it.
    private bool FailedOnWrite;

    // True when the last read of the node failed, so the view holds nothing it knows.
    private protected bool ReadFailed => Failure != null && !FailedOnWrite;

    // Set when this group's object was replaced outside the inspector and nobody accepted it.
    private protected bool Replaced;

    // Set while the inspector writes this node, so the object's own notification of that write does
    // not come back as a change from the object.
    internal bool Writing { get; private protected set; }

    // The last failure reading or writing this node, or null once it works again; the view shows it
    // on the row.
    public InspectorFailureEventArgs? Failure { get; private set; }

    // True when this node, or a group above it, had its object replaced outside the inspector and
    // nobody accepted the new one: the branch is disabled until a Rebind.
    public bool IsCompromised => Replaced || Parent?.IsCompromised == true;

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;
    public event EventHandler<InspectorFailureEventArgs>? BindFailed;
    public event EventHandler<ObjectReplacedEventArgs>? ObjectReplaced;

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
                node = node.Child(name)
                    ?? throw new KeyNotFoundException($"'{Name}' has no field at path '{path}'.");
            }

            return node;
        }
    }

    // The node at the end of a member chain, as in node.Node<Moo>(m => m.Doo.DooX): the same as the
    // path "Doo.DooX", but checked by the compiler and kept up by a rename. T is the type this node
    // holds, or one that type derives from.
    public InspectorNode Node<T>(Expression<Func<T, object?>> selector)
    {
        if (ValueType is not { } type || !typeof(T).IsAssignableFrom(type))
        {
            throw new ArgumentException(
                $"The selector starts from {typeof(T).Name}, but '{Name}' holds {ValueType?.Name ?? "nothing"}.", nameof(selector));
        }

        var chain = MemberPath.Of(selector);
        var node = this;

        foreach (var member in chain)
        {
            node = node.ChildFor(member) ?? throw new KeyNotFoundException(
                $"'{Name}' has no field at path '{string.Join('.', chain.Select(m => m.Name))}'.");
        }

        return node;
    }

    // The value in the first bound object, or null when nothing is bound. A disabled branch throws.
    public object? GetValue()
    {
        ThrowIfCompromised();

        var values = ReadValues();
        return values.Length > 0 ? values[0] : null;
    }

    // One value per bound object, in the order they were bound. A disabled branch throws.
    public IReadOnlyList<object?> GetValues()
    {
        ThrowIfCompromised();
        return ReadValues();
    }

    // True when the bound objects did not all hold the same value here at the last read, what the row
    // shows as mixed. Never with a pending value, which goes to all of them, nor on a disabled branch.
    // Like ViewValue, it does not read the objects.
    public bool IsMixed => !IsCompromised && !HasPendingValue && Known.Distinct().Skip(1).Any();

    public abstract void SetValue(object? value);

    // A member of the type this node holds, added by hand (P1.12). The name is checked at once, and the
    // node gets what the reflection and the attributes say about it, as in the Create, with the manual
    // layer after them; it shows even where they would hide it, since it was added on purpose. It
    // comes without the members below it, which are added the same way.
    // In an inspector with no type, the name waits for the bind (P1.14); once a bind fixed the type, it
    // is checked at once, and the node takes the member with no layers of reflection and attributes.
    public MemberNode Add(string name)
    {
        if (this is not (RootNode or MemberNode))
            throw new InvalidOperationException($"'{Name}' holds nothing to find '{name}' in.");

        if (!Root.Typed && (Root.Instances.Count == 0 || this is MemberNode { Member: null }))
            return Adopt(new MemberNode(this, name));

        var type = ValueType ?? throw new InvalidOperationException($"'{Name}' holds nothing to find '{name}' in.");
        var member = ReflectionDiscovery.MemberNamed(type, name, Name);

        if (!Root.Typed)
        {
            var found = new MemberNode(this, name);
            found.Take(member);
            return Adopt(found);
        }

        var node = new MemberNode(this, member);
        ReflectionPolicy.Apply(node, Inspector);
        AttributePolicy.Apply(node, Inspector);
        node.Ignored = false;

        return Adopt(node);
    }

    // A button added by hand, with the action to run when it is pressed (P1.6).
    public ButtonNode AddButton(string name, string text, Action press)
    {
        return Adopt(new ButtonNode(this, name, text, press));
    }

    // A value added by hand, shown read-only and read through the getter (P1.6).
    public DisplayNode AddDisplay(string name, Func<object?> read)
    {
        return Adopt(new DisplayNode(this, name, read));
    }

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

    // Reads the node again and reports a change against the last read, or one the caller already knows
    // of (a pending value dropped); a disabled branch is not read.
    internal void Update(ValueSource source, bool changed = false)
    {
        if (!IsCompromised)
        {
            var values = ReadValues();

            if (!values.SequenceEqual(Known))
            {
                Known = values;
                changed = true;
            }
        }

        if (changed)
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(this, source));
    }

    // Reads the node again with whatever changed along with it: what hangs below it (a closed object
    // replaced, a struct written as a whole) and, since a struct changes as a whole when one of its
    // fields does, the topmost struct above it with that struct's branch.
    internal void UpdateAffected(ValueSource source, bool changed = false)
    {
        Update(source, changed);

        var top = this;

        while (top.Parent is MemberNode { ValueType.IsValueType: true } owner)
            top = owner;

        if (top != this)
            top.Update(source);

        foreach (var node in top)
        {
            if (node != this)
                node.Update(source);
        }
    }

    // The node starts over from what the objects hold: nothing pending, no failure, and a disabled
    // branch enabled again. A change in the bind does it without an event; ForceReload() reports what
    // the view has to show again.
    internal void Reset(ValueSource? source = null)
    {
        var dropped = DropPending();
        Failure = null;
        FailedOnWrite = false;
        Replaced = false;
        Record();

        var values = ReadValues();
        var changed = dropped || !values.SequenceEqual(Known);
        Known = values;

        if (changed && source is { } reported)
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(this, reported));
    }

    // Drops the pending value and reads the node again: the view goes back to what the objects hold.
    internal void Reload()
    {
        Update(ValueSource.Reload, DropPending());
    }

    // Empties what the view holds (ForceClear): nothing pending, and zero, false, an empty text or null
    // for every object. The objects keep their values.
    internal void Clear()
    {
        var dropped = DropPending();
        var empty = ValueType == null ? null : ValueConverter.Empty(ValueType);
        var values = Enumerable.Repeat(empty, Root.Instances.Count).ToArray();
        var changed = dropped || !values.SequenceEqual(Known);
        Known = values;

        if (changed)
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(this, ValueSource.Force));
    }

    // Writes the pending value (Apply); only a member has one.
    internal virtual void ApplyPending()
    {
    }

    // Writes what the view holds into the objects, as taken before anything was written (ForceApply);
    // true when a pending value went with it. Only a member writes.
    internal virtual bool ForceWrite(object?[] held)
    {
        return false;
    }

    // What the view holds for each bound object: the pending value, or what each one held at the last
    // read.
    internal object?[] HeldValues()
    {
        return Enumerable.Range(0, Root.Instances.Count).Select(ViewValueAt).ToArray();
    }

    // Keeps a value written while ViewToInstance is off, until Apply() writes it or Reload() drops it.
    // A value that goes through clears the failure of an earlier attempt.
    private protected void Hold(object? value)
    {
        OnWritten();

        if (HasPendingValue && Equals(Pending, value))
            return;

        Pending = value;
        HasPendingValue = true;
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(this, ValueSource.Pending));
    }

    // True when there was a pending value to drop.
    private protected bool DropPending()
    {
        if (!HasPendingValue)
            return false;

        Pending = null;
        HasPendingValue = false;
        return true;
    }

    private object? ViewValueAt(int index)
    {
        return HasPendingValue ? Pending : index < Known.Length ? Known[index] : null;
    }

    // Groups remember the object they hold in every bound object, or in one of them; the others have
    // nothing to keep.
    internal virtual void Record()
    {
    }

    internal virtual void Record(object instance)
    {
    }

    // Groups compare the object they hold with the one they saw; the others have nothing to check.
    internal virtual void CheckReplaced()
    {
    }

    private protected void OnObjectReplaced(ObjectReplacedEventArgs e)
    {
        ObjectReplaced?.Invoke(this, e);
    }

    // The public reads and writes first look for a replaced group on the way down to this node.
    private protected void ThrowIfCompromised()
    {
        DetectReplacements();

        if (!IsCompromised)
            return;

        var replaced = this;

        while (!replaced.Replaced)
            replaced = replaced.Parent!;

        throw new InvalidOperationException(
            $"'{Path}' cannot be used: the object of '{replaced.Path}' was replaced outside the inspector. Call Rebind() to restore it.");
    }

    // From the top down, so a replaced parent is caught before its children are compared with
    // objects that are no longer theirs.
    private protected void DetectReplacements()
    {
        var chain = new Stack<InspectorNode>();

        for (var node = this; node != null; node = node.Parent)
            chain.Push(node);

        foreach (var node in chain)
        {
            if (node.Replaced)
                return;

            node.CheckReplaced();

            if (node.Replaced)
                return;
        }
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

    // The children that are members of this node's object: a group of a type stands for its members.
    internal IEnumerable<InspectorNode> OwnMembers =>
        Children.SelectMany(child => child is TypeGroupNode group ? group.Children : (IEnumerable<InspectorNode>)[child]);

    // A child by name. A member hidden with new sits in the group of the type that declares it (P5.9),
    // and the name alone finds the one of the most derived type, as in C#: the groups of the base
    // types come first.
    private InspectorNode? Child(string name)
    {
        return Children.FirstOrDefault(c => c.Name == name)
            ?? Children.OfType<TypeGroupNode>()
                .Select(group => group.Children.FirstOrDefault(c => c.Name == name))
                .LastOrDefault(c => c != null);
    }

    // The child for a member of a selector: in the group of its declaring type when it is hidden,
    // since the member tells which of the two it is; by name otherwise.
    private InspectorNode? ChildFor(MemberInfo member)
    {
        return Children.OfType<TypeGroupNode>()
                .FirstOrDefault(group => group.DeclaringType == member.DeclaringType)?.Children
                .FirstOrDefault(c => c.Name == member.Name)
            ?? Child(member.Name);
    }

    // A node added by hand goes after the ones already here, and its name has to be free: the indexer
    // finds children by name. It reads the bound objects at once, and a member it goes into opens,
    // since a child was added to it on purpose.
    private protected T Adopt<T>(T child) where T : InspectorNode
    {
        if (Child(child.Name) != null)
            throw new ArgumentException($"'{Name}' already has a node named '{child.Name}'.", "name");

        AddChild(child);

        if (this is MemberNode)
            Expandable = true;

        if (Root.Instances.Count > 0)
        {
            child.Reset();
            Root.Owner.Rewire();
        }

        return child;
    }

    internal T AddChild<T>(T child) where T : InspectorNode
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



 