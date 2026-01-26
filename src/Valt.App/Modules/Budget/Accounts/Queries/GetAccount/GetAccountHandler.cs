using LiteDB;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.App.Modules.Budget.Accounts.Queries.GetAccount;

internal sealed class GetAccountHandler : IQueryHandler<GetAccountQuery, AccountDTO?>
{
    private readonly ILocalDatabase _localDatabase;

    public GetAccountHandler(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<AccountDTO?> HandleAsync(GetAccountQuery query, CancellationToken ct = default)
    {
        var account = _localDatabase.GetAccounts()
            .FindById(new ObjectId(query.AccountId));

        if (account is null)
            return Task.FromResult<AccountDTO?>(null);

        var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
        var isBtc = account.AccountEntityType == AccountEntityType.Bitcoin;

        var dto = new AccountDTO(
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

        return Task.FromResult<AccountDTO?>(dto);
    }
}
