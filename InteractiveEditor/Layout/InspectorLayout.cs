using InteractiveEditor.Options;
using PixieLib;

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

    // The area the rows take, with the padding: the width given, up to MaxWidth, and the height they
    // need.
    public PxSize Size { get; }

    // The layout of the rows below the root, in an area this wide. It does not read the bound objects:
    // a list editor is as high as the items it listed at the last read.
    internal static InspectorLayout Compute(InspectorNode root, InspectorOptions options, double width)
    {
        if (!double.IsFinite(width) || width < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "The width is a number from zero up.");

        // The rows take the width given, up to MaxWidth (P7.14); a wider view leaves the rest empty.
        width = Math.Min(width, options.MaxWidth ?? double.PositiveInfinity);

        // The column of help marks before the editors (P7.15), on every row when any node of the tree has
        // Help, so the editors line up; the whole tree counts, so they do not jump when a row hides.
        var marks = root.Any(node => !string.IsNullOrEmpty(node.Help)) ? options.HelpWidth + options.LabelSpacing : 0;

        var padding = options.Padding;
        var rows = Stack(root, options, marks, 0, padding.Left, padding.Top, Math.Max(0, width - padding.Horizontal), out var height);

        return new InspectorLayout(rows, new PxSize(width, padding.Vertical + height));
    }

    // The rows below a node, one after the other from (x, y), in a column this wide; height is what they
    // take together. Marks is the width of the column of help marks, zero when there is none.
    private static IReadOnlyList<LayoutRow> Stack(InspectorNode parent, InspectorOptions options, double marks, int depth, double x,
        double y, double width, out double height)
    {
        var rows = new List<LayoutRow>();
        var bottom = y;

        foreach (var node in parent.ShownChildren)
        {
            var row = Place(node, options, marks, depth, x, rows.Count == 0 ? y : bottom + options.RowSpacing, width);
            rows.Add(row);
            bottom = row.Panel.Height > 0 ? row.Panel.Bottom : row.Row.Bottom;
        }

        height = bottom - y;
        return rows.AsReadOnly();
    }

    // One row at (x, y): the label in its column, the editor in the rest of the row, and a group's panel
    // below it. A header (the group of a type, P5.9) or a separator takes the whole row with its label.
    private static LayoutRow Place(InspectorNode node, InspectorOptions options, double marks, int depth, double x, double y,
        double width)
    {
        var row = new PxRect(x, y, width, HeightOf(node, options));

        var across = node.Editor is EditorKind.Header or EditorKind.Separator;
        var labelWidth = across ? width : Math.Clamp(options.LabelWidth - depth * options.Indent, 0, width);

        var label = new PxRect(x, y, labelWidth, options.RowHeight);

        // The editor takes the rest of the row, after the column of help marks, up to EditorMaxWidth;
        // what is left over is the space between the label and the editor, so the editors line up on
        // the right (P7.14).
        var start = Math.Min(x + labelWidth + options.LabelSpacing + marks, row.Right);
        var editorWidth = Math.Min(row.Right - start, options.EditorMaxWidth ?? double.PositiveInfinity);
        var editor = new PxRect(row.Right - editorWidth, y, editorWidth, row.Height);

        // The help mark goes right before the editor, in the column kept for it: out of the space when
        // there is one, and never over the label (P7.15).
        var help = PxRect.Empty;

        if (marks > 0 && !across && !string.IsNullOrEmpty(node.Help))
        {
            var right = Math.Max(label.Right, editor.X - options.LabelSpacing);
            var left = Math.Max(label.Right, right - options.HelpWidth);
            help = new PxRect(left, y, right - left, options.RowHeight);
        }

        if (!node.IsGroup)
            return new LayoutRow(node, depth, row, label, help, editor, PxRect.Empty, []);

        // The panel goes in by one indent, and its rows are laid out from its corner. They are there even
        // while the group is collapsed, when the panel has no height, so a view can build them once.
        var inner = Math.Max(0, width - options.Indent);
        var rows = Stack(node, options, marks, depth + 1, 0, 0, inner, out var content);
        var panel = new PxRect(x + options.Indent, row.Bottom + options.RowSpacing, inner, node.Collapsed ? 0 : content);

        return new LayoutRow(node, depth, row, label, help, editor, panel, rows);
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
