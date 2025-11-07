using Valt.Core.Kernel;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Core.Modules.Budget.Transactions.Events;

namespace Valt.Core.Modules.Budget.Transactions;

public class Transaction : AggregateRoot<TransactionId>
{
    public DateOnly Date { get; private set; }
    public TransactionName Name { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public TransactionDetails TransactionDetails { get; private set; }

    public AutoSatAmountDetails? AutoSatAmountDetails { get; private set; }
    public bool HasAutoSatAmount => AutoSatAmountDetails is not null;
    public TransactionFixedExpenseReference? FixedExpenseReference { get; private set; }
    public string? Notes { get; }

    private Transaction(TransactionId id, DateOnly date, TransactionName name, CategoryId categoryId,
        TransactionDetails transactionDetails, AutoSatAmountDetails? autoSatAmountDetails, string? notes,
        TransactionFixedExpenseReference? fixedExpenseReference,
        int version)
    {
        //TODO: guard clauses

        Id = id;
        Date = date;
        Name = name;
        CategoryId = categoryId;
        TransactionDetails = transactionDetails;
        AutoSatAmountDetails = autoSatAmountDetails;
        Notes = notes;
        FixedExpenseReference = fixedExpenseReference;
        Version = version;

        if (Version != 0) return;
        
        AddEvent(new TransactionCreatedEvent(this));
        if (FixedExpenseReference is not null)
            AddEvent(new TransactionBoundToFixedExpenseEvent(this.Id, FixedExpenseReference));
    }

    public static Transaction New(DateOnly date, TransactionName name, CategoryId categoryId,
        TransactionDetails transactionDetails, string? notes, TransactionFixedExpenseReference? fixedExpense)
    {
        return new Transaction(new TransactionId(), date, name, categoryId, transactionDetails,
            transactionDetails.EligibleToAutoSatAmount ? AutoSatAmountDetails.Pending : null, notes, fixedExpense, 0);
    }

    public static Transaction Create(TransactionId id, DateOnly date, TransactionName name, CategoryId categoryId,
        TransactionDetails transactionDetails, AutoSatAmountDetails? autoSatAmountDetails, string? notes,
        TransactionFixedExpenseReference? fixedExpense, 
        int version)
    {
        return new Transaction(id, date, name, categoryId, transactionDetails, autoSatAmountDetails, notes, fixedExpense, version);
    }
    
    public void ChangeDate(DateOnly date)
    {
        if (Date == date)
            return;
        
        var previousDate = Date;
        Date = date;

        AddEvent(new TransactionDateChangedEvent(this, TransactionDetails, previousDate));
        AddEvent(new TransactionEditedEvent(this));
        ReprocessAutoSatAmountState();
    }
    
    public void Rename(TransactionName name)
    {
        if (Name == name)
            return;
        
        Name = name;

        AddEvent(new TransactionEditedEvent(this));
    }

    public void ChangeNameAndCategory(TransactionName name, CategoryId categoryId)
    {
        if (Name == name && CategoryId == categoryId)
            return;
        
        var previousName = Name;
        var previousCategoryId = CategoryId;

        Name = name;
        CategoryId = categoryId;
        AddEvent(new TransactionNameAndCategoryChangedEvent(this, previousName, previousCategoryId));
        AddEvent(new TransactionEditedEvent(this));
    }

    public void ChangeTransactionDetails(TransactionDetails transactionDetails)
    {
        if (TransactionDetails == transactionDetails)
            return;
        
        var previousDetails = TransactionDetails;
        TransactionDetails = transactionDetails;

        ReprocessAutoSatAmountState();

        AddEvent(new TransactionDetailsChangedEvent(this, previousDetails));
        AddEvent(new TransactionEditedEvent(this));
    }

    private void ReprocessAutoSatAmountState()
    {
        if (!TransactionDetails.EligibleToAutoSatAmount)
        {
            ChangeAutoSatAmount(null);
            return;
        }

        ChangeAutoSatAmount(AutoSatAmountDetails.Pending);
    }

    public void ChangeAutoSatAmount(AutoSatAmountDetails? autoSatAmountDetails)
    {
        if (AutoSatAmountDetails == autoSatAmountDetails)
            return;
        
        AutoSatAmountDetails = autoSatAmountDetails;

        AddEvent(new TransactionEditedEvent(this));
    }
    
    public void SetFixedExpense(TransactionFixedExpenseReference? fieldExpense)
    {
        if (FixedExpenseReference is null && fieldExpense is null)
            return;

        if (FixedExpenseReference == fieldExpense)
            return;
        
        if (FixedExpenseReference is not null)
        {
            AddEvent(new TransactionUnboundFromFixedExpenseEvent(this.Id, FixedExpenseReference));
            FixedExpenseReference = null;
        }

        if (fieldExpense is not null)
        {
            AddEvent(new TransactionBoundToFixedExpenseEvent(this.Id, fieldExpense));
            FixedExpenseReference = fieldExpense;
        }
        
        AddEvent(new TransactionEditedEvent(this));
    }
}