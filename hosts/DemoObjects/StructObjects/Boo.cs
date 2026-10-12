namespace DemoObjects.StructObjects;

// Fields only, with a nested struct: edits two levels down have to be written back through both copies.
public struct Boo
{
    public int BooX;
    public int BooY;
    public Coo Coo;
}
