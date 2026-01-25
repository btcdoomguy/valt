using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.App.Modules.Budget.Accounts.Queries;

internal sealed class GetAccountSummariesHandler : IQueryHandler<GetAccountSummariesQuery, AccountSummariesDTO>
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IAccountTotalsCalculator _accountTotalsCalculator;

    public GetAccountSummariesHandler(
        ILocalDatabase localDatabase,
        IAccountTotalsCalculator accountTotalsCalculator)
    {
        _localDatabase = localDatabase;
        _accountTotalsCalculator = accountTotalsCalculator;
    }

    public async Task<AccountSummariesDTO> HandleAsync(GetAccountSummariesQuery query, CancellationToken ct = default)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().OrderBy(x => x.DisplayOrder);
        var groups = _localDatabase.GetAccountGroups().FindAll().ToDictionary(g => g.Id, g => g.Name);

        var summaryTasks = accounts
            .Where(account => account.Visible || query.ShowHiddenAccounts)
            .Select(account => CreateAccountSummaryAsync(account, groups))
            .ToList();

        var dtos = await Task.WhenAll(summaryTasks);
        return new AccountSummariesDTO(dtos.ToList());
    }

    private async Task<AccountSummaryDTO> CreateAccountSummaryAsync(
        AccountEntity account,
        Dictionary<LiteDB.ObjectId, string> groups)
    {
        decimal? fiatTotal = null;
        decimal? futureFiatTotal = null;
        long? satsTotal = null;
        long? futureSatsTotal = null;
        bool hasFuture;

        if (account.AccountEntityType == AccountEntityType.Fiat)
        {
            var total = await _accountTotalsCalculator.CalculateFiatTotalAsync(account.Id.ToString());
            fiatTotal = total.CurrentFiatTotal;
            futureFiatTotal = total.FiatTotal;
            hasFuture = total.FiatTotal != total.CurrentFiatTotal;
        }
        else
        {
            var total = await _accountTotalsCalculator.CalculateBtcTotalAsync(account.Id.ToString());
            satsTotal = total.CurrentSatsTotal;
            futureSatsTotal = total.SatsTotal;
            hasFuture = total.SatsTotal != total.CurrentSatsTotal;
        }

        var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
        var currencyDisplayName = !string.IsNullOrEmpty(account.CurrencyNickname)
            ? account.CurrencyNickname
            : account.AccountEntityType == AccountEntityType.Bitcoin ? "BTC" : account.Currency;

        string? groupId = account.GroupId?.ToString();
        string? groupName = account.GroupId != null && groups.TryGetValue(account.GroupId, out var name) ? name : null;

        return new AccountSummaryDTO(
            account.Id.ToString(),
            account.AccountEntityType.ToString(),
            account.Name,
            account.Visible,
            account.Icon,
            icon.Unicode,
            icon.Color,
            account.Currency,
            currencyDisplayName,
            account.AccountEntityType == AccountEntityType.Bitcoin,
            fiatTotal,
            satsTotal,
            hasFuture,
            futureFiatTotal,
            futureSatsTotal,
            groupId,
            groupName);
    }
}
