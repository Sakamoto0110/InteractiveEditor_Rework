

namespace InteractiveEditor.Primitives;

// A color in ARGB bytes. To and from System.Drawing.Color nothing is lost, so both ways are
// implicit; to and from PxColorHsl there is no conversion, only the static functions (P8.4).
public struct PxColorArgb : IEquatable<PxColorArgb>
{
    public static readonly PxColorArgb Empty = new PxColorArgb(0, 0, 0, 0);

    private byte a;
    private byte r;
    private byte g;
    private byte b;

    public PxColorArgb(byte r, byte g, byte b)
    {
        A = 255;
        R = r;
        G = g;
        B = b;
    }

    public PxColorArgb(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public PxColorArgb(int argb)
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

    public static PxColorArgb FromHsl(PxColorHsl color) => PxColorHsl.ToArgb(color);
    public static PxColorHsl ToHsl(PxColorArgb color) => PxColorHsl.FromArgb(color);

    public static implicit operator System.Drawing.Color(PxColorArgb color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static implicit operator PxColorArgb(System.Drawing.Color color) => new PxColorArgb(color.A, color.R, color.G, color.B);

    public static explicit operator int(PxColorArgb color) => color.ToArgb();
    public static explicit operator PxColorArgb(int argb) => new PxColorArgb(argb);

    public static bool operator ==(PxColorArgb left, PxColorArgb right) => left.A == right.A && left.R == right.R && left.G == right.G && left.B == right.B;
    public static bool operator !=(PxColorArgb left, PxColorArgb right) => !(left == right);

    public readonly int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
    public readonly string ToHexString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public readonly bool Equals(PxColorArgb other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxColorArgb other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(A, R, G, B);
    public override readonly string ToString() => $"({A},{R},{G},{B})";
}
 