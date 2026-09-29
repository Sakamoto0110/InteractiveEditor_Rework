

namespace InteractiveEditor.Primitives;

// A color in hue (0 to 360), saturation and lightness (0 to 1), in double, with the alpha in a byte.
// There is no conversion to or from PxColorArgb, only the static functions (P8.4); the math of both
// ways lives here.
public struct PxColorHsl : IEquatable<PxColorHsl>
{
    private byte a;
    private double h;
    private double s;
    private double l;

    public PxColorHsl(double h, double s, double l)
    {
        A = 255;
        H = h;
        S = s;
        L = l;
    }

    public PxColorHsl(byte a, double h, double s, double l)
    {
        A = a;
        H = h;
        S = s;
        L = l;
    }

    public byte A
    {
        readonly get => a;
        set => a = value;
    }

    public double H
    {
        readonly get => h;
        set => h = value;
    }

    public double S
    {
        readonly get => s;
        set => s = value;
    }

    public double L
    {
        readonly get => l;
        set => l = value;
    }

    public readonly bool IsEmpty => A == 0 && H == 0 && S == 0 && L == 0;

    public static PxColorHsl FromArgb(PxColorArgb color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        double l = (max + min) / 2;

        if (delta == 0)
            return new PxColorHsl(color.A, 0, 0, l);

        double s = delta / (1 - Math.Abs(2 * l - 1));
        double h;

        if (max == r)
            h = 60 * (((g - b) / delta) % 6);
        else if (max == g)
            h = 60 * (((b - r) / delta) + 2);
        else
            h = 60 * (((r - g) / delta) + 4);

        if (h < 0)
            h += 360;

        return new PxColorHsl(color.A, h, s, l);
    }

    public static PxColorArgb ToArgb(PxColorHsl color)
    {
        double h = color.H % 360;

        if (h < 0)
            h += 360;

        double c = (1 - Math.Abs(2 * color.L - 1)) * color.S;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = color.L - c / 2;

        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return new PxColorArgb(
            color.A,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    public static bool operator ==(PxColorHsl left, PxColorHsl right) => left.A == right.A && left.H == right.H && left.S == right.S && left.L == right.L;
    public static bool operator !=(PxColorHsl left, PxColorHsl right) => !(left == right);

    public readonly bool Equals(PxColorHsl other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxColorHsl other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(A, H, S, L);
    public override readonly string ToString() => FormattableString.Invariant($"({A},{H},{S},{L})");
}
 