using LiteDB;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.Budget.Accounts.Queries;

public class AccountQueries : IAccountQueries
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IAccountTotalsCalculator _accountTotalsCalculator;

    public AccountQueries(ILocalDatabase localDatabase,
        IAccountTotalsCalculator accountTotalsCalculator)
    {
        _localDatabase = localDatabase;
        _accountTotalsCalculator = accountTotalsCalculator;
    }

    public Task<AccountDTO?> GetAccountAsync(string accountId)
    {
        var account = _localDatabase.GetAccounts().FindById(new ObjectId(accountId));

        if (account is null)
            return Task.FromResult<AccountDTO?>(null);

        var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
        var isBtc = account.AccountEntityType == AccountEntityType.Bitcoin;

        var dto = new AccountDTO(
            account.Id.ToString(),
            account.AccountEntityType.ToString(),
            account.Name,
            account.CurrencyNickname ?? string.Empty,
            account.Visible,
            account.Icon,
            icon.Unicode,
            icon.Color,
            account.Currency,
            isBtc,
            InitialAmountFiat: isBtc ? null : account.InitialAmount,
            InitialAmountSats: isBtc ? Convert.ToInt64(account.InitialAmount) : null,
            GroupId: account.GroupId?.ToString());

        return Task.FromResult<AccountDTO?>(dto);
    }

    public async Task<AccountSummariesDTO> GetAccountSummariesAsync(bool showHiddenAccounts)
    {
        var accounts = (showHiddenAccounts
            ? _localDatabase.GetAccounts().FindAll().OrderBy(x => x.DisplayOrder)
            : _localDatabase.GetAccounts().Find(x => x.Visible).OrderBy(x => x.DisplayOrder)).ToList();
        var groups = _localDatabase.GetAccountGroups().FindAll().ToDictionary(g => g.Id, g => g.Name);
        var caches = _localDatabase.GetAccountCaches().FindAll().ToDictionary(c => c.Id);

        var summaryTasks = accounts.Select(account => CreateAccountSummaryAsync(account, groups, caches)).ToList();

        var dtos = await Task.WhenAll(summaryTasks);
        return new AccountSummariesDTO(dtos.ToList());
    }

    public Task<IEnumerable<AccountDTO>> GetAccountsAsync(bool showHiddenAccounts)
    {
        var accounts = showHiddenAccounts
            ? _localDatabase.GetAccounts().FindAll().OrderBy(x => x.DisplayOrder)
            : _localDatabase.GetAccounts().Find(x => x.Visible).OrderBy(x => x.DisplayOrder);

        return Task.FromResult(accounts.Select(account =>
        {
            var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
            var isBtc = account.AccountEntityType == AccountEntityType.Bitcoin;

            return new AccountDTO(
                account.Id.ToString(),
                account.AccountEntityType.ToString(),
                account.Name,
                account.CurrencyNickname ?? string.Empty,
                account.Visible,
                account.Icon,
                icon.Unicode,
                icon.Color,
                account.Currency,
                isBtc,
                InitialAmountFiat: isBtc ? null : account.InitialAmount,
                InitialAmountSats: isBtc ? Convert.ToInt64(account.InitialAmount) : null,
                GroupId: account.GroupId?.ToString());
        }));
    }

    public Task<IEnumerable<AccountGroupDTO>> GetAccountGroupsAsync()
    {
        var groups = _localDatabase.GetAccountGroups()
            .FindAll()
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new AccountGroupDTO(g.Id.ToString(), g.Name, g.DisplayOrder, g.TotalCurrency));

        return Task.FromResult(groups);
    }

    private async Task<AccountSummaryDTO> CreateAccountSummaryAsync(
        AccountEntity account,
        Dictionary<LiteDB.ObjectId, string> groups,
        Dictionary<LiteDB.ObjectId, AccountCacheEntity> caches)
    {
        decimal? fiatTotal = null;
        decimal? futureFiatTotal = null;
        long? satsTotal = null;
        long? futureSatsTotal = null;
        bool hasFuture;

        var hasCache = caches.TryGetValue(account.Id, out var cache);

        if (account.AccountEntityType == AccountEntityType.Fiat)
        {
            if (hasCache && cache is not null)
            {
                fiatTotal = cache.CurrentTotal;
                futureFiatTotal = cache.Total;
                hasFuture = cache.Total != cache.CurrentTotal;
            }
            else
            {
                var total = await _accountTotalsCalculator.CalculateFiatTotalAsync(account.Id.ToString());
                fiatTotal = total.CurrentFiatTotal;
                futureFiatTotal = total.FiatTotal;
                hasFuture = total.FiatTotal != total.CurrentFiatTotal;
            }
        }
        else
        {
            if (hasCache && cache is not null)
            {
                var currentSats = Convert.ToInt64(cache.CurrentTotal);
                var totalSats = Convert.ToInt64(cache.Total);
                satsTotal = currentSats;
                futureSatsTotal = totalSats;
                hasFuture = totalSats != currentSats;
            }
            else
            {
                var total = await _accountTotalsCalculator.CalculateBtcTotalAsync(account.Id.ToString());
                satsTotal = total.CurrentSatsTotal;
                futureSatsTotal = total.SatsTotal;
                hasFuture = total.SatsTotal != total.CurrentSatsTotal;
            }
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