using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Valt.UI.Converters;

public class CanExecuteToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool canExecute)
        {
            return canExecute; // Directly return the bool for IsVisible
        }
        return false; // Default to hidden if the value is invalid
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}