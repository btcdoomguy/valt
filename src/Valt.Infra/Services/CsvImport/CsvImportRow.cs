namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Represents a single parsed row from a CSV import file.
/// Contains raw data without any transaction type inference - that's handled by the Import Wizard.
/// </summary>
/// <param name="Date">Transaction date</param>
/// <param name="Description">Transaction description/name</param>
/// <param name="Amount">From account amount (negative = debit, positive = credit)</param>
/// <param name="AccountName">From account name with type indicator (e.g., "Checking [USD]" or "Wallet [btc]")</param>
/// <param name="ToAccountName">Optional destination account for transfers/exchanges</param>
/// <param name="ToAmount">Optional destination amount for exchanges with different currencies</param>
/// <param name="CategoryName">Transaction category name</param>
/// <param name="LineNumber">Original line number in CSV file (for error reporting)</param>
public record CsvImportRow(
    DateOnly Date,
    string Description,
    decimal Amount,
    string AccountName,
    string? ToAccountName,
    decimal? ToAmount,
    string CategoryName,
    int LineNumber);
