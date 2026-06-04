using Microsoft.Extensions.Logging;
using Valt.Infra.DataAccess;
using System.Threading.Tasks;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Configuration;

namespace Valt.UI.Services;

/// <summary>
/// Orchestrates database lifecycle operations: open, initialize, migrate, and close.
/// Keeps UI concerns (modals, loading state) in the ViewModel.
/// </summary>
public interface IDatabaseLifecycleService
{
    /// <summary>
    /// Opens the local database file. Returns success or error message.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> OpenLocalDatabaseAsync(string file, string password);

    /// <summary>
    /// Closes the local database.
    /// </summary>
    void CloseLocalDatabase();

    /// <summary>
    /// Checks database version compatibility.
    /// </summary>
    (bool IsCompatible, string? RequiredVersion, string? CurrentVersion) CheckCompatibility();

    /// <summary>
    /// Initializes a new database with default data.
    /// </summary>
    Task InitializeNewDatabaseAsync(string language, string[] currencies);

    /// <summary>
    /// Runs database migrations.
    /// </summary>
    Task MigrateAsync();

    /// <summary>
    /// Initializes the price database (open, install if empty, dedup, start jobs).
    /// Returns success or error message.
    /// </summary>
    Task<PriceDatabaseInitResult> InitializePriceDatabaseAsync();

    /// <summary>
    /// Stops all background jobs and closes both databases.
    /// </summary>
    Task CloseDatabasesAsync();

    /// <summary>
    /// Starts all ValtDatabase background jobs.
    /// </summary>
    Task StartValtDatabaseJobsAsync();

    /// <summary>
    /// Triggers a background job manually.
    /// </summary>
    void TriggerJobManually(BackgroundJobSystemNames systemName);
}

public record PriceDatabaseInitResult(bool Success, string? ErrorMessage);
