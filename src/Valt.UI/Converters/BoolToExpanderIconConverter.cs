using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Valt.UI.Converters;

public class BoolToExpanderIconConverter : IValueConverter
{
    // Material Design icons: chevron_right (E5CC) and expand_more (E5CF)
    private const string ExpandedIcon = "\uE5CF";   // expand_more (down arrow)
    private const string CollapsedIcon = "\uE5CC"; // chevron_right (right arrow)

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? ExpandedIcon : CollapsedIcon;
        }
        return ExpandedIcon;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
