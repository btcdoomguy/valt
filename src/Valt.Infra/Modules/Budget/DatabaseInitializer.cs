using System.Drawing;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Infra.DataAccess.Migrations;

namespace Valt.Infra.Modules.Budget;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly MigrationManager _migrationManager;

    public DatabaseInitializer(IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        MigrationManager migrationManager)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _migrationManager = migrationManager;
    }

    public async Task InitializeAsync()
    {
        var fiatAccount = FiatAccount.New("Bank Account", true, new Icon("MaterialSymbolsOutlined", "account_balanced", '\ue84f', Color.FromArgb(-16731500)), FiatCurrency.Brl, FiatValue.Empty);
        await _accountRepository.SaveAccountAsync(fiatAccount);
        
        var btcAccount = BtcAccount.New("Cold Storage", true, new Icon("MaterialSymbolsOutlined", "currency_bitcoin", '\uebc5', Color.FromArgb(-33532)), BtcValue.Empty);
        await _accountRepository.SaveAccountAsync(btcAccount);

        var foodCategory = Category.New("Food", new Icon("MaterialSymbolsOutlined", "fastfood", '\ue57a', Color.FromArgb(-3997614)));
        await _categoryRepository.SaveCategoryAsync(foodCategory);
        
        var billCategory = Category.New("Bills", new Icon("MaterialSymbolsOutlined", "currency_exchange", '\ueb70', Color.FromArgb(-18176)));
        await _categoryRepository.SaveCategoryAsync(billCategory);
        
        var healthCategory = Category.New("Health", new Icon("MaterialSymbolsOutlined", "health_and_safety", '\ue1d5', Color.FromArgb(-16730173)));
        await _categoryRepository.SaveCategoryAsync(healthCategory);
        
        var transportCategory = Category.New("Transport", new Icon("MaterialSymbolsOutlined", "directions_car", '\ue531', Color.FromArgb(-9155159)));
        await _categoryRepository.SaveCategoryAsync(transportCategory);
        
        var travelCategory = Category.New("Travel", new Icon("MaterialSymbolsOutlined", "airlines", '\uE7CA', Color.FromArgb(-18176)));
        await _categoryRepository.SaveCategoryAsync(travelCategory);
    }

    public async Task MigrateAsync()
    {
        await _migrationManager.RunMigrationsAsync();
    }
}