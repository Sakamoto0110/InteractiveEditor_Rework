

namespace InteractiveEditor.Primitives;

public struct ArgbColor : IEquatable<ArgbColor>
{
    public static readonly ArgbColor Empty = new ArgbColor(0, 0, 0, 0);

    private byte a;
    private byte r;
    private byte g;
    private byte b;

    public ArgbColor(byte r, byte g, byte b)
    {
        A = 255;
        R = r;
        G = g;
        B = b;
    }

    public ArgbColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public ArgbColor(int argb)
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


    public static implicit operator HslColor(ArgbColor color) => new HslColor(color);
    public static implicit operator System.Drawing.Color(ArgbColor color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static implicit operator ArgbColor(System.Drawing.Color color) => new ArgbColor(color.A, color.R, color.G, color.B);

    public static explicit operator int(ArgbColor color) => color.ToArgb();
    public static explicit operator ArgbColor(int argb) => new ArgbColor(argb);

    public static bool operator ==(ArgbColor left, ArgbColor right) => left.A == right.A && left.R == right.R && left.G == right.G && left.B == right.B;
    public static bool operator !=(ArgbColor left, ArgbColor right) => !(left == right);

    public readonly int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
    public readonly string ToHexString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public readonly bool Equals(ArgbColor other) => this == other;
    public override readonly bool Equals(object? obj) => obj is ArgbColor other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(A, R, G, B);
    public override readonly string ToString() => $"({A},{R},{G},{B})";
}
 