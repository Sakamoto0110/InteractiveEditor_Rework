

namespace InteractiveEditor.Primitives;

public partial struct HslColor : IEquatable<HslColor>
{
    private byte a;
    private float h;
    private float s;
    private float l;

    public HslColor(float h, float s, float l)
    {
        A = 255;
        H = h;
        S = s;
        L = l;
    }

    public HslColor(byte a, float h, float s, float l)
    {
        A = a;
        H = h;
        S = s;
        L = l;
    }

    public HslColor(ArgbColor color)
    {
        A = color.A;

        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        L = (max + min) / 2f;

        if (delta == 0f)
        {
            H = 0f;
            S = 0f;
            return;
        }

        S = delta / (1f - Math.Abs(2f * L - 1f));

        if (max == r)
            H = 60f * (((g - b) / delta) % 6f);
        else if (max == g)
            H = 60f * (((b - r) / delta) + 2f);
        else
            H = 60f * (((r - g) / delta) + 4f);

        if (H < 0f)
            H += 360f;
    }

    public byte A
    {
        readonly get => a;
        set => a = value;
    }

    public float H
    {
        readonly get => h;
        set => h = value;
    }

    public float S
    {
        readonly get => s;
        set => s = value;
    }

    public float L
    {
        readonly get => l;
        set => l = value;
    }

    public readonly bool IsEmpty => A == 0 && H == 0f && S == 0f && L == 0f;

    public static implicit operator HslColor(ArgbColor color) => new HslColor(color);

    public static implicit operator ArgbColor(HslColor color)
    {
        float h = color.H % 360f;

        if (h < 0f)
            h += 360f;

        float c = (1f - Math.Abs(2f * color.L - 1f)) * color.S;
        float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        float m = color.L - c / 2f;

        float r;
        float g;
        float b;

        if (h < 60f)
        {
            r = c;
            g = x;
            b = 0f;
        }
        else if (h < 120f)
        {
            r = x;
            g = c;
            b = 0f;
        }
        else if (h < 180f)
        {
            r = 0f;
            g = c;
            b = x;
        }
        else if (h < 240f)
        {
            r = 0f;
            g = x;
            b = c;
        }
        else if (h < 300f)
        {
            r = x;
            g = 0f;
            b = c;
        }
        else
        {
            r = c;
            g = 0f;
            b = x;
        }

        return new ArgbColor(
            color.A,
            (byte)Math.Round((r + m) * 255f),
            (byte)Math.Round((g + m) * 255f),
            (byte)Math.Round((b + m) * 255f));
    }

    public static implicit operator System.Drawing.Color(HslColor color) => (ArgbColor)color;
    public static implicit operator HslColor(System.Drawing.Color color) => new HslColor(new ArgbColor(color.A, color.R, color.G, color.B));

    public static bool operator ==(HslColor left, HslColor right) => left.A == right.A && left.H == right.H && left.S == right.S && left.L == right.L;
    public static bool operator !=(HslColor left, HslColor right) => !(left == right);

    public readonly bool Equals(HslColor other) => this == other;
    public override readonly bool Equals(object? obj) => obj is HslColor other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(A, H, S, L);
    public override readonly string ToString() => $"({A},{H},{S},{L})";
}
 