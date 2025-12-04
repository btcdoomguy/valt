using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Valt.UI.Converters;

public class EnumEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Equals(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter : BindingOperations.DoNothing;
}