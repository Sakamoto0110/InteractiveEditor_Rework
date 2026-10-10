using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace InteractiveEditor.Model;

// What the reflection says about a type, read once and kept for every inspector of it (P5.6): its
// public fields and properties, in the order they were declared, and the attributes on the type and on
// each of them. The nodes stay each inspector's own, since they keep its options.
// A read that fails is kept too, and each Create reports it again, so every inspector's report says
// what it would say with no cache. Only a type whose members cannot be listed is not kept: the
// discovery leaves it out, or fails on it, and the next Create tries it again.
// The models hang on their types weakly, so a type of an assembly that can be unloaded is not held.
internal sealed class TypeModel
{
    private static readonly ConditionalWeakTable<Type, TypeModel> Models = new();

    private readonly Type Type;

    // Each attribute read on the type or on one of its members, by the element and the attribute type.
    private readonly ConcurrentDictionary<(MemberInfo, Type), Reading> Readings = new();

    private Listing? Listed;

    private TypeModel(Type type)
    {
        Type = type;
    }

    // The public fields and properties, in the order they were declared (P5.1); in the reflection's
    // own order when that order could not be read, and then OrderFailure says why.
    public IReadOnlyList<MemberInfo> Members => List().Members;

    public Exception? OrderFailure => List().OrderFailure;

    public static TypeModel Of(Type type)
    {
        return Models.GetValue(type, static t => new TypeModel(t));
    }

    // The attribute on a type or on one of its members, read as GetCustomAttribute reads it (inherited
    // ones count), and kept in the model of the type; a member is looked up in the model of the type
    // it was listed from.
    public static Reading Read<TAttribute>(MemberInfo element)
        where TAttribute : Attribute
    {
        var model = Of(element as Type ?? element.ReflectedType!);
        return model.Readings.GetOrAdd((element, typeof(TAttribute)), static key => ReadNow(key.Item1, key.Item2));
    }

    private static Reading ReadNow(MemberInfo element, Type attributeType)
    {
        try
        {
            return new Reading(element.GetCustomAttribute(attributeType), null);
        }
        catch (Exception e)
        {
            return new Reading(null, e);
        }
    }

    // Two Creates of a type not seen yet may both list it at once; the listings are the same, and the
    // last one stays.
    private Listing List()
    {
        return Listed ??= ListNow();
    }

    private Listing ListNow()
    {
        var members = Type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(member => member is FieldInfo or PropertyInfo)
            .ToList();

        try
        {
            return new Listing(DeclarationOrder.Sort(members), null);
        }
        catch (Exception e)
        {
            return new Listing(members, e);
        }
    }

    private sealed record Listing(IReadOnlyList<MemberInfo> Members, Exception? OrderFailure);

    // An attribute as it was read: the one found, or null when there is none or it could not be read,
    // and then Failure says why.
    internal readonly record struct Reading(Attribute? Attribute, Exception? Failure)
    {
        // Nothing found and nothing failed: whatever is read after it decides.
        public bool IsEmpty => Attribute == null && Failure == null;
    }
}
