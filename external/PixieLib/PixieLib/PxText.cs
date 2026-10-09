using System.Globalization;

namespace PixieLib;

// How a primitive becomes text, the same as in C++ (docs/layout.md, 3): its fields in order, in
// parentheses, separated by ", ". A number is written with a point in any culture, in the shortest
// form that reads back as the same value, and never in scientific notation: 100000 stays 100000, not
// 1E+05. NaN and the infinities come out as nan, inf and -inf, like std::to_chars.
internal static class PxText
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Tuple(params string[] fields) => "(" + string.Join(", ", fields) + ")";

    public static string Number(int value) => value.ToString(Invariant);

    public static string Number(byte value) => value.ToString(Invariant);

    public static string Number(double value)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsInfinity(value))
            return value > 0 ? "inf" : "-inf";
        if (value == 0)
            return IsNegative(value) ? "-0" : "0";

        // The fewest digits that read back as the same value. "R" does it on .NET, but on net481 it
        // can give 17 digits where 16 are enough, so both targets search the same way.
        string text = value.ToString("R", Invariant);
        for (int digits = 1; digits <= 17; digits++)
        {
            string candidate = value.ToString("G" + digits, Invariant);
            if (double.Parse(candidate, Invariant) == value)
            {
                text = candidate;
                break;
            }
        }
        return WithoutExponent(text);
    }

    public static string Number(float value)
    {
        if (float.IsNaN(value))
            return "nan";
        if (float.IsInfinity(value))
            return value > 0 ? "inf" : "-inf";
        if (value == 0)
            return IsNegative(value) ? "-0" : "0";

        string text = value.ToString("R", Invariant);
        for (int digits = 1; digits <= 9; digits++)
        {
            string candidate = value.ToString("G" + digits, Invariant);
            if (float.Parse(candidate, Invariant) == value)
            {
                text = candidate;
                break;
            }
        }
        return WithoutExponent(text);
    }

    // -0 compares equal to 0, so the sign comes from the bits; net481 also drops it when formatting.
    private static bool IsNegative(double value) => System.BitConverter.DoubleToInt64Bits(value) < 0;

    // Moves the decimal point of "1.5E+20" or "5E-324" to where the exponent says, with zeros.
    private static string WithoutExponent(string text)
    {
        int e = text.IndexOfAny(new[] { 'E', 'e' });
        if (e < 0)
            return text;

        bool negative = text[0] == '-';
        string mantissa = text.Substring(negative ? 1 : 0, e - (negative ? 1 : 0));
        int exponent = int.Parse(text.Substring(e + 1), NumberStyles.AllowLeadingSign, Invariant);
        int point = mantissa.IndexOf('.');
        string digits = point < 0 ? mantissa : mantissa.Remove(point, 1);
        int integerDigits = (point < 0 ? mantissa.Length : point) + exponent;

        string plain;
        if (integerDigits <= 0)
            plain = "0." + new string('0', -integerDigits) + digits;
        else if (integerDigits >= digits.Length)
            plain = digits + new string('0', integerDigits - digits.Length);
        else
            plain = digits.Substring(0, integerDigits) + "." + digits.Substring(integerDigits);
        return negative ? "-" + plain : plain;
    }
}
