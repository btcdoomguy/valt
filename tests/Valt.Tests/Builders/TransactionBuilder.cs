using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Tests.Builders;

public class TransactionBuilder
{
    public TransactionId Id { get; set; } = new();
    public DateOnly Date { get; set; }
    public TransactionName Name { get; set; } = null!;
    public CategoryId CategoryId { get; set; } = null!;
    public TransactionDetails TransactionDetails { get; set; } = null!;
    public AutoSatAmountDetails AutoSatAmountDetails { get; set; } = AutoSatAmountDetails.Pending;
    public string? Notes { get; set; }
    public TransactionFixedExpenseReference? FixedExpense { get; set; }
    public int Version { get; set; } = 0;

    public Transaction BuildDomainObject()
    {
        return Transaction.Create(Id, Date, Name, CategoryId, TransactionDetails, AutoSatAmountDetails, Notes, FixedExpense, Version);
    }
    
    public TransactionEntity Build()
    {
        var transaction = Transaction.Create(Id, Date, Name, CategoryId, TransactionDetails, AutoSatAmountDetails, Notes, FixedExpense, Version);
        return transaction.AsEntity();
    }
}