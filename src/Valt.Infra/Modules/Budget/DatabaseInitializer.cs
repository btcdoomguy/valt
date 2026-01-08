using System.Drawing;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Infra.DataAccess.Migrations;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.Modules.Budget;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly MigrationManager _migrationManager;
    private readonly IInitialCategoryNameLanguageProvider _initialCategoryNameLanguageProvider;
    private readonly ConfigurationManager _configurationManager;

    public DatabaseInitializer(IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        MigrationManager migrationManager,
        IInitialCategoryNameLanguageProvider initialCategoryNameLanguageProvider,
        ConfigurationManager configurationManager)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _migrationManager = migrationManager;
        _initialCategoryNameLanguageProvider = initialCategoryNameLanguageProvider;
        _configurationManager = configurationManager;
    }

    public async Task InitializeAsync(string? initialDataLanguage = null, IEnumerable<string>? selectedCurrencies = null)
    {
        var fiatAccount = FiatAccount.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.FiatAccount, initialDataLanguage),
            true, new Icon("MaterialSymbolsOutlined", "account_balanced", '\ue84f', Color.FromArgb(-16731500)),
            FiatCurrency.Brl, FiatValue.Empty);
        await _accountRepository.SaveAccountAsync(fiatAccount);

        var btcAccount = BtcAccount.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.BtcAccount, initialDataLanguage), true,
            new Icon("MaterialSymbolsOutlined", "currency_bitcoin", '\uebc5', Color.FromArgb(-33532)), BtcValue.Empty);
        await _accountRepository.SaveAccountAsync(btcAccount);

        var categories = new List<Category>()
        {
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Food, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "fastfood", '\ue57a', Color.FromArgb(-3997614))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.UtilityBills, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "currency_exchange", '\ueb70', Color.FromArgb(-18176))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Health, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "health_and_safety", '\ue1d5', Color.FromArgb(-16730173))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Transport, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "directions_car", '\ue531', Color.FromArgb(-9155159))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Travel, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "airlines", '\uE7CA', Color.FromArgb(-18176))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Housing, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "house", '\uEA44', Color.FromArgb(-3997614))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Services, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "home_repair_service", '\uf100', Color.FromArgb(-8336444))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Gadgets, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "devices_other", '\uE337', Color.FromArgb(-3238952))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Entertainment, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "sports_volleyball", '\uEA31', Color.FromArgb(-141259))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Groceries, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "shopping_cart", '\uE8CC', Color.FromArgb(-291840))),
            Category.New(_initialCategoryNameLanguageProvider.Get(InitialCategoryNames.Paycheck, initialDataLanguage),
                new Icon("MaterialSymbolsOutlined", "attach_money", '\uE227', Color.FromArgb(-8604862))),
        };

        foreach (var category in categories)
            await _categoryRepository.SaveCategoryAsync(category);

        // Initialize available fiat currencies if provided
        if (selectedCurrencies is not null)
        {
            _configurationManager.SetAvailableFiatCurrencies(selectedCurrencies);
        }
    }

    public async Task MigrateAsync()
    {
        await _migrationManager.RunMigrationsAsync();
    }
}