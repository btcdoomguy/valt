using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Parses CSV files with strict column format for transaction import.
/// Expected columns: date, description, amount, account, to_account, to_amount, category
/// </summary>
internal class CsvImportParser : ICsvImportParser
{
    private const string DateFormat = "yyyy-MM-dd";

    public CsvImportResult Parse(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        return ParseFromReader(reader);
    }

    public CsvImportResult Parse(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        return ParseFromReader(reader);
    }

    private CsvImportResult ParseFromReader(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        var rows = new List<CsvImportRow>();
        var errors = new List<string>();

        try
        {
            using var csv = new CsvReader(reader, config);

            // Read header row
            if (!csv.Read() || !csv.ReadHeader())
            {
                return CsvImportResult.Failure("CSV file is empty or missing header row");
            }

            // Validate required headers
            var headerValidationError = ValidateHeaders(csv.HeaderRecord);
            if (headerValidationError != null)
            {
                return CsvImportResult.Failure(headerValidationError);
            }

            // Parse data rows
            while (csv.Read())
            {
                var lineNumber = csv.Parser.Row;
                var parseResult = ParseRow(csv, lineNumber);

                if (parseResult.Success)
                {
                    rows.Add(parseResult.Row!);
                }
                else
                {
                    errors.Add(parseResult.ErrorMessage!);
                }
            }
        }
        catch (Exception ex)
        {
            return CsvImportResult.Failure($"Failed to parse CSV file: {ex.Message}");
        }

        if (rows.Count == 0 && errors.Count == 0)
        {
            return CsvImportResult.Failure("CSV file contains no data rows");
        }

        if (errors.Count > 0)
        {
            return CsvImportResult.PartialSuccess(rows, errors);
        }

        return CsvImportResult.Success(rows);
    }

    private static string? ValidateHeaders(string[]? headers)
    {
        if (headers == null || headers.Length == 0)
        {
            return "CSV file has no headers";
        }

        var requiredHeaders = new[] { "date", "description", "amount", "account", "category" };
        var normalizedHeaders = headers.Select(h => h.ToLowerInvariant()).ToHashSet();

        var missingHeaders = requiredHeaders.Where(h => !normalizedHeaders.Contains(h)).ToList();
        if (missingHeaders.Count > 0)
        {
            return $"Missing required columns: {string.Join(", ", missingHeaders)}";
        }

        return null;
    }

    private static RowParseResult ParseRow(CsvReader csv, int lineNumber)
    {
        // Parse date (required)
        var dateStr = csv.GetField("date");
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Missing required field 'date'");
        }

        if (!DateOnly.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Invalid date format '{dateStr}'. Expected format: {DateFormat}");
        }

        // Parse description (required)
        var description = csv.GetField("description");
        if (string.IsNullOrWhiteSpace(description))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Missing required field 'description'");
        }

        // Parse amount (required)
        var amountStr = csv.GetField("amount");
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Missing required field 'amount'");
        }

        if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Invalid amount format '{amountStr}'. Expected decimal number");
        }

        // Parse account (required)
        var accountName = csv.GetField("account");
        if (string.IsNullOrWhiteSpace(accountName))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Missing required field 'account'");
        }

        // Parse category (required)
        var categoryName = csv.GetField("category");
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return RowParseResult.Fail($"Line {lineNumber}: Missing required field 'category'");
        }

        // Parse to_account (optional)
        var toAccountName = csv.GetField("to_account");
        if (string.IsNullOrWhiteSpace(toAccountName))
        {
            toAccountName = null;
        }

        // Parse to_amount (optional)
        decimal? toAmount = null;
        var toAmountStr = csv.GetField("to_amount");
        if (!string.IsNullOrWhiteSpace(toAmountStr))
        {
            if (!decimal.TryParse(toAmountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedToAmount))
            {
                return RowParseResult.Fail($"Line {lineNumber}: Invalid to_amount format '{toAmountStr}'. Expected decimal number");
            }
            toAmount = parsedToAmount;
        }

        var row = new CsvImportRow(
            date,
            description,
            amount,
            accountName,
            toAccountName,
            toAmount,
            categoryName,
            lineNumber);

        return RowParseResult.Ok(row);
    }

    private readonly struct RowParseResult
    {
        public bool Success { get; }
        public CsvImportRow? Row { get; }
        public string? ErrorMessage { get; }

        private RowParseResult(bool success, CsvImportRow? row, string? errorMessage)
        {
            Success = success;
            Row = row;
            ErrorMessage = errorMessage;
        }

        public static RowParseResult Ok(CsvImportRow row) => new(true, row, null);
        public static RowParseResult Fail(string errorMessage) => new(false, null, errorMessage);
    }
}
