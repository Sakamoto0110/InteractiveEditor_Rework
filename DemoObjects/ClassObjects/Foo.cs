namespace DemoObjects.ClassObjects;

public class Foo
{
    public int x { get; set; }
    public int y { get; set; }
    public Moo Moo { get; set; } = new Moo();
    public Doo Doo { get; set; } = new Doo();   
}
