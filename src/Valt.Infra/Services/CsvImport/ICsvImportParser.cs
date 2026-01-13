namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Parses CSV files into import row DTOs for transaction import.
/// Does not infer transaction types or validate account names - that's handled by the Import Wizard.
/// </summary>
public interface ICsvImportParser
{
    /// <summary>
    /// Parses a CSV file from a stream.
    /// </summary>
    /// <param name="csvStream">Stream containing CSV content</param>
    /// <returns>Result containing parsed rows or errors</returns>
    CsvImportResult Parse(Stream csvStream);

    /// <summary>
    /// Parses CSV content from a string.
    /// </summary>
    /// <param name="csvContent">String containing CSV content</param>
    /// <returns>Result containing parsed rows or errors</returns>
    CsvImportResult Parse(string csvContent);
}
