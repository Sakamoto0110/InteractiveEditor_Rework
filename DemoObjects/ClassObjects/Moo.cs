using System;
using System.Collections.Generic;
using System.Text;

namespace DemoObjects.ClassObjects;

public class Moo
{

    public int MooX { get; set; } = 13;
    public int MooY { get; set; } = 31;
    public Doo Doo { get; set; } = new Doo();
}
