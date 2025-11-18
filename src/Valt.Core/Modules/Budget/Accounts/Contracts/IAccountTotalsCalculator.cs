namespace Valt.Core.Modules.Budget.Accounts.Contracts;

public interface IAccountTotalsCalculator
{
    Task<CalculatedFiatTotals> CalculateFiatTotalAsync(AccountId accountId);
    Task<CalculatedBtcTotals> CalculateBtcTotalAsync(AccountId accountId);
}

public record CalculatedFiatTotals(decimal? FiatTotal, decimal? CurrentFiatTotal);

public record CalculatedBtcTotals(long? SatsTotal, long? CurrentSatsTotal);