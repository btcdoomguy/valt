using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Services.CsvExport;

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

    /// <summary>
    /// Checks if a category name is the reserved InitialValue category.
    /// </summary>
    private static bool IsInitialValueCategory(string categoryName)
        => string.Equals(categoryName, CsvExportService.InitialValueCategory, StringComparison.OrdinalIgnoreCase);

    public async Task<CsvImportExecutionResult> ExecuteAsync(
        IReadOnlyList<CsvImportRow> rows,
        IReadOnlyList<CsvAccountMapping> accountMappings,
        IReadOnlyList<CsvCategoryMapping> categoryMappings,
        CsvImportMessages messages,
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
        progress?.Report(new CsvImportProgress(currentStep, totalSteps, messages.CreatingAccounts));

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
                    progress?.Report(new CsvImportProgress(currentStep, totalSteps, string.Format(messages.CreatedAccount, cleanName)));
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
                errors.Add(string.Format(messages.FailedToCreateAccount, mapping.CsvAccountName, ex.Message));
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
        progress?.Report(new CsvImportProgress(currentStep, totalSteps, messages.CreatingCategories));

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
                    progress?.Report(new CsvImportProgress(currentStep, totalSteps, string.Format(messages.CreatedCategory, mapping.CsvCategoryName)));
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
                errors.Add(string.Format(messages.FailedToCreateCategory, mapping.CsvCategoryName, ex.Message));
            }
        }

        // Phase 3: Create transactions (or set initial values for InitialValue rows)
        for (var i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[i];
            currentStep++;
            progress?.Report(new CsvImportProgress(currentStep, totalSteps, string.Format(messages.ImportingTransaction, i + 1, rows.Count)));

            try
            {
                // Look up account IDs
                if (!accountIdLookup.TryGetValue(row.AccountName, out var fromAccountId))
                {
                    errors.Add(string.Format(messages.AccountNotFound, row.LineNumber, row.AccountName));
                    continue;
                }

                var fromIsBtc = accountTypeLookup[row.AccountName];

                // Check if this is an InitialValue row - handle specially
                if (IsInitialValueCategory(row.CategoryName))
                {
                    await SetAccountInitialValueAsync(fromAccountId, fromIsBtc, row.Amount);
                    var cleanName = ExtractCleanName(row.AccountName);
                    progress?.Report(new CsvImportProgress(currentStep, totalSteps, string.Format(messages.SetInitialValue, cleanName)));
                    // Note: We don't increment transactionsCreated since this isn't a transaction
                    continue;
                }

                AccountId? toAccountId = null;
                bool? toIsBtc = null;
                if (!string.IsNullOrEmpty(row.ToAccountName))
                {
                    if (!accountIdLookup.TryGetValue(row.ToAccountName, out var toId))
                    {
                        errors.Add(string.Format(messages.ToAccountNotFound, row.LineNumber, row.ToAccountName));
                        continue;
                    }
                    toAccountId = toId;
                    toIsBtc = accountTypeLookup[row.ToAccountName];
                }

                // Look up category ID
                if (!categoryIdLookup.TryGetValue(row.CategoryName, out var categoryId))
                {
                    errors.Add(string.Format(messages.CategoryNotFound, row.LineNumber, row.CategoryName));
                    continue;
                }

                // Create transaction details based on account types
                var details = CreateTransactionDetails(row, fromAccountId, fromIsBtc, toAccountId, toIsBtc, messages);

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
                errors.Add(string.Format(messages.LineError, row.LineNumber, ex.Message));
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
        bool? toIsBtc,
        CsvImportMessages messages)
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
                    BtcValue.ParseBitcoin(Math.Abs(amount)),
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
                BtcValue.ParseBitcoin(Math.Abs(amount)));
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
                BtcValue.ParseBitcoin(Math.Abs(toAmount ?? 0)));
        }

        if (fromIsBtc && toIsBtc == false)
        {
            // BTC to Fiat (selling BTC)
            return new BitcoinToFiatDetails(
                fromAccountId,
                toAccountId,
                BtcValue.ParseBitcoin(Math.Abs(amount)),
                FiatValue.New(Math.Abs(toAmount ?? 0)));
        }

        throw new InvalidOperationException(string.Format(messages.UnableToDetermineType, row.LineNumber));
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

    /// <summary>
    /// Sets the initial value for an account. Used when importing InitialValue rows.
    /// </summary>
    private async Task SetAccountInitialValueAsync(AccountId accountId, bool isBtcAccount, decimal amount)
    {
        var account = await _accountRepository.GetAccountByIdAsync(accountId);
        if (account is null)
            return;

        if (isBtcAccount && account is BtcAccount btcAccount)
        {
            btcAccount.ChangeInitialAmount(BtcValue.ParseBitcoin(amount));
            await _accountRepository.SaveAccountAsync(btcAccount);
        }
        else if (!isBtcAccount && account is FiatAccount fiatAccount)
        {
            fiatAccount.ChangeInitialAmount(FiatValue.New(amount));
            await _accountRepository.SaveAccountAsync(fiatAccount);
        }
    }
}
