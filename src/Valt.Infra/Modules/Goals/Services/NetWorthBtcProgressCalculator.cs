using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Goals.Queries.DTOs;
using Valt.Infra.Settings;

namespace Valt.Infra.Modules.Goals.Services;

internal class NetWorthBtcProgressCalculator : IGoalProgressCalculator
{
    private const decimal SatoshisPerBitcoin = 100_000_000m;

    private readonly ILocalDatabase _localDatabase;
    private readonly IPriceDatabase _priceDatabase;
    private readonly CurrencySettings _currencySettings;

    public GoalTypeNames SupportedType => GoalTypeNames.NetWorthBtc;

    public NetWorthBtcProgressCalculator(
        ILocalDatabase localDatabase,
        IPriceDatabase priceDatabase,
        CurrencySettings currencySettings)
    {
        _localDatabase = localDatabase;
        _priceDatabase = priceDatabase;
        _currencySettings = currencySettings;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeNetWorthBtc(input.GoalTypeJson);

        var accounts = _localDatabase.GetAccounts().FindAll().ToList();
        var accountCache = _localDatabase.GetAccountCaches().FindAll()
            .ToDictionary(x => x.Id);

        // Get latest BTC/USD price
        var latestBtcPrice = _priceDatabase.GetBitcoinData()
            .FindAll()
            .OrderByDescending(x => x.Date)
            .FirstOrDefault();

        var usdBtcPrice = latestBtcPrice?.Price ?? 0m;

        // Get latest fiat rates for currency conversion
        var latestFiatRates = _priceDatabase.GetFiatData()
            .FindAll()
            .GroupBy(x => x.Currency)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Date).First().Price);

        var totalSats = 0L;

        foreach (var account in accounts)
        {
            if (!account.Visible)
                continue;

            if (!accountCache.TryGetValue(account.Id, out var cache))
                continue;

            var balance = cache.CurrentTotal;

            if (account.AccountEntityType == AccountEntityType.Bitcoin)
            {
                // Balance is already in sats
                totalSats += (long)balance;
            }
            else if (account.AccountEntityType == AccountEntityType.Fiat && usdBtcPrice > 0)
            {
                // Convert fiat balance to USD, then to sats
                var accountCurrency = account.Currency ?? _currencySettings.MainFiatCurrency;
                var usdAmount = ConvertToUsd(balance, accountCurrency, latestFiatRates);
                var btcAmount = usdAmount / usdBtcPrice;
                totalSats += (long)(btcAmount * SatoshisPerBitcoin);
            }
        }

        var progress = config.TargetSats > 0
            ? Math.Min(100m, Math.Max(0m, (totalSats * 100m) / config.TargetSats))
            : 0m;

        var updatedGoalType = config.WithCalculatedSats(totalSats);

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }

    private static decimal ConvertToUsd(decimal amount, string currencyCode, Dictionary<string, decimal> fiatRates)
    {
        if (currencyCode == FiatCurrency.Usd.Code)
            return amount;

        if (fiatRates.TryGetValue(currencyCode, out var rateToUsd) && rateToUsd > 0)
            return amount / rateToUsd;

        return 0m;
    }
}
