using System.Numerics;
using ImGuiNET;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Options;
using InteractiveEditor.Views;
using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

// One row of the view, drawn in a line of the table: the label of a node in the first column, as a tree
// node (a group's opens and closes it, P7.10), and its editor in the second, with the failure under it in
// light red (P7.11). A number's label can be dragged to scrub it (P7.18), and a node with Help has a (?) at
// the end of its label, whose tooltip is the long help (P7.15). What fails while the row shows its objects
// stays in the row (3.11).
internal sealed class ImGuiRow
{
    private const string HelpMark = "(?)";

    private readonly ImGuiEditor? Editor;
    private readonly LabelScrub? Scrub;

    // What the core refused as a mistake at the last write, until a write goes through.
    private string? Error;

    // What the row could not show in this frame (a ToString of the objects that throws); the next frame
    // tries again.
    private string? Fault;

    public ImGuiRow(InspectorNode node)
    {
        Node = node;
        Kind = ViewRules.KindFor(node);
        IsGroup = node.IsGroup;
        Editor = ImGuiEditor.For(this, Kind);

        if (!IsGroup && Kind is not (null or EditorKind.Header or EditorKind.Separator))
            Scrub = new LabelScrub(this);
    }

    public InspectorNode Node { get; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // Whether the label scrubs the number now (P7.17).
    public bool Scrubs => Scrub != null && ViewRules.CanScrub(Node);

    // Whether the row shows its objects as holding different values (P7.19).
    public bool Mixed => ViewRules.ShowsMixed(Kind, Node);

    public bool Fits() => ViewRules.KindFor(Node) == Kind && Node.IsGroup == IsGroup;

    // Draws the row in the next line of the table. True when it is an open group: its rows come next, and
    // the view puts them a level in.
    public bool Draw(bool focus)
    {
        Fault = null;
        Gui.PushID(Node.Name);

        try
        {
            Gui.TableNextRow();

            if (Kind == EditorKind.Separator)
            {
                DrawSeparator();
                return false;
            }

            Gui.TableSetColumnIndex(0);
            // Lines the label up with the framed editor in the next column.
            Gui.AlignTextToFramePadding();
            var open = DrawLabel();

            // A header spans both columns with its label.
            if (Kind != EditorKind.Header)
            {
                Gui.TableSetColumnIndex(1);
                DrawValue(focus);
            }

            return IsGroup && open;
        }
        finally
        {
            Gui.PopID();
        }
    }

    // Writes through the node: a value, a press, an operation of the list. What the core refuses as a
    // mistake (a read-only node, a disabled branch, nowhere to write) shows on the row instead of bringing
    // the view down.
    public bool Write(Action write)
    {
        try
        {
            write();
            Error = null;
            return true;
        }
        catch (Exception e)
        {
            Error = e.Message;
            return false;
        }
    }

    // Something of the objects threw while the row showed them in this frame: it shows under the editor,
    // and the next frame tries again.
    public void OnFault(Exception e)
    {
        Fault ??= $"This row could not show its objects.\n{e.Message}";
    }

    // The label as a tree node: a leaf for a value, one that opens and closes for a group, and a framed
    // band across both columns for a header. A group follows the node's Collapsed both ways, so a group
    // collapsed by its options starts closed, and a click collapses the node. It lets the help mark over
    // it take the mouse.
    private bool DrawLabel()
    {
        var flags = ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.AllowOverlap;

        if (Kind == EditorKind.Header)
            flags |= ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAllColumns;

        if (IsGroup)
            Gui.SetNextItemOpen(!Node.Collapsed);
        else
            flags |= ImGuiTreeNodeFlags.Leaf;

        // The label goes in as the format of the tree node, so a '%' in it is doubled.
        var open = Gui.TreeNodeEx("##label", flags, Node.Label.Replace("%", "%%"));

        if (IsGroup && open == Node.Collapsed)
            Node.Collapsed = !open;

        Scrub?.Update();

        var detail = Node.ValueType is { } type ? $"{TypeName(type)}  {Node.Path}" : Node.Path;
        ImGuiInspectorView.Tooltip(Node.Tooltip, detail);

        if (!string.IsNullOrEmpty(Node.Help) && Kind != EditorKind.Header)
            DrawHelpMark(Node.Help);

        return open;
    }

    // The (?) at the right end of the label, whose tooltip is the long help (P7.15). An invisible button
    // takes the mouse over it, so the label's tooltip does not show along with it.
    private static void DrawHelpMark(string help)
    {
        var size = new Vector2(Gui.CalcTextSize(HelpMark).X, Gui.GetFrameHeight());

        Gui.SameLine();
        var room = Gui.GetContentRegionAvail().X - size.X;

        if (room > 0)
            Gui.SetCursorPosX(Gui.GetCursorPosX() + room);

        var corner = Gui.GetCursorScreenPos();
        Gui.InvisibleButton("##help", size);
        Gui.GetWindowDrawList().AddText(corner + new Vector2(0, Gui.GetStyle().FramePadding.Y), Gui.GetColorU32(ImGuiCol.TextDisabled), HelpMark);
        ImGuiInspectorView.Tooltip(help);
    }

    // The second column: the editor, or for a group with none what it holds, and the failure under it.
    // What the objects throw while the row shows them (their Equals for the mixed values, their ToString)
    // is the row's fault; the editors keep the calls into the objects out of ImGui's Begin and End pairs,
    // and the editor's own pairs close on the way, so the frame goes on.
    private void DrawValue(bool focus)
    {
        try
        {
            if (Editor == null)
                DrawSummary();
            else if (HoldsNothing())
                ImGuiInspectorView.Text("null", ImGuiInspectorView.DisabledColor);
            else
                DrawEditor(Editor, focus);
        }
        catch (Exception e)
        {
            OnFault(e);
        }

        if ((Error ?? Fault ?? Describe(Node.Failure)) is { } failure)
            ImGuiInspectorView.Text(failure, ImGuiInspectorView.ErrorColor);
    }

    // The editor as one group, for the tooltip of the node over all of it, disabled while it takes no input.
    private void DrawEditor(ImGuiEditor editor, bool focus)
    {
        var enabled = editor.Enabled;

        if (!enabled)
            Gui.BeginDisabled();

        Gui.BeginGroup();

        try
        {
            if (focus)
                Gui.SetKeyboardFocusHere();

            Gui.SetNextItemWidth(-float.Epsilon);
            editor.Draw();
        }
        finally
        {
            Gui.EndGroup();

            if (!enabled)
                Gui.EndDisabled();
        }

        ImGuiInspectorView.Tooltip(Node.Tooltip);
    }

    // A value type reads null only when there is nothing to read it from (its owner is null, nothing is
    // bound, the read failed), and then there is nothing to edit, as with a group that is null.
    private bool HoldsNothing()
    {
        return Kind is not (EditorKind.Button or EditorKind.Display or EditorKind.Selector or EditorKind.List)
            && Node.ViewValue == null && Node.ValueType is { IsValueType: true } type && Nullable.GetUnderlyingType(type) == null
            && !Node.IsMixed;
    }

    // What a group with no editor shows next to its label: the type it holds, and whether it is null.
    private void DrawSummary()
    {
        var type = Node.ValueType is { } held ? TypeName(held) : string.Empty;
        var text = Node.ViewValue == null && !Node.IsMixed ? $"{type} = null" : type;
        ImGuiInspectorView.Text(text, ImGuiInspectorView.DisabledColor);
    }

    // A line across both columns, with no label (P7.9).
    private static void DrawSeparator()
    {
        Gui.TableSetColumnIndex(0);
        Gui.Separator();
        Gui.TableSetColumnIndex(1);
        Gui.Separator();
    }

    private static string? Describe(InspectorFailureEventArgs? failure)
    {
        if (failure == null)
            return null;

        return failure.Message.Contains(failure.Reason) ? failure.Message : $"{failure.Message}\n{failure.Reason}";
    }

    // Int32?, List<Moo>, ... instead of Nullable`1 and List`1.
    private static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{TypeName(underlying)}?";

        var tick = type.Name.IndexOf('`');

        if (!type.IsGenericType || tick < 0)
            return type.Name;

        return $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }
}
