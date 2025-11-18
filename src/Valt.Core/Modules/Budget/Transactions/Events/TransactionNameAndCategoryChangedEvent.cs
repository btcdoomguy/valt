using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Categories;

namespace Valt.Core.Modules.Budget.Transactions.Events;

public sealed record TransactionNameAndCategoryChangedEvent(
    Transaction Transaction,
    TransactionName PreviousTransactionName,
    CategoryId PreviousCategoryId) : IDomainEvent;