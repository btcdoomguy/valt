using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Valt.UI.Converters;

public class StringToColorBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            return new SolidColorBrush(Color.Parse(hexColor));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
