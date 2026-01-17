# Budget Module

Core module for managing accounts, transactions, and categories.

## Domain Layer (Valt.Core/Modules/Budget/)

### Accounts

**Files:** `Accounts/Account.cs`, `FiatAccount.cs`, `BtcAccount.cs`

Two account types extending abstract `Account` aggregate root:

| Type | Properties | Storage |
|------|-----------|---------|
| FiatAccount | FiatCurrency, InitialAmount (FiatValue) | Decimal |
| BtcAccount | InitialAmount (BtcValue) | Satoshis (long) |

**Common Properties:**
- `Name: AccountName` - Display name with validation
- `CurrencyNickname: AccountCurrencyNickname` - Optional currency override
- `Visible: bool` - UI visibility
- `Icon: Icon` - Display icon with color
- `DisplayOrder: int` - Sort order

**Key Methods:**
- `Rename()`, `ChangeVisibility()`, `ChangeIcon()`, `ChangeDisplayOrder()`
- `ChangeInitialAmount()` - Emits `AccountInitialAmountChangedEvent`

**Events:** `AccountCreatedEvent`, `AccountUpdatedEvent`, `AccountDeletedEvent`, `AccountInitialAmountChangedEvent`

### Transactions

**File:** `Transactions/Transaction.cs`

Aggregate root for financial movements between accounts.

**Properties:**
- `Date: DateOnly` - Transaction date
- `Name: TransactionName` - Description
- `CategoryId: CategoryId` - Budget category
- `TransactionDetails: TransactionDetails` - Polymorphic transaction type
- `AutoSatAmountDetails?` - Optional auto-sat calculation
- `FixedExpenseReference?` - Link to fixed expense
- `Notes?: string` - Optional notes

**Six Transaction Detail Types** (in `Transactions/Details/`):

| Type | Description | Eligible for AutoSat |
|------|-------------|---------------------|
| FiatDetails | Single fiat credit/debit | Yes |
| BitcoinDetails | Single bitcoin credit/debit | No |
| FiatToFiatDetails | Fiat account transfer | Yes |
| FiatToBitcoinDetails | Buy bitcoin | No |
| BitcoinToBitcoinDetails | Bitcoin transfer | No |
| BitcoinToFiatDetails | Sell bitcoin | No |

**Key Methods:**
- `ChangeDate()` - Updates date, may trigger auto-sat reprocessing
- `ChangeTransactionDetails()` - Swap transaction type/accounts
- `SetFixedExpense()` - Link/unlink to fixed expense

**Auto-Sat Processing:**
- Eligible fiat transactions auto-calculate satoshi equivalents
- States: `Pending`, `Processed`, `Manual`, `Missing`

### Categories

**File:** `Categories/Category.cs`

Entity for organizing transactions with optional hierarchy.

**Properties:**
- `Name: CategoryName` - Display name
- `Icon: Icon` - Category icon
- `ParentId?: CategoryId` - Optional parent (for hierarchies)

**Constraint:** Category cannot be its own parent.

## Infrastructure Layer (Valt.Infra/Modules/Budget/)

### Repositories

| Repository | File | Key Features |
|------------|------|--------------|
| AccountRepository | `Accounts/AccountRepository.cs` | Validates no transactions before delete |
| TransactionRepository | `Transactions/TransactionRepository.cs` | Triggers price history fetch for old dates |
| CategoryRepository | `Categories/CategoryRepository.cs` | Basic CRUD with events |

### Query Objects

**IAccountQueries** - `Accounts/Queries/`
- `GetAccountSummariesAsync(showHidden)` - Returns totals with current/future values
- `GetAccountsAsync(showHidden)` - Returns account DTOs

**ITransactionQueries** - `Transactions/Queries/`
- `GetTransactionsAsync(filter)` - Filtered transaction list
- `GetTransactionNamesAsync(searchTerm)` - Autocomplete suggestions

**TransactionQueryFilter:**
```csharp
record TransactionQueryFilter(DateOnly? StartDate, DateOnly? EndDate,
    string? SearchTerm, AccountId? AccountId)
```

### Account Cache Service

**File:** `Accounts/Services/AccountCacheService.cs`

Optimizes performance by pre-calculating account totals:
- `Total` - Historical total (all transactions)
- `CurrentTotal` - Total as of specific date
- Incremental updates on date change

## UI Layer (Valt.UI/Views/Main/Tabs/Transactions/)

### TransactionsViewModel

Main tab ViewModel managing accounts, transactions, and wealth summary.

**Collections:**
- `Accounts` - List of AccountViewModel
- `FixedExpenseEntries` - Current month's fixed expenses
- `GoalEntries` - Active goals

**Wealth Summary Properties:**
- `AllWealthInSats` - BTC + SAT total
- `WealthInBtcRatio` - Percentage in Bitcoin
- `WealthInFiat` - Fiat equivalent

**Key Commands:**
- Account CRUD: `AddAccount`, `EditAccount`, `DeleteAccount`, `HideAccount`
- Fixed expense: `ManageFixedExpenses`, `OpenFixedExpense`, `MarkFixedExpenseAsPaid`
- Goal management: `AddGoal`, `EditGoal`, `DeleteGoal`

### TransactionListViewModel

Manages transaction display with search/filter/sort.

**Commands:**
- `FetchTransactions` - Query and reload
- `AddTransaction`, `EditTransaction`, `DuplicateTransaction`, `DeleteTransaction`
- `ChangeCategoryTransactions` - Bulk update

### TransactionEditorViewModel

**File:** `Modals/TransactionEditor/TransactionEditorViewModel.cs`

Modal for creating/editing transactions with mode selection (Debt/Credit/Transfer).

**Dynamic Features:**
- Form fields adapt to selected mode and account types
- Transfer rate display (BTC price or currency exchange)
- Transaction term autocomplete
- Fixed expense binding support

## Data Flow

### Query Flow
```
UI -> ITransactionQueries.GetTransactionsAsync
   -> Repository queries LiteDB
   -> Entities converted to DTOs
   -> UI maps to ViewModels
```

### Mutation Flow
```
ViewModel command -> Build TransactionDetails
   -> Transaction.New() / domain methods
   -> Repository.Save() -> Entity conversion -> LiteDB upsert
   -> Domain events published -> Event handlers react
   -> Weak messaging updates UI
```

## Value Objects

| Type | Description | File |
|------|-------------|------|
| BtcValue | Bitcoin in satoshis (long) | `Common/BtcValue.cs` |
| FiatValue | Decimal rounded to 2 decimals | `Common/FiatValue.cs` |
| FiatCurrency | 32 supported currencies | `Common/FiatCurrency.cs` |
| Icon | Name, unicode, color | `Common/Icon.cs` |

## Testing

Use builders from `tests/Valt.Tests/Builders/`:
```csharp
// Accounts
FiatAccountBuilder.AnAccount().WithName("Checking").WithFiatCurrency(FiatCurrency.Usd).Build();
BtcAccountBuilder.AnAccount().WithName("Cold Storage").Build();

// Transactions
TransactionBuilder.ATransaction().WithDate(date).WithName("Groceries").Build();

// Categories
CategoryBuilder.ACategory().WithName("Food").Build();
```