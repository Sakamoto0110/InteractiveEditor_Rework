using DemoObjects.ClassObjects;
using InteractiveEditor.Attributes;
using InteractiveEditor.Options;
using InteractiveEditor.Primitives;

namespace DemoObjects.ViewObjects;

// A member for each editor of the views (P7.9), two with a long help (P7.15), for a look at them in
// WindowsHost.
public class Gadget
{
    public string Name { get; set; } = "gadget";

    [InspectorTooltip("How many there are")]
    [InspectorHelp("How many gadgets there are. The tooltip is the short description; this is the long one, shown by the (?) at the end of the label.")]
    public int Count { get; set; } = 3;

    public double Ratio { get; set; } = 0.5;

    public bool Enabled { get; set; } = true;

    public GadgetMode Mode { get; set; } = GadgetMode.Normal;

    [InspectorEditor(EditorKind.Slider)]
    [InspectorRange(0, 255, Step = 5)]
    [InspectorHelp("How solid the gadget is, from 0 (invisible) to 255 (solid), in steps of 5.")]
    public int Opacity { get; set; } = 200;

    [InspectorEditor(EditorKind.Color)]
    public PxColorArgb Fill { get; set; } = new(255, 0, 128, 255);

    public Moo Inner { get; set; } = new();

    public List<Moo> Parts { get; set; } = [new(), new()];

    [InspectorEditor(EditorKind.List)]
    public List<int> Levels { get; set; } = [10, 20, 30];

    [InspectorReadOnly]
    public string Id { get; set; } = "gadget-1";
}
