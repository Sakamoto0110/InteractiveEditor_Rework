using InteractiveEditor.Primitives;

namespace InteractiveEditor.Layout;

// One row of the layout (P7.5): the node, its depth, and the rectangles of the row, its label and its
// editor, in the coordinates of what the row sits in: the inspector's area for the rows at the top, the
// panel of a group for the rows inside it. A group has its panel right below its row, one indent in,
// with its own rows; a collapsed group keeps them, and its panel has no height. A leaf has neither.
public sealed record LayoutRow(
    InspectorNode Node,
    int Depth,
    PxRect Row,
    PxRect Label,
    PxRect Editor,
    PxRect Panel,
    IReadOnlyList<LayoutRow> Rows);
