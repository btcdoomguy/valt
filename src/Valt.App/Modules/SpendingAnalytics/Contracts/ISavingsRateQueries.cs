using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.App.Modules.SpendingAnalytics.Queries;

namespace Valt.App.Modules.SpendingAnalytics.Contracts;

public interface ISavingsRateQueries
{
    Task<SavingsRateDataDto> GetSavingsRateAsync(GetSavingsRateQuery query);
}
