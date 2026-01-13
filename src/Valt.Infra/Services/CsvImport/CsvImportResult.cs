namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Result wrapper for CSV import parsing operations.
/// Contains either successfully parsed rows or a list of parsing errors.
/// </summary>
public class CsvImportResult
{
    public bool IsSuccess { get; }
    public IReadOnlyList<CsvImportRow> Rows { get; }
    public IReadOnlyList<string> Errors { get; }

    private CsvImportResult(bool isSuccess, IReadOnlyList<CsvImportRow> rows, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Rows = rows;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result with parsed rows.
    /// </summary>
    public static CsvImportResult Success(IReadOnlyList<CsvImportRow> rows)
    {
        return new CsvImportResult(true, rows, []);
    }

    /// <summary>
    /// Creates a partial success result with parsed rows and errors.
    /// Some rows were parsed successfully, but some had errors.
    /// </summary>
    public static CsvImportResult PartialSuccess(IReadOnlyList<CsvImportRow> rows, IReadOnlyList<string> errors)
    {
        return new CsvImportResult(rows.Count > 0, rows, errors);
    }

    /// <summary>
    /// Creates a failure result with parsing errors.
    /// </summary>
    public static CsvImportResult Failure(IReadOnlyList<string> errors)
    {
        return new CsvImportResult(false, [], errors);
    }

    /// <summary>
    /// Creates a failure result with a single error message.
    /// </summary>
    public static CsvImportResult Failure(string error)
    {
        return new CsvImportResult(false, [], [error]);
    }
}
