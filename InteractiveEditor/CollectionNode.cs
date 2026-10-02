using System.Collections;
using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Model;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

// A member that is a collection (P5.2, P5.10). It shows its content, not the members of its type: the
// selector lists the items, and the row below it, Item, is the one chosen, with its members below it.
// The list editor (EditorKind.List) shows one row per item, and adds, removes and moves them.
public sealed class CollectionNode : MemberNode
{
    // The place chosen in the collection, -1 for none.
    private int Chosen = -1;

    // What the first bound object's collection held at the last read, which the selector lists.
    private object?[] KnownItems = [];

    internal CollectionNode(InspectorNode parent, MemberInfo member) : base(parent, member)
    {
        var type = Nullable.GetUnderlyingType(ValueType) ?? ValueType;
        ItemType = ItemTypeOf(type);
        TakesItems = TakesItemsOf(type);
        Replaceable = ReflectionPolicy.IsPubliclyWritable(member);
        Item = AddChild(new ItemNode(this));
    }

    // The type of the items: an array's element, the T of IEnumerable<T>, or object.
    public Type ItemType { get; }

    // The row of the item chosen, which reads and writes it in every bound object.
    public ItemNode Item { get; }

    // Whether the collection takes an item in place of another; when it does not, the row of the item
    // is read-only.
    internal bool TakesItems { get; }

    // Whether the collection itself can be replaced: its member has a public setter. Without one, a
    // collection held by reference keeps its items editable (P4.7), and only replacing it is refused.
    private bool Replaceable { get; }

    private protected override string? Locked =>
        base.Locked ?? (Replaceable ? null : $"'{Name}' cannot be replaced: it has no public setter.");

    // What the selector lists: the items of the first bound object's collection at the last read. Like
    // ViewValue, it does not read the object, so it follows the binder control.
    public IReadOnlyList<object?> Items => Array.AsReadOnly(KnownItems);

