

namespace InteractiveEditor.Primitives;

// A size in double, the only precision (P8.1), with the same conversions as PxPoint: implicit from
// System.Drawing, explicit back to it.
public struct PxSize : IEquatable<PxSize>
{
    public static readonly PxSize Empty = new PxSize(0, 0);

    private double width;
    private double height;

    public PxSize(PxPoint pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public PxSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width
    {
        readonly get => width;
        set => width = value;
    }

    public double Height
    {
        readonly get => height;
        set => height = value;
    }

    public readonly bool IsEmpty => Width == 0 && Height == 0;

    public static implicit operator PxSize(System.Drawing.Size size) => new PxSize(size.Width, size.Height);
    public static implicit operator PxSize(System.Drawing.SizeF size) => new PxSize(size.Width, size.Height);
    public static explicit operator System.Drawing.Size(PxSize size) => new System.Drawing.Size((int)Math.Round(size.Width), (int)Math.Round(size.Height));
    public static explicit operator System.Drawing.SizeF(PxSize size) => new System.Drawing.SizeF((float)size.Width, (float)size.Height);
    public static explicit operator PxPoint(PxSize size) => new PxPoint(size.Width, size.Height);

    public static PxSize operator +(PxSize left, PxSize right) => Add(left, right);
    public static PxSize operator -(PxSize left, PxSize right) => Subtract(left, right);

    public static PxSize operator *(double left, PxSize right) => Multiply(right, left);
    public static PxSize operator *(PxSize left, double right) => Multiply(left, right);
    public static PxSize operator /(PxSize left, double right) => new PxSize(left.Width / right, left.Height / right);

    public static PxSize operator +(PxSize size) => size;
    public static PxSize operator -(PxSize size) => new PxSize(-size.Width, -size.Height);

    public static bool operator ==(PxSize left, PxSize right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(PxSize left, PxSize right) => !(left == right);

    public static PxSize Add(PxSize left, PxSize right) => new PxSize(left.Width + right.Width, left.Height + right.Height);
    public static PxSize Subtract(PxSize left, PxSize right) => new PxSize(left.Width - right.Width, left.Height - right.Height);

    public readonly bool Equals(PxSize other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxSize other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => FormattableString.Invariant($"{Width},{Height}");

    private static PxSize Multiply(PxSize size, double multiplier) => new PxSize(size.Width * multiplier, size.Height * multiplier);
}
 