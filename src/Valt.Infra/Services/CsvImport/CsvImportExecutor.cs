using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Configuration;

namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Executes CSV import by creating accounts, categories, and transactions from parsed data.
/// </summary>
public class CsvImportExecutor : ICsvImportExecutor
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IConfigurationManager _configurationManager;

    public CsvImportExecutor(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IConfigurationManager configurationManager)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _configurationManager = configurationManager;
    }

    public async Task<CsvImportExecutionResult> ExecuteAsync(
        IReadOnlyList<CsvImportRow> rows,
        IReadOnlyList<CsvAccountMapping> accountMappings,
        IReadOnlyList<CsvCategoryMapping> categoryMappings,
        IProgress<CsvImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var accountsCreated = 0;
        var categoriesCreated = 0;
        var transactionsCreated = 0;

        // Build lookup dictionaries
        var accountIdLookup = new Dictionary<string, AccountId>(StringComparer.OrdinalIgnoreCase);
        var accountTypeLookup = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase); // true = BTC
        var categoryIdLookup = new Dictionary<string, CategoryId>(StringComparer.OrdinalIgnoreCase);

        var totalSteps = rows.Count + accountMappings.Count(a => a.IsNew) + categoryMappings.Count(c => c.IsNew);
        var currentStep = 0;

        // Phase 1: Create new accounts
        progress?.Report(new CsvImportProgress(currentStep, totalSteps, "Creating accounts..."));

        foreach (var mapping in accountMappings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (mapping.IsNew)
                {
                    var cleanName = ExtractCleanName(mapping.CsvAccountName);
                    AccountId accountId;

                    if (mapping.IsBtcAccount)
                    {
                        var btcAccount = BtcAccount.New(
                            AccountName.New(cleanName),
                            AccountCurrencyNickname.Empty,
                            visible: true,
                            Icon.Empty,
                            BtcValue.Empty);
                        await _accountRepository.SaveAccountAsync(btcAccount);
                        accountId = btcAccount.Id;
                    }
                    else
                    {
                        var currency = FiatCurrency.GetFromCode(mapping.Currency ?? "USD");
                        var fiatAccount = FiatAccount.New(
                            AccountName.New(cleanName),
                            AccountCurrencyNickname.Empty,
                            visible: true,
                            Icon.Empty,
                            currency,
                            FiatValue.Empty);
                        await _accountRepository.SaveAccountAsync(fiatAccount);
                        accountId = fiatAccount.Id;
                    }

                    accountIdLookup[mapping.CsvAccountName] = accountId;
                    accountTypeLookup[mapping.CsvAccountName] = mapping.IsBtcAccount;
                    accountsCreated++;
                    currentStep++;
                    progress?.Report(new CsvImportProgress(currentStep, totalSteps, $"Created account: {cleanName}"));
                }
                else
                {
                    // Existing account - use provided ID
                    if (!string.IsNullOrEmpty(mapping.AccountId))
                    {
                        accountIdLookup[mapping.CsvAccountName] = new AccountId(mapping.AccountId);
                        accountTypeLookup[mapping.CsvAccountName] = mapping.IsBtcAccount;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create account '{mapping.CsvAccountName}': {ex.Message}");
            }
        }

        // Register all fiat currencies from account mappings to the configuration
        var fiatCurrencies = accountMappings
            .Where(m => !m.IsBtcAccount && !string.IsNullOrWhiteSpace(m.Currency))
            .Select(m => m.Currency!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var currency in fiatCurrencies)
        {
            _configurationManager.AddFiatCurrency(currency);
        }

        // Phase 2: Create new categories
        progress?.Report(new CsvImportProgress(currentStep, totalSteps, "Creating categories..."));

        foreach (var mapping in categoryMappings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (mapping.IsNew)
                {
                    var category = Category.New(
                        CategoryName.New(mapping.CsvCategoryName),
                        Icon.Empty,
                        parentId: null);
                    await _categoryRepository.SaveCategoryAsync(category);
                    categoryIdLookup[mapping.CsvCategoryName] = category.Id;
                    categoriesCreated++;
                    currentStep++;
                    progress?.Report(new CsvImportProgress(currentStep, totalSteps, $"Created category: {mapping.CsvCategoryName}"));
                }
                else
                {
                    // Existing category - use provided ID
                    if (!string.IsNullOrEmpty(mapping.CategoryId))
                    {
                        categoryIdLookup[mapping.CsvCategoryName] = new CategoryId(mapping.CategoryId);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create category '{mapping.CsvCategoryName}': {ex.Message}");
            }
        }

        // Phase 3: Create transactions
        for (var i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[i];
            currentStep++;
            progress?.Report(new CsvImportProgress(currentStep, totalSteps, $"Importing transaction {i + 1} of {rows.Count}..."));

            try
            {
                // Look up account IDs
                if (!accountIdLookup.TryGetValue(row.AccountName, out var fromAccountId))
                {
                    errors.Add($"Line {row.LineNumber}: Account '{row.AccountName}' not found in mappings");
                    continue;
                }

                var fromIsBtc = accountTypeLookup[row.AccountName];

                AccountId? toAccountId = null;
                bool? toIsBtc = null;
                if (!string.IsNullOrEmpty(row.ToAccountName))
                {
                    if (!accountIdLookup.TryGetValue(row.ToAccountName, out var toId))
                    {
                        errors.Add($"Line {row.LineNumber}: To-account '{row.ToAccountName}' not found in mappings");
                        continue;
                    }
                    toAccountId = toId;
                    toIsBtc = accountTypeLookup[row.ToAccountName];
                }

                // Look up category ID
                if (!categoryIdLookup.TryGetValue(row.CategoryName, out var categoryId))
                {
                    errors.Add($"Line {row.LineNumber}: Category '{row.CategoryName}' not found in mappings");
                    continue;
                }

                // Create transaction details based on account types
                var details = CreateTransactionDetails(row, fromAccountId, fromIsBtc, toAccountId, toIsBtc);

                // Create the transaction
                var transaction = Transaction.New(
                    row.Date,
                    TransactionName.New(row.Description),
                    categoryId,
                    details,
                    notes: null,
                    fixedExpense: null);

                await _transactionRepository.SaveTransactionAsync(transaction);
                transactionsCreated++;
            }
            catch (Exception ex)
            {
                errors.Add($"Line {row.LineNumber}: {ex.Message}");
            }
        }

        // Build result
        if (errors.Count == 0)
        {
            return CsvImportExecutionResult.Succeeded(transactionsCreated, accountsCreated, categoriesCreated);
        }

        if (transactionsCreated > 0)
        {
            return CsvImportExecutionResult.PartialSuccess(transactionsCreated, accountsCreated, categoriesCreated, errors);
        }

        return CsvImportExecutionResult.Failure(errors);
    }

    private static TransactionDetails CreateTransactionDetails(
        CsvImportRow row,
        AccountId fromAccountId,
        bool fromIsBtc,
        AccountId? toAccountId,
        bool? toIsBtc)
    {
        var amount = row.Amount;
        var toAmount = row.ToAmount;

        // Single account transaction (no transfer)
        if (toAccountId is null)
        {
            if (fromIsBtc)
            {
                // Bitcoin credit/debit
                return new BitcoinDetails(
                    fromAccountId,
                    BtcValue.ParseSats(Math.Abs(amount)),
                    credit: amount > 0);
            }
            else
            {
                // Fiat credit/debit
                return new FiatDetails(
                    fromAccountId,
                    FiatValue.New(Math.Abs(amount)),
                    credit: amount > 0);
            }
        }

        // Transfer between accounts
        if (fromIsBtc && toIsBtc == true)
        {
            // BTC to BTC transfer
            return new BitcoinToBitcoinDetails(
                fromAccountId,
                toAccountId,
                BtcValue.ParseSats(Math.Abs(amount)));
        }

        if (!fromIsBtc && toIsBtc == false)
        {
            // Fiat to Fiat transfer
            return new FiatToFiatDetails(
                fromAccountId,
                toAccountId,
                FiatValue.New(Math.Abs(amount)),
                FiatValue.New(Math.Abs(toAmount ?? amount)));
        }

        if (!fromIsBtc && toIsBtc == true)
        {
            // Fiat to BTC (buying BTC)
            return new FiatToBitcoinDetails(
                fromAccountId,
                toAccountId,
                FiatValue.New(Math.Abs(amount)),
                BtcValue.ParseSats(Math.Abs(toAmount ?? 0)));
        }

        if (fromIsBtc && toIsBtc == false)
        {
            // BTC to Fiat (selling BTC)
            return new BitcoinToFiatDetails(
                fromAccountId,
                toAccountId,
                BtcValue.ParseSats(Math.Abs(amount)),
                FiatValue.New(Math.Abs(toAmount ?? 0)));
        }

        throw new InvalidOperationException($"Unable to determine transaction type for row at line {row.LineNumber}");
    }

    /// <summary>
    /// Extracts the clean account name by removing the bracket suffix (e.g., "[USD]" or "[btc]").
    /// </summary>
    private static string ExtractCleanName(string csvAccountName)
    {
        var bracketStart = csvAccountName.LastIndexOf('[');
        if (bracketStart > 0)
        {
            return csvAccountName[..bracketStart].Trim();
        }
        return csvAccountName.Trim();
    }
}
