using System;
using Microsoft.Extensions.DependencyInjection;
using Valt.Infra.Kernel.Scopes;
using Valt.Infra.Modules.Budget.Categories;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Services.Theming;
using Valt.UI.State;
using Valt.UI.Views;
using Valt.UI.Views.Main;
using Valt.UI.Views.Main.Controls;
using Valt.UI.Views.Main.Modals.About;
using Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;
using Valt.UI.Views.Main.Modals.ChangePassword;
using Valt.UI.Views.Main.Modals.CreateDatabase;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;
using Valt.UI.Views.Main.Modals.FixedExpenseHistory;
using Valt.UI.Views.Main.Modals.IconSelector;
using Valt.UI.Views.Main.Modals.InitialSelection;
using Valt.UI.Views.Main.Modals.InputPassword;
using Valt.UI.Views.Main.Modals.ManageAccount;
using Valt.UI.Views.Main.Modals.ManageCategories;
using Valt.UI.Views.Main.Modals.ManageFixedExpenses;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Modals.MathExpression;
using Valt.UI.Views.Main.Modals.Settings;
using Valt.UI.Views.Main.Modals.StatusDisplay;
using Valt.UI.Views.Main.Tabs.AvgPrice;
using Valt.UI.Views.Main.Tabs.Reports;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Views.Main.Modals.AvgPriceLineEditor;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;
using Valt.UI.Views.Main.Modals.ImportWizard;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI;

