using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.MonthlyTotals;

public interface IMonthlyTotalsReport
{
    Task<MonthlyTotalsData> GetAsync(DateOnly baseDate, DateOnlyRange displayRange, FiatCurrency currency);
}