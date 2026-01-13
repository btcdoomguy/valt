namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Result of CSV import execution.
/// Contains counts of created entities and any errors encountered during import.
/// </summary>
public class CsvImportExecutionResult
{
    public bool Success { get; }
    public int TransactionsCreated { get; }
    public int AccountsCreated { get; }
    public int CategoriesCreated { get; }
    public IReadOnlyList<string> Errors { get; }

    private CsvImportExecutionResult(
        bool success,
        int transactionsCreated,
        int accountsCreated,
        int categoriesCreated,
        IReadOnlyList<string> errors)
    {
        Success = success;
        TransactionsCreated = transactionsCreated;
        AccountsCreated = accountsCreated;
        CategoriesCreated = categoriesCreated;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result with counts of created entities.
    /// </summary>
    public static CsvImportExecutionResult Succeeded(
        int transactionsCreated,
        int accountsCreated,
        int categoriesCreated)
    {
        return new CsvImportExecutionResult(true, transactionsCreated, accountsCreated, categoriesCreated, []);
    }

    /// <summary>
    /// Creates a partial success result - some transactions created but with errors.
    /// </summary>
    public static CsvImportExecutionResult PartialSuccess(
        int transactionsCreated,
        int accountsCreated,
        int categoriesCreated,
        IReadOnlyList<string> errors)
    {
        return new CsvImportExecutionResult(transactionsCreated > 0, transactionsCreated, accountsCreated, categoriesCreated, errors);
    }

    /// <summary>
    /// Creates a failure result with error messages.
    /// </summary>
    public static CsvImportExecutionResult Failure(IReadOnlyList<string> errors)
    {
        return new CsvImportExecutionResult(false, 0, 0, 0, errors);
    }

    /// <summary>
    /// Creates a failure result with a single error message.
    /// </summary>
    public static CsvImportExecutionResult Failure(string error)
    {
        return new CsvImportExecutionResult(false, 0, 0, 0, [error]);
    }
}
