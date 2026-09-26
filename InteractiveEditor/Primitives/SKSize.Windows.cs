namespace InteractiveEditor.Primitives;

public partial struct SKSize
{
    public static implicit operator System.Windows.Size(SKSize size) => new System.Windows.Size(size.Width, size.Height);
}
