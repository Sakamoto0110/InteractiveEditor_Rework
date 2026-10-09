using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PixieLib;

namespace InteractiveEditor.Avalonia;

// A swatch painted with the color, with its code (P7.9). Avalonia has no color dialog of its own (the color
// picker is a package apart), so a click opens a flyout with a slider for each channel, alpha included,
// and the code to type (#RRGGBB, #AARRGGBB or a color's name); the sliders write while they are dragged,
// and the code on Enter or when it loses the focus (P2.12). The member can hold an Avalonia Color, a
// System.Drawing.Color, a PxColorRgba or a PxColorHsl.
internal sealed class ColorEditor : AvaloniaEditor
{
    private readonly Button Swatch = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch,
        Padding = new Thickness(3),
    };

    // The color goes on a border inside the button: the theme paints the button's own background when the
    // mouse is over it.
    private readonly Border Fill = new() { CornerRadius = new CornerRadius(2) };
    private readonly TextBlock Caption = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly Slider Alpha = Channel();
    private readonly Slider Red = Channel();
    private readonly Slider Green = Channel();
    private readonly Slider Blue = Channel();
    private readonly TextBox Code = new() { Margin = new Thickness(0, 8, 0, 0) };

    // The code of the color shown last.
    private string Shown = string.Empty;

    // Set while the view moves the sliders, so only the user's move writes.
    private bool Showing;

    public ColorEditor(AvaloniaRow row) : base(row)
    {
        Fill.Child = Caption;
        Swatch.Content = Fill;

        var panel = new Grid { Width = 260, ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto") };
        (string Name, Slider Slider)[] channels = [("A", Alpha), ("R", Red), ("G", Green), ("B", Blue)];

        for (var i = 0; i < channels.Length; i++)
        {
            var name = new TextBlock { Text = channels[i].Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            Grid.SetRow(name, i);
            Grid.SetRow(channels[i].Slider, i);
            Grid.SetColumn(channels[i].Slider, 1);
            panel.Children.Add(name);
            panel.Children.Add(channels[i].Slider);
            channels[i].Slider.ValueChanged += OnChannelChanged;
        }

        Grid.SetRow(Code, channels.Length);
        Grid.SetColumnSpan(Code, 2);
        panel.Children.Add(Code);
        Code.KeyDown += OnCodeKeyDown;
        Code.LostFocus += (_, _) => CommitCode();

        Swatch.Flyout = new Flyout { Content = panel };
    }

    public override Control Control => Swatch;

    // The theme shows no error under it.
    public override bool ShowsErrors => false;

    // Objects that hold different colors show none, with a dash (P7.19); a color picked goes to all.
    public override void ShowValue()
    {
        Showing = true;

        try
        {
            if (Row.Mixed || ToColor(Node.ViewValue) is not { } color)
            {
                Fill.Background = null;
                Caption.ClearValue(TextBlock.ForegroundProperty);
                Caption.Text = Row.Mixed ? "—" : string.Empty;
                Shown = string.Empty;
                return;
            }

            Shown = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            Fill.Background = new SolidColorBrush(color);
            Caption.Foreground = Brightness(color) < 0.5 ? Brushes.White : Brushes.Black;
            Caption.Text = Shown;
            Alpha.Value = color.A;
            Red.Value = color.R;
            Green.Value = color.G;
            Blue.Value = color.B;

            if (!Code.IsKeyboardFocusWithin)
                Code.Text = Shown;
        }
        finally
        {
            Showing = false;
        }
    }

    private static Slider Channel()
    {
        return new Slider { Minimum = 0, Maximum = 255, TickFrequency = 1, IsSnapToTickEnabled = true, SmallChange = 1, LargeChange = 16 };
    }

    private static Color? ToColor(object? value) => value switch
    {
        Color color => color,
        System.Drawing.Color color => Color.FromArgb(color.A, color.R, color.G, color.B),
        PxColorRgba rgba => rgba.ToAvalonia(),
        PxColorHsl hsl => hsl.ToAvalonia(),
        _ => null,
    };

    // The lightness, as System.Drawing.Color.GetBrightness gives it, so the code reads over any color.
    private static double Brightness(Color color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));
        return (max + min) / 510.0;
    }

    // The color picked, as the type of the member.
    private object FromColor(Color color)
    {
        var type = Node.ValueType is { } held ? Nullable.GetUnderlyingType(held) ?? held : typeof(Color);

        if (type == typeof(PxColorRgba))
            return color.ToPrimitive();

        if (type == typeof(PxColorHsl))
            return PxColorHsl.FromRgba(color.ToPrimitive());

        if (type == typeof(System.Drawing.Color))
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        return color;
    }

    private void Pick(Color color)
    {
        Row.Write(() => Node.SetValue(FromColor(color)));
        Row.Show();
    }

    private void OnChannelChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (Showing)
            return;

        Pick(Color.FromArgb((byte)Alpha.Value, (byte)Red.Value, (byte)Green.Value, (byte)Blue.Value));
    }

    private void OnCodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitCode();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Code.Text = Shown;
            e.Handled = true;
        }
    }

    // A code that is not a color is refused on the row, and the member keeps its value.
    private void CommitCode()
    {
        var typed = (Code.Text ?? string.Empty).Trim();

        if (typed == Shown)
            return;

        if (Color.TryParse(typed, out var color))
            Pick(color);
        else
            Row.Refuse($"'{typed}' is not a color; type #RRGGBB, #AARRGGBB or a color's name.");
    }
}
