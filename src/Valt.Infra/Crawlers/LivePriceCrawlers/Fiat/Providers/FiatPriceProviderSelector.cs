using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

/// <summary>
/// Coordinates multiple fiat price providers, using Frankfurter as the primary provider
/// and falling back to secondary providers for currencies not supported by Frankfurter.
/// </summary>
public class FiatPriceProviderSelector : IFiatPriceProviderSelector
{
    private readonly IEnumerable<IFiatPriceProvider> _providers;
    private readonly IClock _clock;
    private readonly ILogger<FiatPriceProviderSelector> _logger;

    private const string PrimaryProviderName = "Frankfurter";

    public FiatPriceProviderSelector(
        IEnumerable<IFiatPriceProvider> providers,
        IClock clock,
        ILogger<FiatPriceProviderSelector> logger)
    {
        _providers = providers;
        _clock = clock;
        _logger = logger;
    }

    public async Task<FiatUsdPrice> GetAsync(IEnumerable<FiatCurrency> currencies)
    {
        var currencyList = currencies.ToList();
        if (currencyList.Count == 0)
        {
            return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), true,
                new[] { new FiatUsdPrice.Item(FiatCurrency.Usd, 1) });
        }

        var providerAssignments = AssignCurrenciesToProviders(currencyList);

        if (providerAssignments.Count == 0)
        {
            _logger.LogWarning("No providers available for requested currencies: {Currencies}",
                string.Join(", ", currencyList.Select(c => c.Code)));
            return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), false,
                new[] { new FiatUsdPrice.Item(FiatCurrency.Usd, 1) });
        }

        _logger.LogInformation("Fetching prices from {ProviderCount} provider(s): {Providers}",
            providerAssignments.Count,
            string.Join(", ", providerAssignments.Select(p => $"{p.Provider.Name} ({p.Currencies.Count} currencies)")));

        var tasks = providerAssignments
            .Select(async assignment =>
            {
                try
                {
                    _logger.LogDebug("Requesting {Currencies} from {Provider}",
                        string.Join(", ", assignment.Currencies.Select(c => c.Code)),
                        assignment.Provider.Name);

                    return await assignment.Provider.GetAsync(assignment.Currencies);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching prices from {Provider}", assignment.Provider.Name);
                    return null;
                }
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        return MergeResults(results, currencyList);
    }

    /// <summary>
    /// Assigns currencies to providers based on support and priority.
    /// Frankfurter is the primary provider; other providers handle currencies Frankfurter doesn't support.
    /// </summary>
    internal List<ProviderAssignment> AssignCurrenciesToProviders(IReadOnlyList<FiatCurrency> currencies)
    {
        var assignments = new List<ProviderAssignment>();
        var remainingCurrencies = new HashSet<FiatCurrency>(currencies);

        // First, assign to primary provider (Frankfurter)
        var primaryProvider = _providers.FirstOrDefault(p => p.Name == PrimaryProviderName);
        if (primaryProvider != null)
        {
            var primaryCurrencies = remainingCurrencies
                .Where(c => primaryProvider.SupportedCurrencies.Contains(c))
                .ToList();

            if (primaryCurrencies.Count > 0)
            {
                assignments.Add(new ProviderAssignment(primaryProvider, primaryCurrencies));
                foreach (var c in primaryCurrencies)
                    remainingCurrencies.Remove(c);
            }
        }

        // Assign remaining currencies to secondary providers
        if (remainingCurrencies.Count > 0)
        {
            var secondaryProviders = _providers
                .Where(p => p.Name != PrimaryProviderName)
                .ToList();

            foreach (var provider in secondaryProviders)
            {
                if (remainingCurrencies.Count == 0)
                    break;

                var supportedCurrencies = remainingCurrencies
                    .Where(c => provider.SupportedCurrencies.Contains(c))
                    .ToList();

                if (supportedCurrencies.Count > 0)
                {
                    assignments.Add(new ProviderAssignment(provider, supportedCurrencies));
                    foreach (var c in supportedCurrencies)
                        remainingCurrencies.Remove(c);
                }
            }
        }

        if (remainingCurrencies.Count > 0)
        {
            _logger.LogWarning("No provider found for currencies: {Currencies}",
                string.Join(", ", remainingCurrencies.Select(c => c.Code)));
        }

        return assignments;
    }

    private FiatUsdPrice MergeResults(FiatUsdPrice?[] results, IReadOnlyList<FiatCurrency> requestedCurrencies)
    {
        var allItems = new Dictionary<FiatCurrency, FiatUsdPrice.Item>();
        var isUpToDate = true;
        var latestUtc = DateTime.MinValue;

        // Always include USD
        allItems[FiatCurrency.Usd] = new FiatUsdPrice.Item(FiatCurrency.Usd, 1);

        foreach (var result in results)
        {
            if (result == null)
            {
                isUpToDate = false;
                continue;
            }

            if (!result.UpToDate)
                isUpToDate = false;

            if (result.Utc > latestUtc)
                latestUtc = result.Utc;

            foreach (var item in result.Items)
            {
                // Don't override existing items (primary provider has priority)
                if (!allItems.ContainsKey(item.Currency))
                {
                    allItems[item.Currency] = item;
                }
            }
        }

        // Check if we got all requested currencies
        foreach (var currency in requestedCurrencies)
        {
            if (!allItems.ContainsKey(currency))
            {
                _logger.LogWarning("Currency {Currency} was not returned by any provider", currency.Code);
                isUpToDate = false;
            }
        }

        return new FiatUsdPrice(
            latestUtc == DateTime.MinValue ? _clock.GetCurrentDateTimeUtc() : latestUtc,
            isUpToDate,
            allItems.Values);
    }

    internal record ProviderAssignment(IFiatPriceProvider Provider, List<FiatCurrency> Currencies);
}
