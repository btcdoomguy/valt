using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.Contracts;
using Valt.App.Modules.SpendingEvolution.DTOs;

namespace Valt.App.Modules.SpendingEvolution.Queries;

internal sealed class GetSpendingEvolutionHandler : IQueryHandler<GetSpendingEvolutionQuery, SpendingEvolutionDataDto>
{
    private readonly ISpendingEvolutionQueries _spendingEvolutionQueries;

    public GetSpendingEvolutionHandler(ISpendingEvolutionQueries spendingEvolutionQueries)
    {
        _spendingEvolutionQueries = spendingEvolutionQueries;
    }

    public Task<SpendingEvolutionDataDto> HandleAsync(GetSpendingEvolutionQuery query, CancellationToken ct = default)
    {
        return _spendingEvolutionQueries.GetSpendingEvolutionAsync(query);
    }
}
