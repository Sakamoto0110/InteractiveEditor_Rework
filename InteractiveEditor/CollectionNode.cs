using System.Collections;
using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;

namespace InteractiveEditor;

// A member that is a collection (P5.2, P5.10). It shows its content, not the members of its type: the
// selector lists the items, and the row below it, Item, is the one chosen, with its members below it.
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
        Item = AddChild(new ItemNode(this));
    }

    // The type of the items: an array's element, the T of IEnumerable<T>, or object.
    public Type ItemType { get; }

    // The row of the item chosen, which reads and writes it in every bound object.
    public ItemNode Item { get; }

    // Whether the collection takes an item in place of another; when it does not, the row of the item
    // is read-only.
    internal bool TakesItems { get; }

    // What the selector lists: the items of the first bound object's collection at the last read. Like
    // ViewValue, it does not read the object, so it follows the binder control.
    public IReadOnlyList<object?> Items => Array.AsReadOnly(KnownItems);

    // The place chosen in the collection, which the row of the item reads in every bound object; -1 for
    // none. Choosing drops what the rows below held and reads them again; a place past the items the
    // selector lists throws. It also follows the items: a collection that gets items after having none
    // starts at the first, and a place past the end goes to the last item.
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

    // The items are read along with the node, and a change in them is a change of the node. When the
    // item chosen changes with them (another place, or another object in the same place), the rows
    // below start over from it. A struct has no identity: a new value in the chosen place is the same
    // item, as when one of its fields is written.
    private protected override bool ContentChanged(ValueSource? source)
    {
        var items = ReadItems();

        if (items.SequenceEqual(KnownItems))
            return false;

        var before = Chosen >= 0 ? KnownItems[Chosen] : null;
        var index = KnownItems.Length == 0 ? 0 : Math.Min(Chosen, items.Length - 1);
        var moved = index != Chosen;

        KnownItems = items;
        Chosen = index;

        if (moved || index >= 0 && !ItemType.IsValueType && !ReferenceEquals(before, items[index]))
            ResetItem(source == null ? null : ValueSource.Selection);

        return true;
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
