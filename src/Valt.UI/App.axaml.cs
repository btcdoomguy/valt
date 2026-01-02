using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Valt.Core;
using Valt.Infra;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Settings;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Views.Main;
using Valt.UI.Views.Main.Tabs.Transactions.Models;
using MainViewModel = Valt.UI.Views.Main.MainViewModel;

namespace Valt.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var localStorageService = new LocalStorageService();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(localStorageService.LoadCulture());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(localStorageService.LoadCulture());

        var collection = new ServiceCollection();

        collection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        collection
            .AddValtCore()
            .AddValtInfrastructure()
            .AddValtUI(localStorageService);

        var serviceProvider = collection.BuildServiceProvider();

        //register the current service provider as the universal provider
        serviceProvider.SetAsContextScope();

        //initialize all setting classes
        _ = serviceProvider.GetService<CurrencySettings>();
        _ = serviceProvider.GetService<DisplaySettings>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainView()
            {
                DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            };
        }
        
        TransactionGridResources.Initialize();
        FixedExpenseListResources.Initialize();
        
        LiveCharts.Configure(config =>
            config
                .AddDefaultMappers()
                .AddSkiaSharp()
                .AddDarkTheme());

        base.OnFrameworkInitializationCompleted();

        if (!Design.IsDesignMode)
        {
            //otherwise the jobs will run with the IDE!
            var backgroundJobManager = serviceProvider.GetRequiredService<BackgroundJobManager>();

            backgroundJobManager.StartAllJobs(jobType: BackgroundJobTypes.App);
        }
    }

    //TODO: stop jobs before finalizing
}

public class Foo;