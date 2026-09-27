

namespace InteractiveEditor.Primitives;

public struct PxSize : IEquatable<PxSize>
{
    public static readonly PxSize Empty = new PxSize(0, 0);

    private int width;
    private int height;

    public PxSize(PxPoint pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public PxSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width
    {
        readonly get => width;
        set => width = value;
    }

    public int Height
    {
        readonly get => height;
        set => height = value;
    }

    public readonly bool IsEmpty => Width == 0 && Height == 0;

    public static implicit operator PxSizeF(PxSize size) => new PxSizeF(size.Width, size.Height);
    public static implicit operator System.Drawing.Size(PxSize size) => new System.Drawing.Size(size.Width, size.Height);
    public static explicit operator PxPoint(PxSize size) => new PxPoint(size.Width, size.Height);

    public static PxSize operator +(PxSize left, PxSize right) => Add(left, right);
    public static PxSize operator -(PxSize left, PxSize right) => Subtract(left, right);

    public static PxSize operator *(int left, PxSize right) => Multiply(right, left);
    public static PxSize operator *(PxSize left, int right) => Multiply(left, right);
    public static PxSize operator /(PxSize left, int right) => new PxSize(left.Width / right, left.Height / right);

    public static PxSizeF operator *(float left, PxSize right) => Multiply(right, left);
    public static PxSizeF operator *(PxSize left, float right) => Multiply(left, right);
    public static PxSizeF operator /(PxSize left, float right) => new PxSizeF(left.Width / right, left.Height / right);

    public static PxSize operator +(PxSize size) => size;
    public static PxSize operator -(PxSize size) => new PxSize(-size.Width, -size.Height);

    public static bool operator ==(PxSize left, PxSize right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(PxSize left, PxSize right) => !(left == right);

    public static PxSize Add(PxSize left, PxSize right) => new PxSize(left.Width + right.Width, left.Height + right.Height);
    public static PxSize Subtract(PxSize left, PxSize right) => new PxSize(left.Width - right.Width, left.Height - right.Height);

    public static PxSize Ceiling(PxSizeF value) => new PxSize((int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));
    public static PxSize Truncate(PxSizeF value) => new PxSize((int)value.Width, (int)value.Height);
    public static PxSize Round(PxSizeF value) => new PxSize((int)Math.Round(value.Width), (int)Math.Round(value.Height));

    public readonly bool Equals(PxSize other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxSize other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static PxSize Multiply(PxSize size, int multiplier) => new PxSize(size.Width * multiplier, size.Height * multiplier);
    private static PxSizeF Multiply(PxSize size, float multiplier) => new PxSizeF(size.Width * multiplier, size.Height * multiplier);
}
 