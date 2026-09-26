using DemoObjects.ClassObjects;
using InteractiveEditor.Attributes;
using InteractiveEditor.Options;

namespace DemoObjects.AttributedObjects;

public class Boo
{
    [InspectorOrder(-1)]
    public string Name { get; set; } = "Boo";

    [InspectorReadOnly]
    public string Id { get; set; } = "boo-1";

    [InspectorLabel("Position X")]
    [InspectorTooltip("Horizontal position")]
    [InspectorHelp("Horizontal position in pixels, from the left edge of the canvas.")]
    [InspectorScrub(1)]
    public int X { get; set; } = 10;

    [InspectorEditor(EditorKind.Slider)]
    [InspectorRange(0, 255, Step = 1)]
    public int Opacity { get; set; } = 255;

    public string Secret { get; private set; } = "only readable";

    [InspectorIgnore]
    public int Internal { get; set; }

    [InspectorExpandable(Collapsed = true)]
    public Moo Details { get; set; } = new();

    public Doo Extra { get; set; } = new();
}
