using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using InteractiveEditor.Binding;

namespace InteractiveEditor.Avalonia;

/// <summary>One row of the inspector: the member name and the editor bound to a <see cref="Fieldset"/>.</summary>
internal sealed class FieldsetView : Grid
{
    private readonly Fieldset Fieldset;
    private readonly InspectorView Owner;
    private readonly FieldEditor Editor;

    public FieldsetView(Fieldset fieldset, InspectorView owner)
    {
        var descriptor = fieldset.Descriptor
            ?? throw new ArgumentException($"Fieldset '{fieldset.Name}' has no descriptor.", nameof(fieldset));

        Fieldset = fieldset;
        Owner = owner;
        Editor = FieldEditor.For(descriptor.Type);

        ColumnDefinitions = new ColumnDefinitions("160,*");

        var name = new TextBlock { Text = descriptor.Name, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(name, $"{ValueText.TypeName(descriptor.Type)}  {descriptor.FullPath}");

        Editor.Control.IsEnabled = descriptor.Accessors.Getter != null && descriptor.Accessors.Setter != null;
        Editor.Edited += Commit;
        SetColumn(Editor.Control, 1);

        Children.Add(name);
        Children.Add(Editor.Control);
    }

    public Fieldset Field => Fieldset;

    // A control the user is typing into is left alone unless forced, or the refresh would overwrite the edit.
    public void Refresh(bool force = false, bool clearError = false)
    {
        if (!force && Editor.Control.IsKeyboardFocusWithin)
            return;

        try
        {
            Editor.Show(Fieldset.GetValue());

            if (clearError)
                ShowError(null);
        }
        catch (Exception ex)
        {
            ShowError(Unwrap(ex));
        }
    }

    // Only the message: the default error template prints an exception with its whole stack trace.
    public void ShowError(Exception? error)
    {
        if (error == null)
            DataValidationErrors.ClearErrors(Editor.Control);
        else
            DataValidationErrors.SetErrors(Editor.Control, [error.Message]);
    }

    private void Commit(Func<object?> produce)
    {
        try
        {
            Fieldset.SetValue(produce());
            Owner.OnFieldEdited(this, null);
        }
        catch (Exception ex)
        {
            Owner.OnFieldEdited(this, Unwrap(ex));
        }
    }

    private static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
}
