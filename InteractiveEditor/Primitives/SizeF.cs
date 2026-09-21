

namespace InteractiveEditor.Primitives;

public struct SizeF : IEquatable<SizeF>
{
    public static readonly SizeF Empty = new SizeF(0f, 0f);

    private float width;
    private float height;

    public SizeF(SizeF size)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public SizeF(PointF pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public SizeF(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public float Width
    {
        readonly get => width;
        set => width = value;
    }

    public float Height
    {
        readonly get => height;
        set => height = value;
    }

    public readonly bool IsEmpty => Width == 0f && Height == 0f;

    public static implicit operator System.Drawing.SizeF(SizeF size) => new System.Drawing.SizeF(size.Width, size.Height);
    public static implicit operator System.Windows.Size(SizeF size) => new System.Windows.Size(size.Width, size.Height);
    public static explicit operator PointF(SizeF size) => new PointF(size.Width, size.Height);

    public static SizeF operator +(SizeF left, SizeF right) => Add(left, right);
    public static SizeF operator -(SizeF left, SizeF right) => Subtract(left, right);

    public static SizeF operator *(float left, SizeF right) => Multiply(right, left);
    public static SizeF operator *(SizeF left, float right) => Multiply(left, right);
    public static SizeF operator /(SizeF left, float right) => new SizeF(left.Width / right, left.Height / right);

    public static SizeF operator +(SizeF size) => size;
    public static SizeF operator -(SizeF size) => new SizeF(-size.Width, -size.Height);

    public static bool operator ==(SizeF left, SizeF right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(SizeF left, SizeF right) => !(left == right);

    public static SizeF Add(SizeF left, SizeF right) => new SizeF(left.Width + right.Width, left.Height + right.Height);
    public static SizeF Subtract(SizeF left, SizeF right) => new SizeF(left.Width - right.Width, left.Height - right.Height);

    public readonly bool Equals(SizeF other) => this == other;
    public override readonly bool Equals(object? obj) => obj is SizeF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static SizeF Multiply(SizeF size, float multiplier) => new SizeF(size.Width * multiplier, size.Height * multiplier);
}
 