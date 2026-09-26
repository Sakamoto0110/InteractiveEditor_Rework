

namespace InteractiveEditor.Primitives;

public partial struct SKSizeF : IEquatable<SKSizeF>
{
    public static readonly SKSizeF Empty = new SKSizeF(0f, 0f);

    private float width;
    private float height;

    public SKSizeF(SKSizeF size)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public SKSizeF(SKPointF pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public SKSizeF(float width, float height)
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

    public static implicit operator System.Drawing.SizeF(SKSizeF size) => new System.Drawing.SizeF(size.Width, size.Height);
    public static explicit operator SKPointF(SKSizeF size) => new SKPointF(size.Width, size.Height);

    public static SKSizeF operator +(SKSizeF left, SKSizeF right) => Add(left, right);
    public static SKSizeF operator -(SKSizeF left, SKSizeF right) => Subtract(left, right);

    public static SKSizeF operator *(float left, SKSizeF right) => Multiply(right, left);
    public static SKSizeF operator *(SKSizeF left, float right) => Multiply(left, right);
    public static SKSizeF operator /(SKSizeF left, float right) => new SKSizeF(left.Width / right, left.Height / right);

    public static SKSizeF operator +(SKSizeF size) => size;
    public static SKSizeF operator -(SKSizeF size) => new SKSizeF(-size.Width, -size.Height);

    public static bool operator ==(SKSizeF left, SKSizeF right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(SKSizeF left, SKSizeF right) => !(left == right);

    public static SKSizeF Add(SKSizeF left, SKSizeF right) => new SKSizeF(left.Width + right.Width, left.Height + right.Height);
    public static SKSizeF Subtract(SKSizeF left, SKSizeF right) => new SKSizeF(left.Width - right.Width, left.Height - right.Height);

    public readonly bool Equals(SKSizeF other) => this == other;
    public override readonly bool Equals(object? obj) => obj is SKSizeF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static SKSizeF Multiply(SKSizeF size, float multiplier) => new SKSizeF(size.Width * multiplier, size.Height * multiplier);
}
 