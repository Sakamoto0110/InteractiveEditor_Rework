namespace InteractiveEditor.Options;

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
}
