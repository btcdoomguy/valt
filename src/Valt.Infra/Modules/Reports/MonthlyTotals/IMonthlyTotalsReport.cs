using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports;

public interface IMonthlyTotalsReport
{
    Task<MonthlyTotalsData> GetAsync(DateOnly baseDate, FiatCurrency currency);
}