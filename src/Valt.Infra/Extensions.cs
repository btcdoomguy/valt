using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.DataAccess;
using Valt.Infra.DataAccess.Migrations;
using Valt.Infra.DataAccess.Migrations.Scripts;
using Valt.Infra.Kernel;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.EventSystem;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Kernel.Scopes;
using Valt.Infra.Kernel.Time;
using Valt.App.Modules.AvgPrice.Contracts;
using Valt.Infra.Modules.Assets;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.Infra.Modules.Assets.Queries;
using Valt.Infra.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.Budget;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Contracts;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Services;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Services;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Currency.Services;
using Valt.App.Modules.Goals.Contracts;
using Valt.Infra.Modules.Goals;
using Valt.Infra.Modules.Goals.Queries;
using Valt.Infra.Modules.Goals.Services;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Infra.Mcp.Server;
using Valt.Infra.Services.CsvExport;
using Valt.Infra.Services.CsvImport;
using Valt.Infra.Services.Updates;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;

[assembly: InternalsVisibleTo("Valt.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Valt.Infra;

public static class Extensions
{
    public static IServiceCollection AddValtInfrastructure(this IServiceCollection services)
    {
        //register all essential dependencies
        IdGenerator.Configure(new LiteDbIdProvider());

        services.AddSingleton<IContextScope, ContextScope>();
        services.AddSingleton<IClock, Clock>();
        services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();
        services.AddSingleton<ILocalDatabase, LocalDatabase>();
        services.AddSingleton<IPriceDatabase, PriceDatabase>();
        services.AddSingleton<AccountDisplayOrderManager>();

        //transaction terms
        services.AddSingleton<TransactionTermService>();

        //fixed expenses
        services.AddSingleton<IFixedExpenseProvider, FixedExpenseProvider>();

        //settings
        services.AddSingleton<CurrencySettings>();
        services.AddSingleton<DisplaySettings>();
        services.AddSingleton<UISettings>();

        //mcp server
        services.AddSingleton<McpServerState>();
        services.AddSingleton<McpServerService>();

        //price crawlers bitcoin
        services.AddSingleton<IBitcoinPriceProvider, CoinbaseProvider>();

        //price crawlers fiat
        services.AddSingleton<IFiatPriceProvider, FrankfurterFiatRateProvider>();
        services.AddSingleton<IFiatPriceProvider, CurrencyApiFiatRateProvider>();
        services.AddSingleton<IFiatPriceProviderSelector, FiatPriceProviderSelector>();

        //initial seed provider
        services.AddSingleton<IBitcoinInitialSeedPriceProvider, BitcoinInitialSeedPriceProvider>();

        //historical crawlers
        services.AddSingleton<KrakenBitcoinHistoricalDataProvider>();
        services.AddSingleton<IBitcoinHistoricalDataProvider, KrakenBitcoinHistoricalDataProvider>(provider =>
            provider.GetRequiredService<KrakenBitcoinHistoricalDataProvider>());

        services.AddSingleton<FrankfurterFiatHistoricalDataProvider>();
        services.AddSingleton<IFiatHistoricalDataProvider, FrankfurterFiatHistoricalDataProvider>(provider =>
            provider.GetRequiredService<FrankfurterFiatHistoricalDataProvider>());

        services.AddSingleton<StaticCsvFiatHistoricalDataProvider>();
        services.AddSingleton<IFiatHistoricalDataProvider, StaticCsvFiatHistoricalDataProvider>(provider =>
            provider.GetRequiredService<StaticCsvFiatHistoricalDataProvider>());

        services.AddSingleton<CurrencyApiFiatHistoricalDataProvider>();
        services.AddSingleton<IFiatHistoricalDataProvider, CurrencyApiFiatHistoricalDataProvider>(provider =>
            provider.GetRequiredService<CurrencyApiFiatHistoricalDataProvider>());

        //local historical provider
        services.AddSingleton<ILocalHistoricalPriceProvider, LocalHistoricalPriceProvider>();
        
        //reports
        services.AddSingleton<IAllTimeHighReport, AllTimeHighReport>();
        services.AddSingleton<IExpensesByCategoryReport, ExpensesByCategoryReport>();
        services.AddSingleton<IIncomeByCategoryReport, IncomeByCategoryReport>();
        services.AddSingleton<IMonthlyTotalsReport, MonthlyTotalsReport>();
        services.AddSingleton<IStatisticsReport, StatisticsReport>();
        services.AddSingleton<IWealthOverviewReport, WealthOverviewReport>();
        services.AddSingleton<IReportDataProviderFactory, ReportDataProviderFactory>();

        //background jobs
        services.AddSingleton<BackgroundJobManager>();

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<MigrationManager>();
        services.AddSingleton<IDatabaseVersionChecker, DatabaseVersionChecker>();

        services.Scan(scan => scan
            .FromAssemblyOf<Extensions.Foo>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i == typeof(IBackgroundJob))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        //event handlers
        services.Scan(scan => scan
            .FromAssemblyOf<Extensions.Foo>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<Extensions.Foo>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        //migrations
        services.Scan(scan => scan
            .FromAssemblyOf<Extensions.Foo>()
            .AddClasses(classes => classes.Where(type =>
                type.GetInterfaces().Any(i =>
                    i == typeof(IMigrationScript))), publicOnly: false)
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        return services
            .AddDbAccess()
            .AddRepositories()
            .AddQueries()
            .AddServices();
    }

    private static IServiceCollection AddDbAccess(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IAccountGroupRepository, AccountGroupRepository>();
        services.AddSingleton<IAvgPriceRepository, AvgPriceRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IFixedExpenseRepository, FixedExpenseRepository>();
        services.AddSingleton<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IGoalRepository, GoalRepository>();
        services.AddSingleton<IAssetRepository, AssetRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddSingleton<IAccountQueries, AccountQueries>();
        services.AddSingleton<IAvgPriceQueries, AvgPriceQueries>();
        services.AddSingleton<ICategoryQueries, CategoryQueries>();
        services.AddSingleton<IFixedExpenseQueries, FixedExpenseQueries>();
        services.AddSingleton<ITransactionQueries, TransactionQueries>();
        services.AddSingleton<IGoalQueries, GoalQueries>();
        services.AddSingleton<IAssetQueries, AssetQueries>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IFixedExpenseRecordService, FixedExpenseRecordService>();
        services.AddSingleton<IAccountCacheService, AccountCacheService>();
        services.AddSingleton<ITransactionTermService, TransactionTermService>();
        services.AddSingleton<IAccountTotalsCalculator, AccountTotalsCalculator>();
        services.AddSingleton<ITransactionAutoSatAmountCalculator, TransactionAutoSatAmountCalculator>();
        services.AddSingleton<IAvgPriceTotalizer, AvgPriceTotalizer>();
        services.AddSingleton<IUpdateChecker, GitHubUpdateChecker>();
        services.AddSingleton<ICurrencyConversionService, CurrencyConversionService>();

        //csv import
        services.AddSingleton<ICsvImportParser, CsvImportParser>();
        services.AddSingleton<ICsvTemplateGenerator, CsvTemplateGenerator>();
        services.AddSingleton<ICsvImportExecutor, CsvImportExecutor>();

        //csv export
        services.AddSingleton<ICsvExportService, CsvExportService>();

        //asset price providers
        services.AddSingleton<IAssetPriceProvider, YahooFinancePriceProvider>();
        services.AddSingleton<IAssetPriceProvider, LivePricePriceProvider>();
        services.AddSingleton<IAssetPriceProviderSelector, AssetPriceProviderSelector>();

        //goals
        services.AddSingleton<IGoalTransactionReader, GoalTransactionReader>();
        services.AddSingleton<IGoalProgressCalculator, StackBitcoinProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, SpendingLimitProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, DcaProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, IncomeFiatProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, IncomeBtcProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, ReduceExpenseCategoryProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculator, BitcoinHodlProgressCalculator>();
        services.AddSingleton<IGoalProgressCalculatorFactory, GoalProgressCalculatorFactory>();
        services.AddSingleton<IGoalProgressState, GoalProgressState>();

        return services;
    }

    public class Foo
    {
    }
}