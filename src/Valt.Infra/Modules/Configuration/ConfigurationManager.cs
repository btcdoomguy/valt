using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Configuration;

public class ConfigurationManager
{
    private readonly ILocalDatabase _localDatabase;

    public ConfigurationManager(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<int> GetMigrationVersionAsync()
    {
        var latestVersion = _localDatabase.GetConfiguration().
            FindOne(x => x.Key == ConfigurationKeys.MigrationVersion);

        if (latestVersion is null)
            return Task.FromResult(0);

        return Task.FromResult(int.Parse(latestVersion.Value));
    }

    public Task SetMigrationVersionAsync(int version)
    {
        var latestVersion = _localDatabase.GetConfiguration().
            FindOne(x => x.Key == ConfigurationKeys.MigrationVersion);

        if (latestVersion is null)
            latestVersion = new ConfigurationEntity() { Key = ConfigurationKeys.MigrationVersion };

        latestVersion.Value = version.ToString();

        _localDatabase.GetConfiguration().Upsert(latestVersion);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the list of available fiat currencies from configuration.
    /// If the record doesn't exist, it will be auto-populated from existing fiat accounts.
    /// </summary>
    public List<string> GetAvailableFiatCurrencies()
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.AvailableFiatCurrencies);

        if (config is null)
        {
            // Auto-populate from existing fiat accounts
            var currencies = InitializeFiatCurrenciesFromAccounts();
            return currencies;
        }

        if (string.IsNullOrWhiteSpace(config.Value))
            return new List<string>();

        return config.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Sets the list of available fiat currencies.
    /// </summary>
    public void SetAvailableFiatCurrencies(IEnumerable<string> currencies)
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.AvailableFiatCurrencies);

        if (config is null)
            config = new ConfigurationEntity { Key = ConfigurationKeys.AvailableFiatCurrencies };

        var distinctCurrencies = currencies
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct();

        config.Value = string.Join(",", distinctCurrencies);

        _localDatabase.GetConfiguration().Upsert(config);
    }

    /// <summary>
    /// Adds a fiat currency to the available currencies list if not already present.
    /// </summary>
    public void AddFiatCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return;

        var currencies = GetAvailableFiatCurrencies();
        var normalizedCode = currencyCode.Trim().ToUpperInvariant();

        if (!currencies.Contains(normalizedCode))
        {
            currencies.Add(normalizedCode);
            SetAvailableFiatCurrencies(currencies);
        }
    }

    /// <summary>
    /// Removes a fiat currency from the available currencies list.
    /// </summary>
    public void RemoveFiatCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return;

        var currencies = GetAvailableFiatCurrencies();
        var normalizedCode = currencyCode.Trim().ToUpperInvariant();

        if (currencies.Remove(normalizedCode))
        {
            SetAvailableFiatCurrencies(currencies);
        }
    }

    private List<string> InitializeFiatCurrenciesFromAccounts()
    {
        var accounts = _localDatabase.GetAccounts().FindAll();

        var currencies = accounts
            .Where(a => a.AccountEntityType == AccountEntityType.Fiat && !string.IsNullOrWhiteSpace(a.Currency))
            .Select(a => a.Currency!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        // Save the initialized list
        SetAvailableFiatCurrencies(currencies);

        return currencies;
    }
}