

namespace InteractiveEditor.Primitives;

public partial struct SKSize : IEquatable<SKSize>
{
    public static readonly SKSize Empty = new SKSize(0, 0);

    private int width;
    private int height;

    public SKSize(SKPoint pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public SKSize(int width, int height)
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

    public static implicit operator SKSizeF(SKSize size) => new SKSizeF(size.Width, size.Height);
    public static implicit operator System.Drawing.Size(SKSize size) => new System.Drawing.Size(size.Width, size.Height);
    public static explicit operator SKPoint(SKSize size) => new SKPoint(size.Width, size.Height);

    public static SKSize operator +(SKSize left, SKSize right) => Add(left, right);
    public static SKSize operator -(SKSize left, SKSize right) => Subtract(left, right);

    public static SKSize operator *(int left, SKSize right) => Multiply(right, left);
    public static SKSize operator *(SKSize left, int right) => Multiply(left, right);
    public static SKSize operator /(SKSize left, int right) => new SKSize(left.Width / right, left.Height / right);

    public static SKSizeF operator *(float left, SKSize right) => Multiply(right, left);
    public static SKSizeF operator *(SKSize left, float right) => Multiply(left, right);
    public static SKSizeF operator /(SKSize left, float right) => new SKSizeF(left.Width / right, left.Height / right);

    public static SKSize operator +(SKSize size) => size;
    public static SKSize operator -(SKSize size) => new SKSize(-size.Width, -size.Height);

    public static bool operator ==(SKSize left, SKSize right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(SKSize left, SKSize right) => !(left == right);

    public static SKSize Add(SKSize left, SKSize right) => new SKSize(left.Width + right.Width, left.Height + right.Height);
    public static SKSize Subtract(SKSize left, SKSize right) => new SKSize(left.Width - right.Width, left.Height - right.Height);

    public static SKSize Ceiling(SKSizeF value) => new SKSize((int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));
    public static SKSize Truncate(SKSizeF value) => new SKSize((int)value.Width, (int)value.Height);
    public static SKSize Round(SKSizeF value) => new SKSize((int)Math.Round(value.Width), (int)Math.Round(value.Height));

    public readonly bool Equals(SKSize other) => this == other;
    public override readonly bool Equals(object? obj) => obj is SKSize other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static SKSize Multiply(SKSize size, int multiplier) => new SKSize(size.Width * multiplier, size.Height * multiplier);
    private static SKSizeF Multiply(SKSize size, float multiplier) => new SKSizeF(size.Width * multiplier, size.Height * multiplier);
}
 