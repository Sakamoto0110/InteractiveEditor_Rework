namespace InteractiveEditor.Primitives;

public partial struct SKSizeF
{
    public static implicit operator System.Windows.Size(SKSizeF size) => new System.Windows.Size(size.Width, size.Height);
}
