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
using Valt.Infra.Modules.AvgPrice;
using Valt.Infra.Modules.AvgPrice.Queries;
using Valt.Infra.Modules.Budget;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.Infra.Modules.Budget.Categories;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Services;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Services;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Services.Updates;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;

[assembly: InternalsVisibleTo("Valt.Tests")]

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

        //price crawlers bitcoin
        services.AddSingleton<IBitcoinPriceProvider, CoinbaseProvider>();

        //price crawlers fiat
        services.AddSingleton<IFiatPriceProvider, FrankfurterFiatRateProvider>();
        
        //initial seed provider
        services.AddSingleton<IBitcoinInitialSeedPriceProvider, BitcoinInitialSeedPriceProvider>();

        //historical crawlers
        services.AddSingleton<KrakenBitcoinHistoricalDataProvider>();
        services.AddSingleton<IBitcoinHistoricalDataProvider, KrakenBitcoinHistoricalDataProvider>(provider =>
            provider.GetRequiredService<KrakenBitcoinHistoricalDataProvider>());

        services.AddSingleton<FrankfurterFiatHistoricalDataProvider>();
        services.AddSingleton<IFiatHistoricalDataProvider, FrankfurterFiatHistoricalDataProvider>(provider =>
            provider.GetRequiredService<FrankfurterFiatHistoricalDataProvider>());

        //local historical provider
        services.AddSingleton<ILocalHistoricalPriceProvider, LocalHistoricalPriceProvider>();
        
        //reports
        services.AddSingleton<IAllTimeHighReport, AllTimeHighReport>();
        services.AddSingleton<IExpensesByCategoryReport, ExpensesByCategoryReport>();
        services.AddSingleton<IMonthlyTotalsReport, MonthlyTotalsReport>();
        services.AddSingleton<IStatisticsReport, StatisticsReport>();
        services.AddSingleton<IReportDataProviderFactory, ReportDataProviderFactory>();

        //background jobs
        services.AddSingleton<BackgroundJobManager>();
        services.AddSingleton<BackgroundJobCoordinator>();

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<ConfigurationManager>();
        services.AddSingleton<MigrationManager>();

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
        services.AddSingleton<IAvgPriceRepository, AvgPriceRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IFixedExpenseRepository, FixedExpenseRepository>();
        services.AddSingleton<ITransactionRepository, TransactionRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddSingleton<IAccountQueries, AccountQueries>();
        services.AddSingleton<IAvgPriceQueries, AvgPriceQueries>();
        services.AddSingleton<ICategoryQueries, CategoryQueries>();
        services.AddSingleton<IFixedExpenseQueries, FixedExpenseQueries>();
        services.AddSingleton<ITransactionQueries, TransactionQueries>();

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

        return services;
    }

    public class Foo
    {
    }
}