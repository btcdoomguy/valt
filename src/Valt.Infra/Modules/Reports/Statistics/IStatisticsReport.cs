using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.Statistics;

public interface IStatisticsReport
{
    /// <summary>
    /// Gets statistics data including median expenses and wealth coverage
    /// </summary>
    /// <param name="currency">The fiat currency to use for calculations</param>
    /// <param name="currentWealthInFiat">The current total wealth in the specified fiat currency</param>
    /// <param name="provider">The report data provider</param>
    /// <returns>Statistics data</returns>
    Task<StatisticsData> GetAsync(FiatCurrency currency, decimal currentWealthInFiat, IReportDataProvider provider);
}
