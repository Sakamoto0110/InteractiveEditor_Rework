

namespace InteractiveEditor.Primitives;

public struct Color : IEquatable<Color>
{
    public static readonly Color Empty = new Color(0, 0, 0, 0);

    private byte a;
    private byte r;
    private byte g;
    private byte b;

    public Color(byte r, byte g, byte b)
    {
        A = 255;
        R = r;
        G = g;
        B = b;
    }

    public Color(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public Color(int argb)
    {
        A = (byte)((argb >> 24) & 0xFF);
        R = (byte)((argb >> 16) & 0xFF);
        G = (byte)((argb >> 8) & 0xFF);
        B = (byte)(argb & 0xFF);
    }

    public byte A
    {
        readonly get => a;
        set => a = value;
    }

    public byte R
    {
        readonly get => r;
        set => r = value;
    }

    public byte G
    {
        readonly get => g;
        set => g = value;
    }

    public byte B
    {
        readonly get => b;
        set => b = value;
    }

    public readonly bool IsEmpty => A == 0 && R == 0 && G == 0 && B == 0;


    public static implicit operator ColorHSL(Color color) => new ColorHSL(color);
    public static implicit operator System.Drawing.Color(Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static implicit operator Color(System.Drawing.Color color) => new Color(color.A, color.R, color.G, color.B);

    public static implicit operator System.Windows.Media.Color(Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static implicit operator Color(System.Windows.Media.Color color) => new Color(color.A, color.R, color.G, color.B);

    public static explicit operator int(Color color) => color.ToArgb();
    public static explicit operator Color(int argb) => new Color(argb);

    public static bool operator ==(Color left, Color right) => left.A == right.A && left.R == right.R && left.G == right.G && left.B == right.B;
    public static bool operator !=(Color left, Color right) => !(left == right);

    public readonly int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
    public readonly string ToHexString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public readonly bool Equals(Color other) => this == other;
    public override readonly bool Equals(object? obj) => obj is Color other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(A, R, G, B);
    public override readonly string ToString() => $"({A},{R},{G},{B})";
}
 