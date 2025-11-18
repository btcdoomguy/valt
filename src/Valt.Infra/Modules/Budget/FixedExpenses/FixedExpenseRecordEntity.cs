using LiteDB;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public class FixedExpenseRecordEntity
{
    [BsonId] public ObjectId Id { get; set; } = null!;
    
    [BsonRef("budget_fixedexpenses")]
    public required FixedExpenseEntity FixedExpense { get; set; }
    
    [BsonField("dt")] public DateTime ReferenceDate { get; set; }
    
    [BsonRef("budget_transactions")]
    public TransactionEntity? Transaction { get; set; }
    
    [BsonField("st")]
    public int FixedExpenseRecordStateId { get; set; }
}