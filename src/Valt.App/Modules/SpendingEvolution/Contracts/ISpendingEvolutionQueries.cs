using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;

namespace Valt.App.Modules.SpendingEvolution.Contracts;

public interface ISpendingEvolutionQueries
{
    Task<SpendingEvolutionDataDto> GetSpendingEvolutionAsync(GetSpendingEvolutionQuery query);
}
