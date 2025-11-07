using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Valt.UI.Converters;

public class SystemDrawingColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not System.Drawing.Color drawingColor) return null;
        var avaloniaColor = Avalonia.Media.Color.FromArgb(
            drawingColor.A,
            drawingColor.R,
            drawingColor.G,
            drawingColor.B
        );
        return new Avalonia.Media.SolidColorBrush(avaloniaColor);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Avalonia.Media.SolidColorBrush brush) return System.Drawing.Color.Black;

        var color = brush.Color; // Avalonia.Media.Color
        return System.Drawing.Color.FromArgb(
            color.A,
            color.R,
            color.G,
            color.B
        );
    }
}