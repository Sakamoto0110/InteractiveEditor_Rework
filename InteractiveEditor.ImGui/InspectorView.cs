using System.Numerics;
using ImGuiNET;
using InteractiveEditor.Binding;
using InteractiveEditor.Presentation;
using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

/// <summary>
/// Dear ImGui view of an <see cref="InteractiveEditor.Inspector"/>: a two-column table where nested inspectors are
/// tree nodes and fieldsets are rows with an editor picked from the member type. Being immediate mode, it reads
/// every value on every frame, so changes made outside the view show up without a refresh.
/// </summary>
public class InspectorView : IInspectorView
{
    internal static readonly Vector4 DisabledColor = new(0.6f, 0.6f, 0.6f, 1f);

    private readonly Dictionary<Fieldset, FieldsetView> Fields = [];
    private IImGuiHost? Host;
    private Fieldset? PendingFocus;

    public InspectorView(Inspector inspector)
    {
        Inspector = inspector;
        Title = inspector.Name;

        foreach (var field in inspector.OfType<Fieldset>())
            Fields[field] = new FieldsetView(field, this);
    }

    public InspectorView(Inspector inspector, IImGuiHost host)
        : this(inspector)
    {
        AttachToHost(host);
    }

    public Inspector Inspector { get; }

    // Title of the window DrawWindow opens. It is also the window's ImGui ID: "Label###id" keeps the ID fixed.
    public string Title { get; set; }

    public event EventHandler<FieldEditedEventArgs>? FieldEdited;

    // Subscribes DrawWindow to the host's frames.
    public void AttachToHost(object host)
    {
        if (host is not IImGuiHost imGuiHost)
            throw new ArgumentException($"Cannot attach to a {host.GetType().Name}; expected an {nameof(IImGuiHost)}.", nameof(host));

        Detach();
        Host = imGuiHost;
        Host.Frame += DrawWindow;
    }

    public void Detach()
    {
        if (Host == null)
            return;

        Host.Frame -= DrawWindow;
        Host = null;
    }

    // Gives keyboard focus to a field on the next frame, opening the tree nodes above it.
    public void Focus(Fieldset field) => PendingFocus = field;

    public void DrawWindow()
    {
        if (Gui.Begin(Title))
            Draw();

        Gui.End();
    }

    // Draws into the current window, for hosts that place the view themselves.
    public void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg;

        if (!Gui.BeginTable("##inspector", 2, flags))
            return;

        Gui.TableSetupColumn("Member", ImGuiTableColumnFlags.WidthFixed, 180f);
        Gui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        DrawNodes(Inspector);

        Gui.EndTable();
    }

    internal void OnFieldEdited(Fieldset field, Exception? error) =>
        FieldEdited?.Invoke(this, new FieldEditedEventArgs(field, error));

    // Values are user data: ImGui's Text and SetTooltip take printf formats, so a '%' in a value would be read as one.
    internal static void Text(string text, Vector4? color = null)
    {
        if (color is { } c)
            Gui.PushStyleColor(ImGuiCol.Text, c);

        Gui.PushTextWrapPos(0f);
        Gui.TextUnformatted(text);
        Gui.PopTextWrapPos();

        if (color != null)
            Gui.PopStyleColor();
    }

    private void DrawNodes(Inspector parent)
    {
        foreach (var node in parent.Nodes)
        {
            Gui.PushID(node.Name);
            Gui.TableNextRow();
            Gui.TableSetColumnIndex(0);
            // Lines the label up with the framed editor in the next column.
            Gui.AlignTextToFramePadding();

            switch (node)
            {
                case Inspector nested:
                    if (PendingFocus != null && IsAncestor(nested, PendingFocus))
                        Gui.SetNextItemOpen(true);

                    var open = Gui.TreeNodeEx(nested.Name, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanFullWidth);
                    Tooltip(nested);

                    Gui.TableSetColumnIndex(1);
                    Text(HeaderText(nested), DisabledColor);

                    if (open)
                    {
                        DrawNodes(nested);
                        Gui.TreePop();
                    }
                    break;

                case Fieldset field:
                    Gui.TreeNodeEx(field.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
                    Tooltip(field);

                    Gui.TableSetColumnIndex(1);

                    var focus = PendingFocus == field;

                    if (focus)
                        PendingFocus = null;

                    Fields[field].Draw(focus);
                    break;
            }

            Gui.PopID();
        }
    }

    private static void Tooltip(InspectorNode node)
    {
        if (node.Descriptor is not { } descriptor || !Gui.IsItemHovered())
            return;

        Gui.BeginTooltip();
        Gui.TextUnformatted($"{ValueText.TypeName(descriptor.Type)}  {descriptor.FullPath}");
        Gui.EndTooltip();
    }

    private static bool IsAncestor(Inspector inspector, InspectorNode node)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent == inspector)
                return true;
        }

        return false;
    }

    private static string HeaderText(Inspector node)
    {
        var type = node.Descriptor is { } descriptor ? ValueText.TypeName(descriptor.Type) : string.Empty;

        try
        {
            return node.GetValue() == null ? $"{type} = null" : type;
        }
        catch (Exception ex)
        {
            return $"{type} <error: {ex.Message}>";
        }
    }
}
