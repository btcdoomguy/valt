using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Configuration;

public class ConfigurationManager : IConfigurationManager
{
    private readonly ILocalDatabase _localDatabase;

    public ConfigurationManager(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    /// <summary>
    /// Gets whether the local database is currently open.
    /// </summary>
    public bool HasLocalDatabaseOpen => _localDatabase.HasDatabaseOpen;

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

    /// <summary>
    /// Gets the minimum assembly version required to open this database.
    /// Returns null if no minimum version has been set (legacy database or new database).
    /// </summary>
    public Version? GetMinimumAssemblyVersion()
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.MinimumAssemblyVersion);

        if (config is null || string.IsNullOrWhiteSpace(config.Value))
            return null;

        return Version.TryParse(config.Value, out var version) ? version : null;
    }

    /// <summary>
    /// Sets the minimum assembly version required to open this database.
    /// </summary>
    public void SetMinimumAssemblyVersion(Version version)
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.MinimumAssemblyVersion);

        if (config is null)
            config = new ConfigurationEntity { Key = ConfigurationKeys.MinimumAssemblyVersion };

        config.Value = version.ToString();
        _localDatabase.GetConfiguration().Upsert(config);
    }

    /// <summary>
    /// Gets the list of fiat currencies currently in use by accounts, fixed expenses, and avg price profiles.
    /// These currencies cannot be removed from the available currencies list.
    /// NOTE: This method should only be used for UI validation (preventing removal of in-use currencies)
    /// and for initial configuration seeding. Do NOT use this for determining which currencies to fetch
    /// prices for - use GetAvailableFiatCurrencies() instead.
    /// </summary>
    public List<string> GetCurrenciesInUse()
    {
        var currenciesInUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get currencies from fiat accounts
        var accounts = _localDatabase.GetAccounts().FindAll();
        foreach (var account in accounts)
        {
            if (account.AccountEntityType == AccountEntityType.Fiat && !string.IsNullOrWhiteSpace(account.Currency))
            {
                currenciesInUse.Add(account.Currency.Trim().ToUpperInvariant());
            }
        }

        // Get currencies from fixed expenses
        var fixedExpenses = _localDatabase.GetFixedExpenses().FindAll();
        foreach (var expense in fixedExpenses)
        {
            if (!string.IsNullOrWhiteSpace(expense.Currency))
            {
                currenciesInUse.Add(expense.Currency.Trim().ToUpperInvariant());
            }
        }

        // Get currencies from avg price profiles
        var avgPriceProfiles = _localDatabase.GetAvgPriceProfiles().FindAll();
        foreach (var profile in avgPriceProfiles)
        {
            if (!string.IsNullOrWhiteSpace(profile.Currency))
            {
                currenciesInUse.Add(profile.Currency.Trim().ToUpperInvariant());
            }
        }

        return currenciesInUse.ToList();
    }

    /// <summary>
    /// Gets the list of category IDs excluded from statistics calculations.
    /// Returns an empty list if no exclusions have been configured.
    /// </summary>
    public List<string> GetStatisticsExcludedCategoryIds()
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.StatisticsExcludedCategories);

        if (config is null || string.IsNullOrWhiteSpace(config.Value))
            return new List<string>();

        return config.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => id.Trim())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Sets the list of category IDs to exclude from statistics calculations.
    /// </summary>
    public void SetStatisticsExcludedCategoryIds(IEnumerable<string> categoryIds)
    {
        var config = _localDatabase.GetConfiguration()
            .FindOne(x => x.Key == ConfigurationKeys.StatisticsExcludedCategories);

        if (config is null)
            config = new ConfigurationEntity { Key = ConfigurationKeys.StatisticsExcludedCategories };

        var distinctIds = categoryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct();

        config.Value = string.Join(",", distinctIds);

        _localDatabase.GetConfiguration().Upsert(config);
    }
}