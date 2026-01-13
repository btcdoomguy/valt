using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

namespace Valt.Infra.Services.CsvExport;

/// <summary>
/// Exports transactions to CSV format matching the import template.
/// Enables round-trip data migration: export -> import produces identical data.
/// </summary>
internal class CsvExportService : ICsvExportService
{
    private readonly ITransactionQueries _transactionQueries;
    private readonly IAccountQueries _accountQueries;
    private readonly ICategoryQueries _categoryQueries;

    public CsvExportService(
        ITransactionQueries transactionQueries,
        IAccountQueries accountQueries,
        ICategoryQueries categoryQueries)
    {
        _transactionQueries = transactionQueries;
        _accountQueries = accountQueries;
        _categoryQueries = categoryQueries;
    }

    public async Task<string> ExportTransactionsAsync()
    {
        // Fetch all data
        var filter = new TransactionQueryFilter();
        var transactionsResult = await _transactionQueries.GetTransactionsAsync(filter);
        var accounts = (await _accountQueries.GetAccountsAsync(showHiddenAccounts: true)).ToList();
        var categories = (await _categoryQueries.GetCategoriesAsync()).Items;

        // Build lookup dictionaries
        var accountDict = accounts.ToDictionary(a => a.Id);
        var categoryDict = categories.ToDictionary(c => c.Id);

        // Configure CSV writer with same settings as import template
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var stringWriter = new StringWriter();
        using var csv = new CsvWriter(stringWriter, config);

        // Write header
        csv.WriteField("date");
        csv.WriteField("description");
        csv.WriteField("amount");
        csv.WriteField("account");
        csv.WriteField("to_account");
        csv.WriteField("to_amount");
        csv.WriteField("category");
        csv.NextRecord();

        // Write each transaction
        foreach (var transaction in transactionsResult.Items)
        {
            var row = MapTransactionToRow(transaction, accountDict, categoryDict);
            WriteRow(csv, row);
        }

        return stringWriter.ToString();
    }

    private static CsvExportRow MapTransactionToRow(
        TransactionDTO transaction,
        Dictionary<string, Modules.Budget.Accounts.Queries.DTOs.AccountDTO> accountDict,
        Dictionary<string, Modules.Budget.Categories.Queries.DTOs.CategoryDTO> categoryDict)
    {
        var fromAccount = accountDict.GetValueOrDefault(transaction.FromAccountId);
        var toAccount = transaction.ToAccountId != null
            ? accountDict.GetValueOrDefault(transaction.ToAccountId)
            : null;
        var category = categoryDict.GetValueOrDefault(transaction.CategoryId);

        // Format account names with currency suffix
        var fromAccountFormatted = FormatAccountName(fromAccount);
        var toAccountFormatted = toAccount != null ? FormatAccountName(toAccount) : string.Empty;

        // Format amounts based on account type and transaction type
        var (amount, toAmount) = FormatAmounts(transaction, fromAccount, toAccount);

        // Get category simple name (not nested format)
        var categoryName = category?.SimpleName ?? transaction.CategoryName;

        return new CsvExportRow(
            Date: transaction.Date.ToString("yyyy-MM-dd"),
            Description: transaction.Name,
            Amount: amount,
            Account: fromAccountFormatted,
            ToAccount: toAccountFormatted,
            ToAmount: toAmount,
            Category: categoryName);
    }

    private static string FormatAccountName(Modules.Budget.Accounts.Queries.DTOs.AccountDTO? account)
    {
        if (account == null) return string.Empty;

        // BTC accounts use [btc] suffix, fiat accounts use their currency code
        var suffix = account.IsBtcAccount ? "btc" : account.Currency ?? "USD";
        return $"{account.Name} [{suffix}]";
    }

    private static (string Amount, string ToAmount) FormatAmounts(
        TransactionDTO transaction,
        Modules.Budget.Accounts.Queries.DTOs.AccountDTO? fromAccount,
        Modules.Budget.Accounts.Queries.DTOs.AccountDTO? toAccount)
    {
        var isFromBtc = fromAccount?.IsBtcAccount ?? false;
        var isToBtc = toAccount?.IsBtcAccount ?? false;

        // Determine if this is a transfer (has destination account)
        var isTransfer = toAccount != null;

        // Format from amount
        string amount;
        if (isFromBtc && transaction.FromAmountSats.HasValue)
        {
            // BTC account: convert sats to BTC
            var btcValue = transaction.FromAmountSats.Value / 100_000_000m;
            amount = btcValue.ToString("F8", CultureInfo.InvariantCulture);
        }
        else if (transaction.FromAmountFiat.HasValue)
        {
            // Fiat account
            amount = transaction.FromAmountFiat.Value.ToString("F2", CultureInfo.InvariantCulture);
        }
        else
        {
            amount = "0.00";
        }

        // Format to amount (only for transfers)
        string toAmount = string.Empty;
        if (isTransfer)
        {
            if (isToBtc && transaction.ToAmountSats.HasValue)
            {
                // BTC destination: convert sats to BTC
                var btcValue = transaction.ToAmountSats.Value / 100_000_000m;
                toAmount = btcValue.ToString("F8", CultureInfo.InvariantCulture);
            }
            else if (transaction.ToAmountFiat.HasValue)
            {
                // Fiat destination
                toAmount = transaction.ToAmountFiat.Value.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        return (amount, toAmount);
    }

    private static void WriteRow(CsvWriter csv, CsvExportRow row)
    {
        csv.WriteField(row.Date);
        csv.WriteField(row.Description);
        csv.WriteField(row.Amount);
        csv.WriteField(row.Account);
        csv.WriteField(row.ToAccount);
        csv.WriteField(row.ToAmount);
        csv.WriteField(row.Category);
        csv.NextRecord();
    }

    /// <summary>
    /// Internal record for mapping transaction data to CSV row format.
    /// </summary>
    private record CsvExportRow(
        string Date,
        string Description,
        string Amount,
        string Account,
        string ToAccount,
        string ToAmount,
        string Category);
}
