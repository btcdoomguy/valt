using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating Transaction test data.
/// Supports both fluent API (preferred) and property initialization syntax.
/// </summary>
public class TransactionBuilder
{
    private TransactionId _id = new();
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private TransactionName _name = "Test Transaction";
    private CategoryId _categoryId = new();
    private TransactionDetails _transactionDetails = null!;
    private AutoSatAmountDetails? _autoSatAmountDetails = AutoSatAmountDetails.Pending;
    private string? _notes;
    private TransactionFixedExpenseReference? _fixedExpense;
    private GroupId? _groupId;
    private int _version = 0;

    // Public properties for backward compatibility with property initializer syntax
    public TransactionId Id { get => _id; set => _id = value; }
    public DateOnly Date { get => _date; set => _date = value; }
    public TransactionName Name { get => _name; set => _name = value; }
    public CategoryId CategoryId { get => _categoryId; set => _categoryId = value; }
    public TransactionDetails TransactionDetails { get => _transactionDetails; set => _transactionDetails = value; }
    public AutoSatAmountDetails? AutoSatAmountDetails { get => _autoSatAmountDetails; set => _autoSatAmountDetails = value; }
    public string? Notes { get => _notes; set => _notes = value; }
    public TransactionFixedExpenseReference? FixedExpense { get => _fixedExpense; set => _fixedExpense = value; }
    public GroupId? GroupId { get => _groupId; set => _groupId = value; }
    public int Version { get => _version; set => _version = value; }

    public static TransactionBuilder ATransaction() => new();

    public TransactionBuilder WithId(TransactionId id)
    {
        _id = id;
        return this;
    }

    public TransactionBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public TransactionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TransactionBuilder WithCategoryId(CategoryId categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public TransactionBuilder WithTransactionDetails(TransactionDetails details)
    {
        _transactionDetails = details;
        return this;
    }

    public TransactionBuilder WithAutoSatAmountDetails(AutoSatAmountDetails? details)
    {
        _autoSatAmountDetails = details;
        return this;
    }

    public TransactionBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public TransactionBuilder WithFixedExpense(TransactionFixedExpenseReference? fixedExpense)
    {
        _fixedExpense = fixedExpense;
        return this;
    }

    public TransactionBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public TransactionBuilder WithGroupId(GroupId? groupId)
    {
        _groupId = groupId;
        return this;
    }

    #region Transaction Type Convenience Methods

    /// <summary>
    /// Configure as a Bitcoin purchase (fiat to bitcoin).
    /// </summary>
    public TransactionBuilder AsBitcoinPurchase(long satAmount, decimal fiatAmount = 100m)
    {
        var fromFiatAccountId = new AccountId();
        var toBtcAccountId = new AccountId();
        _transactionDetails = new FiatToBitcoinDetails(
            fromFiatAccountId, toBtcAccountId, fiatAmount, satAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a Bitcoin purchase with specific account IDs.
    /// </summary>
    public TransactionBuilder AsBitcoinPurchase(AccountId fromFiatAccountId, AccountId toBtcAccountId, long satAmount, decimal fiatAmount = 100m)
    {
        _transactionDetails = new FiatToBitcoinDetails(
            fromFiatAccountId, toBtcAccountId, fiatAmount, satAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a Bitcoin sale (bitcoin to fiat).
    /// </summary>
    public TransactionBuilder AsBitcoinSale(long satAmount, decimal fiatAmount = 100m)
    {
        var fromBtcAccountId = new AccountId();
        var toFiatAccountId = new AccountId();
        _transactionDetails = new BitcoinToFiatDetails(
            fromBtcAccountId, toFiatAccountId, satAmount, fiatAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a Bitcoin sale with specific account IDs.
    /// </summary>
    public TransactionBuilder AsBitcoinSale(AccountId fromBtcAccountId, AccountId toFiatAccountId, long satAmount, decimal fiatAmount = 100m)
    {
        _transactionDetails = new BitcoinToFiatDetails(
            fromBtcAccountId, toFiatAccountId, satAmount, fiatAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as direct Bitcoin income (credit).
    /// </summary>
    public TransactionBuilder AsBitcoinIncome(long satAmount)
    {
        var btcAccountId = new AccountId();
        _transactionDetails = new BitcoinDetails(btcAccountId, satAmount, credit: true);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as direct Bitcoin income with specific account ID.
    /// </summary>
    public TransactionBuilder AsBitcoinIncome(AccountId btcAccountId, long satAmount)
    {
        _transactionDetails = new BitcoinDetails(btcAccountId, satAmount, credit: true);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as direct Bitcoin expense (debit).
    /// </summary>
    public TransactionBuilder AsBitcoinExpense(long satAmount)
    {
        var btcAccountId = new AccountId();
        _transactionDetails = new BitcoinDetails(btcAccountId, satAmount, credit: false);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as direct Bitcoin expense with specific account ID.
    /// </summary>
    public TransactionBuilder AsBitcoinExpense(AccountId btcAccountId, long satAmount)
    {
        _transactionDetails = new BitcoinDetails(btcAccountId, satAmount, credit: false);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a Bitcoin to Bitcoin transfer.
    /// </summary>
    public TransactionBuilder AsBitcoinToBitcoinTransfer(long satAmount)
    {
        var fromBtcAccountId = new AccountId();
        var toBtcAccountId = new AccountId();
        _transactionDetails = new BitcoinToBitcoinDetails(fromBtcAccountId, toBtcAccountId, satAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a Bitcoin to Bitcoin transfer with specific account IDs.
    /// </summary>
    public TransactionBuilder AsBitcoinToBitcoinTransfer(AccountId fromBtcAccountId, AccountId toBtcAccountId, long satAmount)
    {
        _transactionDetails = new BitcoinToBitcoinDetails(fromBtcAccountId, toBtcAccountId, satAmount);
        _autoSatAmountDetails = null;
        return this;
    }

    /// <summary>
    /// Configure as a fiat expense (debit).
    /// </summary>
    public TransactionBuilder AsFiatExpense(decimal amount)
    {
        var fiatAccountId = new AccountId();
        _transactionDetails = new FiatDetails(fiatAccountId, amount, credit: false);
        return this;
    }

    /// <summary>
    /// Configure as a fiat expense with specific account ID.
    /// </summary>
    public TransactionBuilder AsFiatExpense(AccountId fiatAccountId, decimal amount)
    {
        _transactionDetails = new FiatDetails(fiatAccountId, amount, credit: false);
        return this;
    }

    /// <summary>
    /// Configure as a fiat income (credit).
    /// </summary>
    public TransactionBuilder AsFiatIncome(decimal amount)
    {
        var fiatAccountId = new AccountId();
        _transactionDetails = new FiatDetails(fiatAccountId, amount, credit: true);
        return this;
    }

    /// <summary>
    /// Configure as a fiat income with specific account ID.
    /// </summary>
    public TransactionBuilder AsFiatIncome(AccountId fiatAccountId, decimal amount)
    {
        _transactionDetails = new FiatDetails(fiatAccountId, amount, credit: true);
        return this;
    }

    #endregion

    public Transaction BuildDomainObject()
    {
        return Transaction.Create(_id, _date, _name, _categoryId, _transactionDetails, _autoSatAmountDetails, _notes, _fixedExpense, _groupId, _version);
    }

    public TransactionEntity Build()
    {
        var transaction = Transaction.Create(_id, _date, _name, _categoryId, _transactionDetails, _autoSatAmountDetails, _notes, _fixedExpense, _groupId, _version);
        return transaction.AsEntity();
    }
}
