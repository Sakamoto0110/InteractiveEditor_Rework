namespace PixieInspector.Options;

public enum EditorKind
{
    Auto,
    Text,
    Number,
    Toggle,
    Choice,
    Slider,
    Color,
    Button,
    Display,
    Header,
    Separator,

    // A collection: a list of its items to choose one, which shows in the row below (P5.10).
    Selector,

    // A collection shown one row per item, with adding, removing and moving them: a choice, as
    // [InspectorEditor(EditorKind.List)] (P5.10). The item chosen still shows in the row below.
    List,

    // An angle and the width of an arc around it, edited together: the node is the center, and its
    // ArcSpread names the sibling with the width, both in radians. A view that draws no arc shows a number.
    Arc,
}