public static class Extensions
{
    public static IServiceCollection AddValtUI(this IServiceCollection services, ILocalStorageService localStorageService)
    {
        services.AddSingleton(localStorageService);
        services.AddTransient<MainViewModel>();
        
        //controls
        services.AddTransient<LiveRatesViewModel>();
        services.AddSingleton<UpdateIndicatorViewModel>();

        //pages
        services.AddSingleton<TransactionsViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<AvgPriceViewModel>();
        //factory method for pages
        services.AddSingleton<Func<MainViewTabNames, ValtTabViewModel>>(services => pageNames =>
        {
            return pageNames switch
            {
                MainViewTabNames.TransactionsPageContent => services.GetRequiredService<TransactionsViewModel>(),
                MainViewTabNames.ReportsPageContent => services.GetRequiredService<ReportsViewModel>(),
                MainViewTabNames.AvgPricePageContent => services.GetRequiredService<AvgPriceViewModel>(),
                _ => throw new ArgumentOutOfRangeException(nameof(pageNames), pageNames, null)
            };
        });
        services.AddSingleton<IPageFactory, PageFactory>();
        
        //transactions tabs
        services.AddSingleton<TransactionListViewModel>();
        //factory method for the transactions tabs
        services.AddSingleton<Func<TransactionsTabNames, ValtViewModel>>(services => tabNames =>
        {
            return tabNames switch
            {
                TransactionsTabNames.List => services.GetRequiredService<TransactionListViewModel>(),
                _ => throw new ArgumentOutOfRangeException(nameof(tabNames), tabNames, null)
            };
        });
        services.AddSingleton<ITransactionTabFactory, TransactionTabFactory>();

        //modals
        services.AddTransient<AboutViewModel>();
        services.AddTransient<InitialSelectionViewModel>();
        services.AddTransient<CreateDatabaseViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<ChangeCategoryTransactionsViewModel>();
        services.AddTransient<InputPasswordViewModel>();
        services.AddTransient<ManageAccountViewModel>();
        services.AddTransient<ManageCategoriesViewModel>();
        services.AddTransient<TransactionEditorViewModel>();
        services.AddTransient<IconSelectorViewModel>();
        services.AddTransient<MathExpressionViewModel>();
        services.AddTransient<StatusDisplayViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ManageFixedExpensesViewModel>();
        services.AddTransient<FixedExpenseEditorViewModel>();
        services.AddTransient<ManageAvgPriceProfilesViewModel>();
        services.AddTransient<AvgPriceLineEditorViewModel>();
        services.AddTransient<FixedExpenseHistoryViewModel>();
        services.AddTransient<ImportWizardViewModel>();

        //other
        services.AddSingleton<IInitialCategoryNameLanguageProvider, InitialCategoryNameLanguageProvider>();


        //factory method for modals
        services.AddSingleton<Func<ApplicationModalNames, ValtBaseWindow>>(services => modalNames =>
        {
            return modalNames switch
            {
                ApplicationModalNames.About => new AboutView()
                {
                    DataContext = services.GetRequiredService<AboutViewModel>(),
                },
                ApplicationModalNames.InitialSelection => new InitialSelectionView()
                {
                    DataContext = services.GetRequiredService<InitialSelectionViewModel>(),
                },
                ApplicationModalNames.CreateDatabase => new CreateDatabaseView()
                {
                    DataContext = services.GetRequiredService<CreateDatabaseViewModel>(),
                },
                ApplicationModalNames.ChangePassword => new ChangePasswordView()
                {
                    DataContext = services.GetRequiredService<ChangePasswordViewModel>(),
                },
                ApplicationModalNames.ChangeCategoryTransactions => new ChangeCategoryTransactionsView()
                {
                    DataContext = services.GetRequiredService<ChangeCategoryTransactionsViewModel>(),
                },
                ApplicationModalNames.InputPassword => new InputPasswordView()
                {
                    DataContext = services.GetRequiredService<InputPasswordViewModel>(),
                },
                ApplicationModalNames.ManageAccount => new ManageAccountView()
                {
                    DataContext = services.GetRequiredService<ManageAccountViewModel>(),
                },
                ApplicationModalNames.ManageCategories => new ManageCategoriesView()
                {
                    DataContext = services.GetRequiredService<ManageCategoriesViewModel>(),
                },
                ApplicationModalNames.TransactionEditor => new TransactionEditorView()
                {
                    DataContext = services.GetRequiredService<TransactionEditorViewModel>(),
                },
                ApplicationModalNames.IconSelector => new IconSelectorView()
                {
                    DataContext = services.GetRequiredService<IconSelectorViewModel>(),
                },
                ApplicationModalNames.MathExpression => new MathExpressionView()
                {
                    DataContext = services.GetRequiredService<MathExpressionViewModel>(),
                },
                ApplicationModalNames.StatusDisplay => new StatusDisplayView()
                {
                    DataContext = services.GetRequiredService<StatusDisplayViewModel>(),
                },
                ApplicationModalNames.Settings => new SettingsView()
                {
                    DataContext = services.GetRequiredService<SettingsViewModel>(),
                },
                ApplicationModalNames.ManageFixedExpenses => new ManageFixedExpensesView()
                {
                    DataContext = services.GetRequiredService<ManageFixedExpensesViewModel>(),
                },
                ApplicationModalNames.FixedExpenseEditor => new FixedExpenseEditorView()
                {
                    DataContext = services.GetRequiredService<FixedExpenseEditorViewModel>(),
                },
                ApplicationModalNames.AvgPriceProfileManager => new ManageAvgPriceProfilesView()
                {
                    DataContext = services.GetRequiredService<ManageAvgPriceProfilesViewModel>(),
                },
                ApplicationModalNames.AvgPriceLineEditor => new AvgPriceLineEditorView()
                {
                    DataContext = services.GetRequiredService<AvgPriceLineEditorViewModel>(),
                },
                ApplicationModalNames.FixedExpenseHistory => new FixedExpenseHistoryView()
                {
                    DataContext = services.GetRequiredService<FixedExpenseHistoryViewModel>(),
                },
                ApplicationModalNames.ImportWizard => new ImportWizardView()
                {
                    DataContext = services.GetRequiredService<ImportWizardViewModel>(),
                },
                _ => throw new ArgumentOutOfRangeException(nameof(modalNames), modalNames, null)
            };
        });
        services.AddSingleton<IModalFactory, ModalFactory>();

        //state objects
        services.AddSingleton<RatesState>();
        services.AddSingleton<AccountsTotalState>();
        services.AddSingleton<FilterState>();
        services.AddSingleton<LiveRateState>();
        services.AddSingleton<SecureModeState>();

        //theming
        services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }

    public static ServiceProvider SetAsContextScope(this ServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<IContextScope>().SetCustomServiceProvider(serviceProvider);
        return serviceProvider;
    }
}