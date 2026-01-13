namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Service for executing CSV import - creates accounts, categories, and transactions
/// from parsed CSV data and user-specified mappings.
/// </summary>
public interface ICsvImportExecutor
{
    /// <summary>
    /// Executes the CSV import, creating accounts, categories, and transactions.
    /// </summary>
    /// <param name="rows">Parsed CSV rows to import</param>
    /// <param name="accountMappings">Account mapping configuration (new vs existing accounts)</param>
    /// <param name="categoryMappings">Category mapping configuration (new vs existing categories)</param>
    /// <param name="progress">Optional progress reporter for UI updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing counts of created entities and any errors</returns>
    Task<CsvImportExecutionResult> ExecuteAsync(
        IReadOnlyList<CsvImportRow> rows,
        IReadOnlyList<CsvAccountMapping> accountMappings,
        IReadOnlyList<CsvCategoryMapping> categoryMappings,
        IProgress<CsvImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
