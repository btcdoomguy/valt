using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;

namespace Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenseNames;

internal sealed class GetFixedExpenseNamesHandler : IQueryHandler<GetFixedExpenseNamesQuery, FixedExpenseNamesDTO>
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;

    public GetFixedExpenseNamesHandler(IFixedExpenseQueries fixedExpenseQueries)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
    }

    public async Task<FixedExpenseNamesDTO> HandleAsync(GetFixedExpenseNamesQuery query, CancellationToken ct = default)
    {
        FixedExpenseId? currentId = string.IsNullOrWhiteSpace(query.CurrentFixedExpenseId)
            ? null
            : new FixedExpenseId(query.CurrentFixedExpenseId);

        var infraResult = await _fixedExpenseQueries.GetFixedExpenseNamesAsync(currentId);

        return new FixedExpenseNamesDTO(
            infraResult.Items.Select(i => new FixedExpenseNameDTO(i.Id, i.Name)).ToList());
    }
}
