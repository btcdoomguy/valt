using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;

namespace Valt.Infra.Services.CsvExport;

/// <summary>
/// Exports transactions to CSV format matching the import template.
/// Enables round-trip data migration: export -> import produces identical data.
/// </summary>
internal class CsvExportService : ICsvExportService
{
    /// <summary>
    /// Reserved category name for initial account values.
    /// When importing, rows with this category set the account's initial value instead of creating a transaction.
    /// </summary>
    public const string InitialValueCategory = "InitialValue";

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

        // Determine the first transaction date for each account
        var firstTransactionDateByAccount = GetFirstTransactionDateByAccount(transactionsResult.Items.ToList(), accountDict);

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

        // Write initial value rows for accounts with non-zero initial amounts
        foreach (var account in accounts)
        {
            var initialValueRow = CreateInitialValueRow(account, firstTransactionDateByAccount);
            if (initialValueRow != null)
            {
                WriteRow(csv, initialValueRow);
            }
        }

        // Write each transaction
        foreach (var transaction in transactionsResult.Items)
        {
            var row = MapTransactionToRow(transaction, accountDict, categoryDict);
            WriteRow(csv, row);
        }

        return stringWriter.ToString();
    }

    private static Dictionary<string, DateOnly> GetFirstTransactionDateByAccount(
        List<TransactionDTO> transactions,
        Dictionary<string, AccountDTO> accountDict)
    {
        var result = new Dictionary<string, DateOnly>();

        foreach (var transaction in transactions)
        {
            // Track the from account
            if (!result.ContainsKey(transaction.FromAccountId) ||
                transaction.Date < result[transaction.FromAccountId])
            {
                result[transaction.FromAccountId] = transaction.Date;
            }

            // Track the to account if present
            if (transaction.ToAccountId != null)
            {
                if (!result.ContainsKey(transaction.ToAccountId) ||
                    transaction.Date < result[transaction.ToAccountId])
                {
                    result[transaction.ToAccountId] = transaction.Date;
                }
            }
        }

        return result;
    }

    private static CsvExportRow? CreateInitialValueRow(
        AccountDTO account,
        Dictionary<string, DateOnly> firstTransactionDateByAccount)
    {
        // Check if account has a non-zero initial amount
        bool hasInitialAmount;
        string amountStr;

        if (account.IsBtcAccount)
        {
            hasInitialAmount = account.InitialAmountSats.HasValue && account.InitialAmountSats.Value != 0;
            if (!hasInitialAmount) return null;

            var btcValue = account.InitialAmountSats!.Value / 100_000_000m;
            amountStr = btcValue.ToString("F8", CultureInfo.InvariantCulture);
        }
        else
        {
            hasInitialAmount = account.InitialAmountFiat.HasValue && account.InitialAmountFiat.Value != 0;
            if (!hasInitialAmount) return null;

            amountStr = account.InitialAmountFiat!.Value.ToString("F2", CultureInfo.InvariantCulture);
        }

        // Determine the date - use first transaction date for this account, or today if no transactions
        var date = firstTransactionDateByAccount.TryGetValue(account.Id, out var firstDate)
            ? firstDate
            : DateOnly.FromDateTime(DateTime.Today);

        return new CsvExportRow(
            Date: date.ToString("yyyy-MM-dd"),
            Description: InitialValueCategory,
            Amount: amountStr,
            Account: FormatAccountName(account),
            ToAccount: string.Empty,
            ToAmount: string.Empty,
            Category: InitialValueCategory);
    }

    private static CsvExportRow MapTransactionToRow(
        TransactionDTO transaction,
        Dictionary<string, AccountDTO> accountDict,
        Dictionary<string, CategoryDTO> categoryDict)
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

    private static string FormatAccountName(AccountDTO? account)
    {
        if (account == null) return string.Empty;

        // BTC accounts use [btc] suffix, fiat accounts use their currency code
        var suffix = account.IsBtcAccount ? "btc" : account.Currency ?? "USD";
        return $"{account.Name} [{suffix}]";
    }

    private static (string Amount, string ToAmount) FormatAmounts(
        TransactionDTO transaction,
        AccountDTO? fromAccount,
        AccountDTO? toAccount)
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
