using System;
using Valt.Infra.Modules.Budget.Categories;
using Valt.UI.Lang;

namespace Valt.UI.Services;

public class InitialCategoryNameLanguageProvider : IInitialCategoryNameLanguageProvider
{
    public string Get(InitialCategoryNames categoryName)
    {
        return categoryName switch
        {
            InitialCategoryNames.BtcAccount => language.BootDatabase_BtcAccount,
            InitialCategoryNames.FiatAccount => language.BootDatabase_FiatAccount,
            InitialCategoryNames.Food => language.BootDatabase_Category_Food,
            InitialCategoryNames.UtilityBills => language.BootDatabase_Category_UtilityBills,
            InitialCategoryNames.Services => language.BootDatabase_Category_Services,
            InitialCategoryNames.Gadgets => language.BootDatabase_Category_Gadgets,
            InitialCategoryNames.Health => language.BootDatabase_Category_Health,
            InitialCategoryNames.Transport => language.BootDatabase_Category_Transport,
            InitialCategoryNames.Travel => language.BootDatabase_Category_Travel,
            InitialCategoryNames.Entertainment => language.BootDatabase_Category_Entertainment,
            InitialCategoryNames.Groceries => language.BootDatabase_Category_Groceries,
            InitialCategoryNames.Paycheck => language.BootDatabase_Category_Paycheck,
            _ => throw new ArgumentOutOfRangeException(nameof(categoryName), categoryName, null)
        };
    }
}