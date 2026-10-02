using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.Views.Wpf;

// A button painted with the color, which opens the system's color dialog (P7.9). WPF has none of its
// own, so it is the one the WinForms view opens, with the view's window as its owner. The member can
// hold a System.Drawing.Color, a WPF Color, a PxColorArgb or a PxColorHsl; the dialog has no alpha, so
// the alpha stays. The background is the value here, so a failure shows on the border instead (P7.11).
internal sealed class ColorEditor : WpfEditor
{
    private static readonly SolidColorBrush FailedBorder = WpfInspectorView.Frozen(Colors.Red);

    private readonly Button Swatch = new();
    private readonly TextBlock Caption = new();

    public ColorEditor(WpfRow row) : base(row)
    {
        Swatch.Content = Caption;
        Swatch.Click += OnClick;
    }

    public override FrameworkElement Control => Swatch;

    // Objects that hold different colors show none, with a dash (P7.19); a color picked goes to all.
    public override void ShowValue()
    {
        if (Row.Mixed)
        {
            Swatch.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
            Caption.Foreground = SystemColors.GrayTextBrush;
            Caption.Text = "—";
            return;
        }

        if (ToColor(Node.ViewValue) is not { } color)
        {
            Swatch.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
            Caption.ClearValue(TextBlock.ForegroundProperty);
            Caption.Text = string.Empty;
            return;
        }

        Swatch.Background = WpfInspectorView.Frozen(color);
        Caption.Foreground = Brightness(color) < 0.5 ? Brushes.White : Brushes.Black;
        Caption.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Swatch.IsEnabled = enabled && !readOnly;

        if (failed)
        {
            Swatch.BorderBrush = FailedBorder;
            Swatch.BorderThickness = new Thickness(2);
        }
        else
        {
            Swatch.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
            Swatch.ClearValue(System.Windows.Controls.Control.BorderThicknessProperty);
        }
    }

    private static Color? ToColor(object? value) => value switch
    {
        Color color => color,
        System.Drawing.Color color => Color.FromArgb(color.A, color.R, color.G, color.B),
        PxColorArgb argb => argb,
        PxColorHsl hsl => PxColorHsl.ToArgb(hsl),
        _ => null,
    };

    // The lightness, as System.Drawing.Color.GetBrightness gives it, so the text reads the same in both
    // views.
    private static double Brightness(Color color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));
        return (max + min) / 510.0;
    }

    // The color picked, as the type of the member.
    private object FromColor(Color color)
    {
        var type = Nullable.GetUnderlyingType(Node.ValueType ?? typeof(Color)) ?? Node.ValueType;

        if (type == typeof(PxColorArgb))
            return (PxColorArgb)color;

        if (type == typeof(PxColorHsl))
            return PxColorHsl.FromArgb(color);

        if (type == typeof(System.Drawing.Color))
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        return color;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        var current = ToColor(Node.ViewValue);
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };

        if (current is { } shown)
            dialog.Color = System.Drawing.Color.FromArgb(shown.R, shown.G, shown.B);

        var window = Window.GetWindow(Swatch);
        var owner = window == null ? null : new Owner(new WindowInteropHelper(window).Handle);

        if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
            return;

        var picked = Color.FromArgb(current?.A ?? 255, dialog.Color.R, dialog.Color.G, dialog.Color.B);
        Row.Write(() => Node.SetValue(FromColor(picked)));
        Row.Show();
    }

    // The WPF window as the owner of a WinForms dialog.
    private sealed class Owner(IntPtr handle) : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
