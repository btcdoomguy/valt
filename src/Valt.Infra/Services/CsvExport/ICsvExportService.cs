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

    /// <summary>
    /// Exports all avg price lines for a given profile to CSV format.
    /// </summary>
    /// <param name="profileId">The profile ID to export lines for</param>
    /// <returns>CSV content as a string with headers and line data</returns>
    Task<string> ExportAvgPriceLinesAsync(string profileId);
}
