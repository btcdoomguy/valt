namespace Valt.Infra.Services.CsvExport;

/// <summary>
/// Service for exporting transactions to CSV format.
/// The export format matches the import template for round-trip compatibility.
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Exports all transactions to CSV format.
    /// </summary>
    /// <returns>CSV content as a string with headers and transaction data</returns>
    Task<string> ExportTransactionsAsync();
}
