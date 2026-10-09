using DemoObjects.ClassObjects;
using DemoObjects.StructObjects;

namespace DemoObjects.HybridObjects;

public class Hoo
{
    public string Name { get; set; } = "hoo";
    public bool Enabled { get; set; } = true;
    public DayOfWeek Day { get; set; } = DayOfWeek.Monday;
    public int? Optional { get; set; }
    public Boo Boo { get; set; } = new Boo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } };
    public Moo Moo { get; set; } = new Moo();
}
