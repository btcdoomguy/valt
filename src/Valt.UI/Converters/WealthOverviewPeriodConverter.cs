using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.UI.Lang;

namespace Valt.UI.Converters;

public class WealthOverviewPeriodConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WealthOverviewPeriod period)
            return null;

        return period switch
        {
            WealthOverviewPeriod.Daily => language.Reports_WealthOverview_Period_Daily,
            WealthOverviewPeriod.Weekly => language.Reports_WealthOverview_Period_Weekly,
            WealthOverviewPeriod.Monthly => language.Reports_WealthOverview_Period_Monthly,
            WealthOverviewPeriod.Yearly => language.Reports_WealthOverview_Period_Yearly,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
