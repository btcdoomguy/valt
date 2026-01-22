using System;
using System.Globalization;
using Valt.Infra.Modules.Budget.Categories;
using Valt.UI.Lang;

namespace Valt.UI.Services;

public class InitialCategoryNameLanguageProvider : IInitialCategoryNameLanguageProvider
{
    public string Get(InitialCategoryNames categoryName, string? cultureCode = null)
    {
        var resourceKey = categoryName switch
        {
            InitialCategoryNames.BtcAccount => "BootDatabase.BtcAccount",
            InitialCategoryNames.FiatAccount => "BootDatabase.FiatAccount",
            InitialCategoryNames.Food => "BootDatabase.Category.Food",
            InitialCategoryNames.UtilityBills => "BootDatabase.Category.UtilityBills",
            InitialCategoryNames.Services => "BootDatabase.Category.Services",
            InitialCategoryNames.Gadgets => "BootDatabase.Category.Gadgets",
            InitialCategoryNames.Health => "BootDatabase.Category.Health",
            InitialCategoryNames.Transport => "BootDatabase.Category.Transport",
            InitialCategoryNames.Travel => "BootDatabase.Category.Travel",
            InitialCategoryNames.Entertainment => "BootDatabase.Category.Entertainment",
            InitialCategoryNames.Groceries => "BootDatabase.Category.Groceries",
            InitialCategoryNames.Paycheck => "BootDatabase.Category.Paycheck",
            InitialCategoryNames.Housing => "BootDatabase.Category.Housing",
            InitialCategoryNames.RegularAccountsGroup => "BootDatabase.AccountGroup.RegularAccounts",
            InitialCategoryNames.BitcoinGroup => "BootDatabase.AccountGroup.Bitcoin",
            _ => throw new ArgumentOutOfRangeException(nameof(categoryName), categoryName, null)
        };

        var culture = string.IsNullOrEmpty(cultureCode)
            ? null
            : CultureInfo.GetCultureInfo(cultureCode);

        return language.ResourceManager.GetString(resourceKey, culture) ?? resourceKey;
    }
}