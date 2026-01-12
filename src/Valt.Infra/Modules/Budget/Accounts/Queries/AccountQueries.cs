using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

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

    public async Task<AccountSummariesDTO> GetAccountSummariesAsync(bool showHiddenAccounts)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().OrderBy(x => x.DisplayOrder);
        var summaryTasks = (from account in accounts where account.Visible || showHiddenAccounts select CreateAccountSummaryAsync(account))
            .ToList();

        var dtos = await Task.WhenAll(summaryTasks);
        return new AccountSummariesDTO(dtos.ToList());
    }

    public Task<IEnumerable<AccountDTO>> GetAccountsAsync(bool showHiddenAccounts)
    {
        var accounts = _localDatabase.GetAccounts().FindAll().Where(account => account.Visible || showHiddenAccounts).OrderBy(x => x.DisplayOrder);

        return Task.FromResult(accounts.Select(account =>
        {
            var icon = account.Icon != null ? Icon.RestoreFromId(account.Icon) : Icon.Empty;
            
            return new AccountDTO(account.Id.ToString(),
                account.AccountEntityType.ToString(),
                account.Name, account.Visible, account.Icon, icon.Unicode, icon.Color, account.Currency,
                account.AccountEntityType == AccountEntityType.Bitcoin);
        }));
    }

    private async Task<AccountSummaryDTO> CreateAccountSummaryAsync(AccountEntity account)
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
            futureSatsTotal);
    }
}