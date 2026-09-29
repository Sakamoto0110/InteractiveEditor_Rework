using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace InteractiveEditor.Model;

// Turns what a view or a caller hands in into what a member takes.
internal static class ValueConverter
{
    private static readonly HashSet<Type> NumberTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
    ];

    private static readonly MethodInfo ParseParsable =
        typeof(ValueConverter).GetMethod(nameof(TryParseParsable), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static bool IsNumber(Type type) => NumberTypes.Contains(type);

    // Text to the member's type: IParsable<T> when the type implements it, TypeConverter for the rest.
    // An empty text is null for a nullable member.
    public static bool TryParse(string text, Type memberType, CultureInfo culture, out object? value)
    {
        value = null;
        var type = Nullable.GetUnderlyingType(memberType);

        if (type != null && text.Length == 0)
            return true;

        type ??= memberType;

        if (type == typeof(string))
        {
            value = text;
            return true;
        }

        if (IsParsable(type))
        {
            var arguments = new object?[] { text, culture, null };
            var parsed = (bool)ParseParsable.MakeGenericMethod(type).Invoke(null, arguments)!;
            value = arguments[2];
            return parsed;
        }

        var converter = TypeDescriptor.GetConverter(type);

        if (!converter.CanConvertFrom(typeof(string)))
            return false;

        try
        {
            value = converter.ConvertFromString(null, culture, text);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // A value that is not text fits when it is of the member's type, when both are numbers, or when
    // a number goes into an enum. Anything else is a mistake of the caller, so it throws.
    public static void CheckFits(object? value, Type memberType, string path)
    {
        var type = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (value == null)
        {
            if (type.IsValueType && type == memberType)
                throw new ArgumentException($"'{path}' takes {type.Name}, not null.");

            return;
        }

        if (type.IsInstanceOfType(value) || IsNumber(value.GetType()) && (IsNumber(type) || type.IsEnum))
            return;

        throw new ArgumentException($"'{path}' takes {type.Name}, not {value.GetType().Name}.");
    }

    // Converts a value that fits into the member's type; a number too big for it throws
    // (OverflowException).
    public static object? Convert(object? value, Type memberType, CultureInfo culture)
    {
        var type = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (value == null || type.IsInstanceOfType(value))
            return value;

        if (type.IsEnum)
            return Enum.ToObject(type, value);

        return System.Convert.ChangeType(value, type, culture);
    }

    // A number outside [min, max] comes back to its edge, in its own type; anything else passes.
    public static object? Clamp(object? value, double min, double max)
    {
        if (value == null || !IsNumber(value.GetType()))
            return value;

        var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (number >= min && number <= max)
            return value;

        return System.Convert.ChangeType(Math.Clamp(number, min, max), value.GetType(), CultureInfo.InvariantCulture);
    }

    private static bool IsParsable(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IParsable<>)
            && i.GenericTypeArguments[0] == type);
    }

    private static bool TryParseParsable<T>(string text, IFormatProvider provider, out object? value)
        where T : IParsable<T>
    {
        var parsed = T.TryParse(text, provider, out var result);
        value = result;
        return parsed;
    }
}
