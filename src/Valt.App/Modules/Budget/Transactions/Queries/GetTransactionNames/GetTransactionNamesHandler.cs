using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using InfraDTO = Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.App.Modules.Budget.Transactions.Queries.GetTransactionNames;

internal sealed class GetTransactionNamesHandler : IQueryHandler<GetTransactionNamesQuery, IReadOnlyList<TransactionNameSearchDTO>>
{
    private readonly ITransactionQueries _transactionQueries;

    public GetTransactionNamesHandler(ITransactionQueries transactionQueries)
    {
        _transactionQueries = transactionQueries;
    }

    public async Task<IReadOnlyList<TransactionNameSearchDTO>> HandleAsync(GetTransactionNamesQuery query, CancellationToken ct = default)
    {
        var infraResult = await _transactionQueries.GetTransactionNamesAsync(query.SearchTerm);

        return infraResult.Select(MapToAppDto).ToList();
    }

    private static TransactionNameSearchDTO MapToAppDto(InfraDTO.TransactionNameSearchDTO infra)
    {
        return new TransactionNameSearchDTO
        {
            Name = infra.Name,
            CategoryId = infra.CategoryId,
            CategoryName = infra.CategoryName,
            Count = infra.Count,
            IsBitcoin = infra.IsBitcoin,
            SatAmount = infra.SatAmount,
            FiatAmount = infra.FiatAmount
        };
    }
}
