using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Valt.UI.Views.Main.Tabs.Reports;

public static class DashboardDataConverters
{
    public static readonly IValueConverter CursorConverter = new BoolToCursorConverter();
    public static readonly IValueConverter UnderlineConverter = new BoolToUnderlineConverter();
    public static readonly IValueConverter BrushOrDefaultConverter = new BrushOrDefaultConverterImpl();

    private sealed class BoolToCursorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? new Cursor(StandardCursorType.Hand) : null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class BoolToUnderlineConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? TextDecorations.Underline : null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class BrushOrDefaultConverterImpl : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IBrush brush)
                return brush;

            if (Application.Current?.TryGetResource("Text100Brush", Avalonia.Styling.ThemeVariant.Default, out var resource) == true)
                return resource as IBrush;

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
