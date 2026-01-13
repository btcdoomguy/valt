namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Progress information for CSV import execution.
/// </summary>
/// <param name="CurrentRow">The current row being processed (1-based)</param>
/// <param name="TotalRows">Total number of rows to process</param>
/// <param name="CurrentAction">Description of current action (e.g., "Creating accounts...", "Importing transaction 5 of 10...")</param>
public record CsvImportProgress(int CurrentRow, int TotalRows, string CurrentAction)
{
    /// <summary>
    /// Percentage of completion (0-100).
    /// </summary>
    public int Percentage => TotalRows > 0 ? (int)((CurrentRow * 100.0) / TotalRows) : 0;
}
