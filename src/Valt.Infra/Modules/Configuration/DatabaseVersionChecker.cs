using System.Reflection;

namespace Valt.Infra.Modules.Configuration;

/// <summary>
/// Result of checking database version compatibility.
/// </summary>
public record DatabaseCompatibilityResult(
    bool IsCompatible,
    Version? RequiredVersion,
    Version? CurrentVersion);

/// <summary>
/// Checks if the current application version is compatible with the database.
/// </summary>
public interface IDatabaseVersionChecker
{
    /// <summary>
    /// Checks if the current assembly version is compatible with the minimum version required by the database.
    /// </summary>
    /// <returns>A result indicating compatibility status and version information.</returns>
    DatabaseCompatibilityResult CheckCompatibility();
}

internal class DatabaseVersionChecker : IDatabaseVersionChecker
{
    private readonly IConfigurationManager _configurationManager;

    public DatabaseVersionChecker(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public DatabaseCompatibilityResult CheckCompatibility()
    {
        var requiredVersion = _configurationManager.GetMinimumAssemblyVersion();
        var currentVersion = GetCurrentAssemblyVersion();

        // If no minimum version is set, the database is compatible (legacy or new database)
        if (requiredVersion is null)
        {
            return new DatabaseCompatibilityResult(true, null, currentVersion);
        }

        // Compare versions: current must be >= required
        var isCompatible = currentVersion >= requiredVersion;

        return new DatabaseCompatibilityResult(isCompatible, requiredVersion, currentVersion);
    }

    private static Version GetCurrentAssemblyVersion()
    {
        // Get the assembly version from the entry assembly (Valt.UI)
        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;

        // Fallback to a default version if unable to determine
        return assemblyVersion ?? new Version(0, 0, 0);
    }
}
