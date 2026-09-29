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
}
