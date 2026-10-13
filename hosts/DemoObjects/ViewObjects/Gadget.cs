using DemoObjects.ClassObjects;
using PixieInspector.Attributes;
using PixieInspector.Options;
using PixieLib;

namespace DemoObjects.ViewObjects;

// A member for each editor of the views (P7.9), two with a long help (P7.15) and two that scrub, one
// across and one up and down (P7.18), for a look at them in WindowsHost.
public class Gadget
{
    public string Name { get; set; } = "gadget";

    [InspectorTooltip("How many there are")]
    [InspectorScrub(1)]
    [InspectorHelp("How many gadgets there are. The tooltip is the short description; this is the long one, shown by the (?) at the end of the label.")]
    public int Count { get; set; } = 3;

    [InspectorScrub(0.01, Axis = ScrubAxis.Vertical)]
    public double Ratio { get; set; } = 0.5;

    public bool Enabled { get; set; } = true;

    public GadgetMode Mode { get; set; } = GadgetMode.Normal;

    [InspectorEditor(EditorKind.Slider)]
    [InspectorRange(0, 255, Step = 5)]
    [InspectorHelp("How solid the gadget is, from 0 (invisible) to 255 (solid), in steps of 5.")]
    public int Opacity { get; set; } = 200;

    [InspectorEditor(EditorKind.Color)]
    public PxColorRgba Fill { get; set; } = new(0, 128, 255);

    // An arc: the direction of a cone and its width, held in radians and shown in degrees. The views that
    // draw no arc show the direction as a number.
    [InspectorArc(nameof(Spread))]
    [InspectorUnit("°", DisplayUnit.RadiansToDegrees)]
    [InspectorScrub(1)]
    public double Direction { get; set; } = Math.PI / 2;

    [InspectorUnit("°", DisplayUnit.RadiansToDegrees)]
    [InspectorRange(0, 360)]
    public double Spread { get; set; } = Math.PI / 3;

    public Moo Inner { get; set; } = new();

    public List<Moo> Parts { get; set; } = [new(), new()];

    [InspectorEditor(EditorKind.List)]
    public List<int> Levels { get; set; } = [10, 20, 30];

    [InspectorReadOnly]
    public string Id { get; set; } = "gadget-1";
}
