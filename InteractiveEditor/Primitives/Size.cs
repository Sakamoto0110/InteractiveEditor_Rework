

namespace InteractiveEditor.Primitives;

public struct Size : IEquatable<Size>
{
    public static readonly Size Empty = new Size(0, 0);

    private int width;
    private int height;

    public Size(Point pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public Size(int width, int height)
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

    public static implicit operator SizeF(Size size) => new SizeF(size.Width, size.Height);
    public static implicit operator System.Drawing.Size(Size size) => new System.Drawing.Size(size.Width, size.Height);
    public static implicit operator System.Windows.Size(Size size) => new System.Windows.Size(size.Width, size.Height);
    public static explicit operator Point(Size size) => new Point(size.Width, size.Height);

    public static Size operator +(Size left, Size right) => Add(left, right);
    public static Size operator -(Size left, Size right) => Subtract(left, right);

    public static Size operator *(int left, Size right) => Multiply(right, left);
    public static Size operator *(Size left, int right) => Multiply(left, right);
    public static Size operator /(Size left, int right) => new Size(left.Width / right, left.Height / right);

    public static SizeF operator *(float left, Size right) => Multiply(right, left);
    public static SizeF operator *(Size left, float right) => Multiply(left, right);
    public static SizeF operator /(Size left, float right) => new SizeF(left.Width / right, left.Height / right);

    public static Size operator +(Size size) => size;
    public static Size operator -(Size size) => new Size(-size.Width, -size.Height);

    public static bool operator ==(Size left, Size right) => left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(Size left, Size right) => !(left == right);

    public static Size Add(Size left, Size right) => new Size(left.Width + right.Width, left.Height + right.Height);
    public static Size Subtract(Size left, Size right) => new Size(left.Width - right.Width, left.Height - right.Height);

    public static Size Ceiling(SizeF value) => new Size((int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));
    public static Size Truncate(SizeF value) => new Size((int)value.Width, (int)value.Height);
    public static Size Round(SizeF value) => new Size((int)Math.Round(value.Width), (int)Math.Round(value.Height));

    public readonly bool Equals(Size other) => this == other;
    public override readonly bool Equals(object? obj) => obj is Size other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
    public override readonly string ToString() => $"{Width},{Height}";

    private static Size Multiply(Size size, int multiplier) => new Size(size.Width * multiplier, size.Height * multiplier);
    private static SizeF Multiply(Size size, float multiplier) => new SizeF(size.Width * multiplier, size.Height * multiplier);
}
 