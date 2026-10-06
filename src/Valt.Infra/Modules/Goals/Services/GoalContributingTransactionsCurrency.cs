using LiteDB;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Goals.Services;

/// <summary>
/// Currency-code resolution for the direct-database contributing-transactions overrides.
/// Those calculators only take <see cref="ILocalDatabase"/>, so the main fiat currency is
/// read from the persisted settings collection (the same key <c>BaseSettings</c> writes)
/// with a USD default — no <c>CurrencySettings</c> instance or reader dependency needed.
/// </summary>
internal static class GoalContributingTransactionsCurrency
{
    private const string MainFiatCurrencyKey = "CurrencySettings.MainFiatCurrency";

    public static string GetMainFiatCurrencyCode(ILocalDatabase localDatabase)
    {
        var setting = localDatabase.GetSettings().FindOne(x => x.Property == MainFiatCurrencyKey);
        return !string.IsNullOrEmpty(setting?.Value) ? setting.Value : FiatCurrency.Usd.Code;
    }

    /// <summary>
    /// Resolves the fiat currency code of a transaction's fiat leg through its from-account,
    /// falling back to the main fiat currency when the account row or its currency is missing
    /// (Q3 decision: unresolved fiat legs carry the main currency).
    /// </summary>
    public static string ResolveFromAccountCurrency(
        TransactionEntity transaction,
        IReadOnlyDictionary<ObjectId, AccountEntity> accounts,
        string mainFiatCurrencyCode)
    {
        return ResolveAccountCurrency(transaction.FromAccountId, accounts, mainFiatCurrencyCode);
    }

    /// <summary>
    /// Resolves the fiat currency code through the given (fiat-leg) account id, falling back
    /// to the main fiat currency when the account row or its currency is missing
    /// (Q3 decision: unresolved fiat legs carry the main currency).
    /// </summary>
    public static string ResolveAccountCurrency(
        ObjectId? accountId,
        IReadOnlyDictionary<ObjectId, AccountEntity> accounts,
        string mainFiatCurrencyCode)
    {
        return accountId is not null && accounts.TryGetValue(accountId, out var account)
            ? account.Currency ?? mainFiatCurrencyCode
            : mainFiatCurrencyCode;
    }
}
