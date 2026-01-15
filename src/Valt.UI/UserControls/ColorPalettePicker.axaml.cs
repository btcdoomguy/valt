using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Valt.UI.UserControls;

public partial class ColorPalettePicker : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPalettePicker, Color>(nameof(SelectedColor), Colors.White);

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private static SolidColorBrush? _selectedBorderBrush;
    private static SolidColorBrush SelectedBorderBrush =>
        _selectedBorderBrush ??= Application.Current!.TryGetResource("ColorPickerSelectedBorderBrush", Avalonia.Styling.ThemeVariant.Default, out var brush)
            ? (SolidColorBrush)brush! : new SolidColorBrush(Color.Parse("#FFE98805"));

    private static readonly SolidColorBrush UnselectedBorderBrush = new(Colors.Transparent);

    private readonly List<Button> _colorButtons = [];

    public ColorPalettePicker()
    {
        InitializeComponent();
        CreateColorButtons();
        UpdatePreview();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedColorProperty)
        {
            UpdateSelection();
            UpdatePreview();
        }
    }

    private void CreateColorButtons()
    {
        // Material Design color palette
        var colors = new[]
        {
            // Row 1 - Reds, Pinks, Purples
            "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
            "#2196F3", "#03A9F4", "#00BCD4", "#009688", "#4CAF50",
            // Row 2 - Greens, Yellows, Oranges
            "#8BC34A", "#CDDC39", "#FFEB3B", "#FFC107", "#FF9800",
            "#FF5722", "#795548", "#9E9E9E", "#607D8B", "#000000",
            // Row 3 - Light variants
            "#FFCDD2", "#F8BBD0", "#E1BEE7", "#D1C4E9", "#C5CAE9",
            "#BBDEFB", "#B3E5FC", "#B2EBF2", "#B2DFDB", "#C8E6C9",
            // Row 4 - More light variants + White
            "#DCEDC8", "#F0F4C3", "#FFF9C4", "#FFECB3", "#FFE0B2",
            "#FFCCBC", "#D7CCC8", "#F5F5F5", "#CFD8DC", "#FFFFFF"
        };

        foreach (var hex in colors)
        {
            var color = Color.Parse(hex);
            var button = CreateColorButton(color);
            _colorButtons.Add(button);
            ColorPanel.Children.Add(button);
        }

        UpdateSelection();
    }

    private Button CreateColorButton(Color color)
    {
        var button = new Button
        {
            Width = 24,
            Height = 24,
            Margin = new Thickness(2),
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(color),
            BorderThickness = new Thickness(2),
            BorderBrush = UnselectedBorderBrush,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Tag = color
        };

        button.Click += (_, _) =>
        {
            SelectedColor = color;
            DropdownButton.Flyout?.Hide();
        };

        return button;
    }

    private void UpdateSelection()
    {
        foreach (var button in _colorButtons)
        {
            if (button.Tag is Color buttonColor)
            {
                var isSelected = ColorsMatch(buttonColor, SelectedColor);
                button.BorderBrush = isSelected ? SelectedBorderBrush : UnselectedBorderBrush;
            }
        }
    }

    private void UpdatePreview()
    {
        ColorPreview.Background = new SolidColorBrush(SelectedColor);
        ColorHexText.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
    }

    private static bool ColorsMatch(Color a, Color b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B;
    }
}
