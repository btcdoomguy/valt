using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Modules.Reports;
using Valt.Infra.Modules.Reports.AllTimeHigh;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.IncomeByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.Infra.Modules.Reports.Statistics;
using Valt.Infra.Modules.Reports.WealthOverview;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Settings;

namespace Valt.Infra.Mcp.Server;

/// <summary>
/// Service that manages the embedded MCP server lifecycle.
/// Runs Kestrel on a background thread with SSE transport for MCP clients.
/// </summary>
public class McpServerService
{
    private readonly McpServerState _state;
    private readonly ILocalDatabase _localDatabase;
    private readonly IServiceProvider _appServices;
    private readonly ILogger<McpServerService> _logger;

    private WebApplication? _app;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    private static readonly int[] FallbackPorts = [5200, 5201, 5202, 5203, 5204];

    public McpServerService(
        McpServerState state,
        ILocalDatabase localDatabase,
        IServiceProvider appServices,
        ILogger<McpServerService> logger)
    {
        _state = state;
        _localDatabase = localDatabase;
        _appServices = appServices;
        _logger = logger;
    }

    /// <summary>
    /// Starts the MCP server on the configured port or a fallback port.
    /// </summary>
    public async Task StartAsync(int preferredPort)
    {
        if (_state.IsRunning)
        {
            _logger.LogWarning("MCP server is already running");
            return;
        }

        if (!_localDatabase.HasDatabaseOpen)
        {
            _state.SetError("Database must be open to start MCP server");
            return;
        }

        _cts = new CancellationTokenSource();

        // Try the preferred port first, then fallbacks
        var portsToTry = new List<int> { preferredPort };
        portsToTry.AddRange(FallbackPorts.Where(p => p != preferredPort));

        foreach (var port in portsToTry)
        {
            try
            {
                await StartOnPortAsync(port, _cts.Token);
                _logger.LogInformation("MCP server started on port {Port}", port);
                _state.SetRunning(port);
                return;
            }
            catch (Exception ex) when (IsPortInUseException(ex))
            {
                _logger.LogWarning("Port {Port} is in use, trying next port", port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MCP server on port {Port}", port);
            }
        }

        _state.SetError($"Could not start MCP server. All ports ({string.Join(", ", portsToTry)}) are in use.");
    }

    private async Task StartOnPortAsync(int port, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Add MCP server with HTTP transport in stateless mode (no session tracking needed)
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options =>
            {
                // Stateless mode is better for embedded servers - no session management needed
                options.Stateless = true;
            })
            .WithToolsFromAssembly(typeof(McpServerService).Assembly);

        // Forward application services from the main DI container to MCP tools
        ForwardServicesFromMainApp(builder.Services);

        _app = builder.Build();

        // Map the MCP endpoint (stateless mode uses HTTP streaming, not SSE)
        _app.MapMcp("/mcp");

        _serverTask = Task.Run(async () =>
        {
            try
            {
                await _app.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP server error");
                _state.SetError(ex.Message);
            }
        }, ct);

        // Give the server a moment to start and verify it's running
        await Task.Delay(100, ct);

        if (_serverTask.IsFaulted)
        {
            await _serverTask; // This will throw the exception
        }
    }

    /// <summary>
    /// Stops the MCP server gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_state.IsRunning || _app is null)
        {
            return;
        }

        _logger.LogInformation("Stopping MCP server");

        try
        {
            _cts?.Cancel();

            using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _app.StopAsync(stopCts.Token);

            if (_serverTask is not null)
            {
                await Task.WhenAny(_serverTask, Task.Delay(TimeSpan.FromSeconds(5)));
            }

            await _app.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server");
        }
        finally
        {
            _app = null;
            _serverTask = null;
            _cts?.Dispose();
            _cts = null;
            _state.SetStopped();
        }
    }

    /// <summary>
    /// Toggles the MCP server on or off.
    /// </summary>
    public async Task ToggleAsync(int preferredPort)
    {
        if (_state.IsRunning)
        {
            await StopAsync();
        }
        else
        {
            await StartAsync(preferredPort);
        }
    }

    private static bool IsPortInUseException(Exception ex)
    {
        // Check for common port-in-use indicators
        return ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("EADDRINUSE", StringComparison.OrdinalIgnoreCase)
               || ex.InnerException?.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Forwards services from the main application's DI container to the embedded MCP server.
    /// This allows MCP tools to access application services like dispatchers and reports.
    /// </summary>
    private void ForwardServicesFromMainApp(IServiceCollection services)
    {
        // Database access
        services.AddSingleton(_localDatabase);

        // Command/Query dispatchers (needed by all CRUD tools)
        services.AddSingleton(_appServices.GetRequiredService<ICommandDispatcher>());
        services.AddSingleton(_appServices.GetRequiredService<IQueryDispatcher>());

        // Report services (needed by ReportTools)
        services.AddSingleton(_appServices.GetRequiredService<IReportDataProviderFactory>());
        services.AddSingleton(_appServices.GetRequiredService<IAllTimeHighReport>());
        services.AddSingleton(_appServices.GetRequiredService<IExpensesByCategoryReport>());
        services.AddSingleton(_appServices.GetRequiredService<IIncomeByCategoryReport>());
        services.AddSingleton(_appServices.GetRequiredService<IMonthlyTotalsReport>());
        services.AddSingleton(_appServices.GetRequiredService<IStatisticsReport>());
        services.AddSingleton(_appServices.GetRequiredService<IWealthOverviewReport>());

        // Notification publisher (needed for MCP tools to notify UI of changes)
        services.AddSingleton(_appServices.GetRequiredService<INotificationPublisher>());

        // Currency services (needed by CurrencyTools)
        services.AddSingleton(_appServices.GetRequiredService<IConfigurationManager>());
        services.AddSingleton(_appServices.GetRequiredService<CurrencySettings>());
        services.AddSingleton(_appServices.GetRequiredService<ICurrencyConversionService>());
        services.AddSingleton(_appServices.GetRequiredService<ILocalHistoricalPriceProvider>());
        services.AddSingleton(_appServices.GetRequiredService<IBitcoinPriceProvider>());
        services.AddSingleton(_appServices.GetRequiredService<IFiatPriceProviderSelector>());
    }
}
