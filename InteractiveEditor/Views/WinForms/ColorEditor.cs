using System.Drawing;
using System.Windows.Forms;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.Views.WinForms;

// A button painted with the color, which opens the system's ColorDialog (P7.9). The member can hold a
// System.Drawing.Color, a PxColorArgb or a PxColorHsl; the dialog has no alpha, so the alpha stays. The
// background is the value here, so a failure shows on the border instead (P7.11).
internal sealed class ColorEditor : WinFormsEditor
{
    private readonly Button Swatch = new() { FlatStyle = FlatStyle.Flat, UseMnemonic = false };

    public ColorEditor(WinFormsRow row) : base(row)
    {
        Swatch.Click += OnClick;
    }

    public override Control Control => Swatch;

    public override void ShowValue()
    {
        var color = ToColor(Node.ViewValue);
        Swatch.BackColor = color ?? SystemColors.Control;
        Swatch.ForeColor = color is { } c && c.GetBrightness() < 0.5 ? Color.White : Color.Black;
        Swatch.Text = color is { } shown ? $"#{shown.A:X2}{shown.R:X2}{shown.G:X2}{shown.B:X2}" : string.Empty;
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Swatch.Enabled = enabled && !readOnly;
        Swatch.FlatAppearance.BorderColor = failed ? Color.Red : SystemColors.ControlDark;
        Swatch.FlatAppearance.BorderSize = failed ? 2 : 1;
    }

    private static Color? ToColor(object? value) => value switch
    {
        Color color => color,
        PxColorArgb argb => (Color)argb,
        PxColorHsl hsl => (Color)PxColorHsl.ToArgb(hsl),
        _ => null,
    };

    // The color picked, as the type of the member.
    private object FromColor(Color color)
    {
        var type = Nullable.GetUnderlyingType(Node.ValueType ?? typeof(Color)) ?? Node.ValueType;

        if (type == typeof(PxColorArgb))
            return (PxColorArgb)color;

        if (type == typeof(PxColorHsl))
            return PxColorHsl.FromArgb(color);

        return color;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        var current = ToColor(Node.ViewValue);
        using var dialog = new ColorDialog { Color = current ?? Color.Black, FullOpen = true };

        if (dialog.ShowDialog(Swatch.FindForm()) != DialogResult.OK)
            return;

        var picked = Color.FromArgb(current?.A ?? 255, dialog.Color);
        Row.Write(() => Node.SetValue(FromColor(picked)));
        ShowValue();
    }
}
