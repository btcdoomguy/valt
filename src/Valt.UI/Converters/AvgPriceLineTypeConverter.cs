using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Valt.Core.Modules.AvgPrice;
using Valt.UI.Lang;

namespace Valt.UI.Converters;

public class AvgPriceLineTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int typeId)
            return null;

        var lineType = (AvgPriceLineTypes)typeId;
        return lineType switch
        {
            AvgPriceLineTypes.Buy => language.AvgPrice_LineType_Buy,
            AvgPriceLineTypes.Sell => language.AvgPrice_LineType_Sell,
            AvgPriceLineTypes.Setup => language.AvgPrice_LineType_Setup,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
