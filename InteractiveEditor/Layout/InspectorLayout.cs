using InteractiveEditor.Options;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.Layout;

// The layout step (P7.5): the rectangles of the rows a view shows, worked out in the core with no UI,
// from the rows (Rows), their depth, the collapsed groups and the inspector's options. A view only puts
// its controls on them, with a panel for each group (P7.3), so the step runs, and is tested, anywhere.
public sealed class InspectorLayout
{
    private InspectorLayout(IReadOnlyList<LayoutRow> rows, PxSize size)
    {
        Rows = rows;
        Size = size;
    }

    // The rows at the top, in the inspector's area; a group holds the rows inside it.
    public IReadOnlyList<LayoutRow> Rows { get; }

    // The area the rows take, with the padding: the width given, and the height they need.
    public PxSize Size { get; }

    // The layout of the rows below the root, in an area this wide. It does not read the bound objects:
    // a list editor is as high as the items it listed at the last read.
    internal static InspectorLayout Compute(InspectorNode root, InspectorOptions options, double width)
    {
        if (!double.IsFinite(width) || width < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "The width is a number from zero up.");

        var padding = options.Padding;
        var rows = Stack(root, options, 0, padding.Left, padding.Top, Math.Max(0, width - padding.Horizontal), out var height);

        return new InspectorLayout(rows, new PxSize(width, padding.Vertical + height));
    }

    // The rows below a node, one after the other from (x, y), in a column this wide; height is what they
    // take together.
    private static IReadOnlyList<LayoutRow> Stack(InspectorNode parent, InspectorOptions options, int depth, double x, double y,
        double width, out double height)
    {
        var rows = new List<LayoutRow>();
        var bottom = y;

        foreach (var node in parent.ShownChildren)
        {
            var row = Place(node, options, depth, x, rows.Count == 0 ? y : bottom + options.RowSpacing, width);
            rows.Add(row);
            bottom = row.Panel.Height > 0 ? row.Panel.Bottom : row.Row.Bottom;
        }

        height = bottom - y;
        return rows.AsReadOnly();
    }

    // One row at (x, y): the label in its column, the editor in the rest of the row, and a group's panel
    // below it. A header (the group of a type, P5.9) or a separator takes the whole row with its label.
    private static LayoutRow Place(InspectorNode node, InspectorOptions options, int depth, double x, double y, double width)
    {
        var row = new PxRect(x, y, width, HeightOf(node, options));

        var labelWidth = node.Editor is EditorKind.Header or EditorKind.Separator
            ? width
            : Math.Clamp(options.LabelWidth - depth * options.Indent, 0, width);
        var editorX = Math.Min(x + labelWidth + options.LabelSpacing, row.Right);
        var label = new PxRect(x, y, labelWidth, options.RowHeight);
        var editor = new PxRect(editorX, y, row.Right - editorX, row.Height);

        if (!node.IsGroup)
            return new LayoutRow(node, depth, row, label, editor, PxRect.Empty, []);

        // The panel goes in by one indent, and its rows are laid out from its corner. They are there even
        // while the group is collapsed, when the panel has no height, so a view can build them once.
        var inner = Math.Max(0, width - options.Indent);
        var rows = Stack(node, options, depth + 1, 0, 0, inner, out var content);
        var panel = new PxRect(x + options.Indent, row.Bottom + options.RowSpacing, inner, node.Collapsed ? 0 : content);

        return new LayoutRow(node, depth, row, label, editor, panel, rows);
    }

    // A row is one line high; a list editor has a line for each item it shows, up to ListRows, and one
    // more for its buttons (P5.10).
    private static double HeightOf(InspectorNode node, InspectorOptions options)
    {
        if (node is not CollectionNode { Editor: EditorKind.List } collection)
            return options.RowHeight;

        return (Math.Clamp(collection.Items.Count, 1, options.ListRows) + 1) * options.RowHeight;
    }
}
