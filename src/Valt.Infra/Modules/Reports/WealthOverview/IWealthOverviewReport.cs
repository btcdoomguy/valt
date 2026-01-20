using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.WealthOverview;

public interface IWealthOverviewReport
{
    Task<WealthOverviewData> GetAsync(WealthOverviewPeriod period, FiatCurrency currency, IReportDataProvider provider);
}
