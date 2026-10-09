using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using InteractiveEditor.Binding;
using InteractiveEditor.Presentation;

namespace InteractiveEditor.Avalonia;

public sealed class FieldEditedEventArgs(Fieldset field, Exception? error) : EventArgs
{
    public Fieldset Field { get; } = field;
    // Set when parsing or SetValue failed; the editor then shows the error and the current value again.
    public Exception? Error { get; } = error;
}

/// <summary>
/// Property-grid style view of an <see cref="InteractiveEditor.Inspector"/>: nested inspectors become expanders and
/// fieldsets become rows with an editor picked from the member type. The core raises no change notifications,
/// so values are pulled with <see cref="Refresh"/>, which also runs after every edit.
/// </summary>
public class InspectorView : UserControl, IInspectorView
{
    private readonly List<FieldsetView> Fields = [];
    private readonly List<(Inspector Node, TextBlock Header)> Headers = [];

    public InspectorView(Inspector inspector)
    {
        Inspector = inspector;

        var root = Build(inspector);
        // Room for the overlay scrollbar, which would otherwise cover the spinner buttons.
        root.Margin = new Thickness(0, 0, 16, 0);

        Content = new ScrollViewer { Content = root };
        Refresh();
    }

    public InspectorView(Inspector inspector, Control host)
        : this(inspector)
    {
        AttachToHost(host);
    }

    public Inspector Inspector { get; }

    public event EventHandler<FieldEditedEventArgs>? FieldEdited;

    public void AttachToHost(object host)
    {
        switch (host)
        {
            case ContentControl contentControl:
                contentControl.Content = this;
                break;
            case ContentPresenter presenter:
                presenter.Content = this;
                break;
            case Decorator decorator:
                decorator.Child = this;
                break;
            case Panel panel:
                panel.Children.Add(this);
                break;
            default:
                throw new ArgumentException(
                    $"Cannot attach to a {host.GetType().Name}; expected a ContentControl, ContentPresenter, Decorator or Panel.", nameof(host));
        }
    }

    // Re-reads every value; errors left by earlier edits are dropped, since they describe values no longer shown.
    public void Refresh() => RefreshAll(clearErrors: true);

    internal void OnFieldEdited(FieldsetView view, Exception? error)
    {
        // Other members can change too (struct write-back, setters with side effects), so everything is re-read.
        RefreshAll(clearErrors: false);
        view.Refresh(force: true);
        view.ShowError(error);

        FieldEdited?.Invoke(this, new FieldEditedEventArgs(view.Field, error));
    }

    private void RefreshAll(bool clearErrors)
    {
        foreach (var (node, header) in Headers)
            header.Text = HeaderText(node);

        foreach (var field in Fields)
            field.Refresh(clearError: clearErrors);
    }

    internal static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{TypeName(underlying)}?";

        var tick = type.Name.IndexOf('`');

        if (!type.IsGenericType || tick < 0)
            return type.Name;

        return $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    private Control Build(Inspector inspector)
    {
        var panel = new StackPanel { Spacing = 4 };

        foreach (var node in inspector.Nodes)
        {
            switch (node)
            {
                case Inspector nested:
                    var header = new TextBlock { FontWeight = FontWeight.SemiBold };
                    Headers.Add((nested, header));

                    panel.Children.Add(new Expander
                    {
                        Header = header,
                        IsExpanded = true,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = Build(nested)
                    });
                    break;

                case Fieldset fieldset:
                    var row = new FieldsetView(fieldset, this);
                    Fields.Add(row);
                    panel.Children.Add(row);
                    break;
            }
        }

        return panel;
    }

    private static string HeaderText(Inspector node)
    {
        var type = node.Descriptor is { } descriptor ? TypeName(descriptor.Type) : string.Empty;

        try
        {
            return node.GetValue() == null ? $"{node.Name} : {type} = null" : $"{node.Name} : {type}";
        }
        catch (Exception ex)
        {
            return $"{node.Name} : {type} <error: {ex.Message}>";
        }
    }
}