    // The place chosen in the collection, which the row of the item reads in every bound object; -1 for
    // none. Choosing drops what the rows below held and reads them again; a place past the items the
    // selector lists throws. It also follows the items (ContentChanged).
    public int SelectedIndex
    {
        get => Chosen;
        set
        {
            ThrowIfCompromised();

            if (value < -1 || value >= KnownItems.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"'{Name}' lists {KnownItems.Length} items; -1 chooses none.");
            }

            if (value == Chosen)
                return;

            Chosen = value;
            ResetItem(ValueSource.Selection);
            OnValueChanged(ValueSource.Selection);
            Root.Owner.Rewire();
        }
    }

    // Adds a new item at the end of the collection in every bound object, and chooses it: an object
    // made with the item type's constructor without parameters, or the type's empty value (zero,
    // false, an empty text, null). Like the other operations of the list editor, it writes at once,
    // whatever the binder control, as a button does.
    public void AddItem()
    {
        var lists = Editable(resizes: true);
        var worked = Change(lists, list => list.Add(NewItem()));
        Changed(worked, lists[0].Count - 1, another: true);
    }

    // Removes the item in this place of the collection in every bound object. The choice stays with
    // the item chosen; when that is the one removed, the item that takes its place is chosen.
    public void RemoveItem(int index)
    {
        var lists = Editable(resizes: true, (index, nameof(index)));
        var worked = Change(lists, list => list.RemoveAt(index));
        Changed(worked, index < Chosen ? Chosen - 1 : Chosen, another: index == Chosen);
    }

    // Moves the item in one place of the collection to another, in every bound object, the items
    // between them making room; an array too, as its size does not change. The choice goes with the
    // item chosen.
    public void MoveItem(int from, int to)
    {
        var lists = Editable(resizes: false, (from, nameof(from)), (to, nameof(to)));

        if (from == to)
            return;

        var follow = Chosen == from ? to
            : from < Chosen && Chosen <= to ? Chosen - 1
            : to <= Chosen && Chosen < from ? Chosen + 1
            : Chosen;

        var worked = Change(lists, list =>
        {
            var item = list[from];
            var step = from < to ? 1 : -1;

            for (var place = from; place != to; place += step)
                list[place] = list[place + step];

            list[to] = item;
        });

        Changed(worked, follow, another: false);
    }

    // The item in the chosen place of a bound object's collection, or null when there is none there.
    internal object? ItemIn(object? instance)
    {
        if (Chosen < 0 || Resolve(instance) is not { } collection)
            return null;

        if (AsList(collection) is { } list)
            return Chosen < list.Count ? list[Chosen] : null;

        return ((IEnumerable)collection).Cast<object?>().ElementAtOrDefault(Chosen);
    }

    // Why the chosen place of a bound object's collection cannot take an item now, or null when it can.
    internal string? CannotPut(object? instance)
    {
        if (Resolve(instance) is not { } collection)
            return $"'{Name}' is null.";

        if (Chosen < 0)
            return $"no item of '{Name}' is chosen.";

        if (AsList(collection) is not { IsReadOnly: false } list)
            return $"'{Name}' cannot take an item in place of another.";

        return Chosen < list.Count ? null : $"'{Name}' has no item {Chosen}.";
    }

    // Puts an item in the chosen place of a bound object's collection. The indexer is the item's
    // setter: what it throws comes out as a setter's does, and the write reports it.
    internal void Put(object? instance, object? item)
    {
        if (CannotPut(instance) is { } reason)
            throw new InvalidOperationException($"Cannot set '{Item.Name}': {reason}");

        var collection = Resolve(instance)!;

        // Marked while the indexer runs, so a collection that reports its changes does not come back
        // with this write as a change from outside.
        Writing = true;

        try
        {
            AsList(collection)![Chosen] = item;
        }
        catch (Exception e)
        {
            throw new TargetInvocationException(e);
        }
        finally
        {
            Writing = false;
        }

        // A collection that is a struct is a copy, so it goes back into its own owner.
        if (collection.GetType().IsValueType)
            WriteTo(instance, collection);
    }

    // The items are read along with the node, and a change in them is a change of the node. The choice
    // follows the item chosen: an object that went to another place takes the choice with it, and a
    // struct, a null or an object that left keeps only the place, which goes to the last item when it
    // is past the end. A collection that gets items after having none starts at the first. When the
    // item chosen is another one, the rows below start over from it; a struct has no identity, so a new
    // value in its place is the same item, as when one of its fields is written.
    private protected override bool ContentChanged(ValueSource? source)
    {
        var items = ReadItems();

        if (items.SequenceEqual(KnownItems))
            return false;

        var before = Chosen >= 0 ? KnownItems[Chosen] : null;
        var index = KnownItems.Length == 0 ? 0 : Math.Min(Chosen, items.Length - 1);

        if (Chosen >= 0 && before != null && !ItemType.IsValueType && !(index >= 0 && ReferenceEquals(items[index], before)))
        {
            var found = Array.FindIndex(items, item => ReferenceEquals(item, before));

            if (found >= 0)
                index = found;
        }

        var another = Chosen < 0 || index < 0
            ? index != Chosen
            : ItemType.IsValueType ? index != Chosen : !ReferenceEquals(before, items[index]);

        KnownItems = items;
        Chosen = index;

        if (another)
            ResetItem(source == null ? null : ValueSource.Selection);

        return true;
    }

    // The lists an operation of the list editor changes: the bound objects' collections, each one once
    // (two objects can hold the same one). What cannot take the operation throws before any of them
    // changes: a read-only node, a disabled branch, a collection that is null or cannot change its
    // items (nor grow and shrink, when the operation resizes), and a place past the items listed or
    // past the items of one of the objects.
    private List<IList> Editable(bool resizes, params (int Place, string Name)[] places)
    {
        ThrowIfCompromised();

        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        foreach (var (place, name) in places)
        {
            if (place < 0 || place >= KnownItems.Length)
                throw new ArgumentOutOfRangeException(name, place, $"'{Name}' lists {KnownItems.Length} items.");
        }

        if (Root.Instances.Count == 0)
            throw new InvalidOperationException($"'{Name}' has no object bound.");

        var lists = new List<IList>();

        foreach (var instance in Root.Instances)
        {
            if (Resolve(instance) is not { } collection)
                throw new InvalidOperationException($"'{Name}' is null.");

            if (!TakesItems || AsList(collection) is not { IsReadOnly: false } list || resizes && list.IsFixedSize)
                throw new InvalidOperationException($"'{Name}' cannot {(resizes ? "take or lose items" : "change its items")}.");

            if (places.FirstOrDefault(place => place.Place >= list.Count) is { Name: not null } missing)
                throw new InvalidOperationException($"'{Name}' has no item {missing.Place} in one of the objects.");

            if (!lists.Any(known => ReferenceEquals(known, list)))
                lists.Add(list);
        }

        return lists;
    }

    // Runs an operation of the list editor on every list, marked as the inspector's own change. What a
    // list throws is reported on the node, and the lists keep what it did; false then.
    private bool Change(List<IList> lists, Action<IList> operation)
    {
        Writing = true;

        try
        {
            foreach (var list in lists)
                operation(list);

            OnWritten();
            return true;
        }
        catch (Exception e)
        {
            OnBindFailed(FailureSeverity.WorkedAround, Unwrap(e), $"The items of '{Path}' could not be changed.",
                "Check the collection; the objects keep what it did.", onWrite: true);
            return false;
        }
        finally
        {
            Writing = false;
        }
    }

    // The items are read again after an operation of the list editor. When it worked, the choice goes
    // where the operation says (the item chosen, where it went; or a new one), and the rows below start
    // over when it is another item; when it failed, the choice follows the items as after any change.
    private void Changed(bool worked, int choose, bool another)
    {
        if (worked)
        {
            KnownItems = ReadItems();
            Chosen = Math.Min(choose, KnownItems.Length - 1);

            if (another)
                ResetItem(ValueSource.Selection);
        }

        UpdateAffected(ValueSource.Write, changed: worked);
        Root.CheckRules(ValueSource.Write);
        Root.Owner.Rewire();
    }

    // A new item: an object made with the item type's constructor without parameters, or the type's
    // empty value.
    private object? NewItem()
    {
        if (!ReflectionDiscovery.IsTerminal(ItemType) && ItemType is { IsValueType: false, IsAbstract: false }
            && ItemType.GetConstructor(Type.EmptyTypes) != null)
        {
            return Activator.CreateInstance(ItemType);
        }

        return ValueConverter.Empty(ItemType);
    }

    // The items of the first bound object's collection, read now. A getter that throws is reported by
    // the read of the node; an enumeration that throws is reported here, and the selector lists none.
    private object?[] ReadItems()
    {
        if (Root.Instances.Count == 0)
            return [];

        object? collection;

        try
        {
            collection = Resolve(Root.Instances[0]);
        }
        catch (Exception)
        {
            return [];
        }

        try
        {
            return collection is IEnumerable items ? items.Cast<object?>().ToArray() : [];
        }
        catch (Exception e)
        {
            OnBindFailed(FailureSeverity.Recovered, Unwrap(e), $"The items of '{Path}' could not be read.",
                "Check the collection; the next read tries again.", onWrite: false);
            return [];
        }
    }

    // The row of the item and everything below it start over from the item chosen now: what they held
    // goes, and the objects below it are the new item's.
    private void ResetItem(ValueSource? source)
    {
        foreach (var node in this)
            node.Reset(source);
    }

    // The collection as a list, to reach a place by its index: an IList, or an array of one dimension.
    private static IList? AsList(object collection)
    {
        return collection is IList list and not Array { Rank: > 1 } ? list : null;
    }

    // An array's element, the T of the one IEnumerable<T> the type is, or object.
    private static Type ItemTypeOf(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        var sequences = (type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .ToList();

        return sequences.Count == 1 ? sequences[0].GetGenericArguments()[0] : typeof(object);
    }

    // An array or a list takes an item in place of another; a sequence that is only read
    // (IEnumerable<T>, IReadOnlyList<T>) does not.
    private static bool TakesItemsOf(Type type)
    {
        if (type.IsArray)
            return type.GetArrayRank() == 1;

        return typeof(IList).IsAssignableFrom(type)
            || type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>);
    }
}
