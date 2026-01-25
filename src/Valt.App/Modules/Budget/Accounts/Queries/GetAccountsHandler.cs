using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.App.Modules.Budget.Accounts.Queries;

internal sealed class GetAccountsHandler : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDTO>>
{
    private readonly ILocalDatabase _localDatabase;

    public GetAccountsHandler(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IReadOnlyList<AccountDTO>> HandleAsync(GetAccountsQuery query, CancellationToken ct = default)
    {
        var accounts = _localDatabase.GetAccounts()
            .FindAll()
            .Where(account => account.Visible || query.ShowHiddenAccounts)
            .OrderBy(x => x.DisplayOrder)
            .Select(account =>
            {
                var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
                var isBtc = account.AccountEntityType == AccountEntityType.Bitcoin;

                return new AccountDTO(
                    account.Id.ToString(),
                    account.AccountEntityType.ToString(),
                    account.Name,
                    account.Visible,
                    account.Icon,
                    icon.Unicode,
                    icon.Color,
                    account.Currency,
                    isBtc,
                    InitialAmountFiat: isBtc ? null : account.InitialAmount,
                    InitialAmountSats: isBtc ? Convert.ToInt64(account.InitialAmount) : null);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<AccountDTO>>(accounts);
    }
}
