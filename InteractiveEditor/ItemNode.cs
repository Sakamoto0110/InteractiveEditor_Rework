namespace InteractiveEditor;

// The row of the item chosen in a collection (P5.10): the value in the chosen place of every bound
// object's collection, read and written as a member is, with the members of the item's type below it.
// There is no member behind it; the collection's selector says which item it is.
public sealed class ItemNode : MemberNode
{
    internal ItemNode(CollectionNode collection) : base(collection, "Item", byName: false)
    {
        Collection = collection;
    }

    // The collection whose chosen item this is.
    public CollectionNode Collection { get; }

    public override Type ValueType => Collection.ItemType;

    // A collection that takes no item in place of another (IEnumerable<T>, IReadOnlyList<T>) leaves its
    // item read-only, and with it what a struct item has below it, which goes back the same way.
    private protected override bool FixedReadOnly => !Collection.TakesItems;

    // Another item in the chosen place is not a swap: the row shows whatever is there.
    private protected override bool KeepsObject => false;

    private protected override string? Unwritable(object instance) => Collection.CannotPut(instance);

    internal override object? Resolve(object? instance) => Collection.ItemIn(instance);

    internal override void WriteTo(object? instance, object? value)
    {
        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        Collection.Put(instance, value);
    }
}
