namespace InteractiveEditor.Primitives;

// Space around something, in double: the Padding of WinForms and the Thickness of WPF (P8.2). The
// conversions with them come with the views.
public struct PxPadding : IEquatable<PxPadding>
{
    public static readonly PxPadding Empty = new PxPadding(0);

    private double left;
    private double top;
    private double right;
    private double bottom;

    public PxPadding(double all) : this(all, all, all, all)
    {
    }

    public PxPadding(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left
    {
        readonly get => left;
        set => left = value;
    }

    public double Top
    {
        readonly get => top;
        set => top = value;
    }

    public double Right
    {
        readonly get => right;
        set => right = value;
    }

    public double Bottom
    {
        readonly get => bottom;
        set => bottom = value;
    }

    // The space taken across and along, as in the Padding of WinForms.
    public readonly double Horizontal => Left + Right;
    public readonly double Vertical => Top + Bottom;

    public readonly bool IsEmpty => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    public static bool operator ==(PxPadding left, PxPadding right) =>
        left.Left == right.Left && left.Top == right.Top && left.Right == right.Right && left.Bottom == right.Bottom;

    public static bool operator !=(PxPadding left, PxPadding right) => !(left == right);

    public readonly bool Equals(PxPadding other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxPadding other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
    public override readonly string ToString() => FormattableString.Invariant($"{Left},{Top},{Right},{Bottom}");
}
