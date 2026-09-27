

namespace InteractiveEditor.Primitives;

public struct PxSizeF : IEquatable<PxSizeF>
{
    public static readonly PxSizeF Empty = new PxSizeF(0f, 0f);

    private float width;
    private float height;

    public PxSizeF(PxSizeF size)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public PxSizeF(PxPointF pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public PxSizeF(float width, float height)
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

    public static implicit operator System.Drawing.SizeF(PxSizeF size) => new System.Drawing.SizeF(size.Width, size.Height);
    public static explicit operator PxPointF(PxSizeF size) => new PxPointF(size.Width, size.Height);

    public static PxSizeF operator +(PxSizeF left, PxSizeF right) => Add(left, right);
    public static PxSizeF operator -(PxSizeF left, PxSizeF right) => Subtract(left, right);

    public static PxSizeF operator *(float left, PxSizeF right) => Multiply(right, left);
    public static PxSizeF operator *(PxSizeF left, float right) => Multiply(left, right);
    public static PxSizeF operator /(PxSizeF left, float right) => new PxSizeF(left.Width / right, left.Height / right);

    public static PxSizeF operator +(PxSizeF size) => size;
    public static PxSizeF operator -(PxSizeF size) => new PxSizeF(-size.Width, -size.Height);

    public static bool operator ==(PxSizeF left, PxSizeF right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(PxSizeF left, PxSizeF right) => !(left == right);

    public static PxSizeF Add(PxSizeF left, PxSizeF right) => new PxSizeF(left.Width + right.Width, left.Height + right.Height);
    public static PxSizeF Subtract(PxSizeF left, PxSizeF right) => new PxSizeF(left.Width - right.Width, left.Height - right.Height);

    public readonly bool Equals(PxSizeF other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxSizeF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static PxSizeF Multiply(PxSizeF size, float multiplier) => new PxSizeF(size.Width * multiplier, size.Height * multiplier);
}
 