# Fixed Expenses Module

Recurring expense tracking with multiple recurrence patterns and transaction binding.

## Domain Layer (Valt.Core/Modules/Budget/FixedExpenses/)

### Core Entity: FixedExpense

**File:** `FixedExpense.cs`

Aggregate root for recurring expenses.

**Properties:**
- `Name: FixedExpenseName` - Display name (validated)
- `CategoryId: CategoryId` - Budget category
- `Enabled: bool` - Active flag
- `Ranges: IEnumerable<FixedExpenseRange>` - Price history
- `CurrentRange` - Latest range (by PeriodStart)
- `DefaultAccountId?: AccountId` - Target account (mutually exclusive with Currency)
- `Currency?: FiatCurrency` - Target currency (mutually exclusive with DefaultAccountId)
- `LastFixedExpenseRecordDate?: DateOnly` - Last recorded expense

**Constraint:** Either `DefaultAccountId` OR `Currency` must be set (exclusively).

**Key Methods:**
- `Rename()`, `SetCategory()`, `SetEnabled()`
- `SetDefaultAccountId()` - Sets account, clears currency
- `SetCurrency()` - Sets currency, clears account
- `AddRange()` - Add new price range (validates start date)

### FixedExpenseRange (Value Object)

**File:** `FixedExpenseRange.cs`

Represents a pricing period.

**Properties:**
- `FixedAmount?: FiatValue` - Single amount
- `RangedAmount?: RangedFiatValue` - Min/max range
- `Period: FixedExpensePeriods` - Recurrence type
- `PeriodStart: DateOnly` - When pricing begins
- `Day: int` - Day of month (1-31) or DayOfWeek (0-6)
- `DayOfWeek?: DayOfWeek` - Calculated for weekly periods

**Factory Methods:**
```csharp
CreateFixedAmount(amount, period, start, day/dayOfWeek)
CreateRangedAmount(range, period, start, day/dayOfWeek)
```

### Enums

**FixedExpensePeriods:**
- `Monthly = 0` - Day is 1-31
- `Yearly = 1` - Day is 1-31, only fires in matching month
- `Weekly = 2` - Day is DayOfWeek (0-6)
- `Biweekly = 3` - Day is DayOfWeek, every 2 weeks

**FixedExpenseRecordState:**
- `Empty = 0` - No action taken
- `Paid = 1` - Linked to transaction
- `ManuallyPaid = 2` - Marked paid without transaction
- `Ignored = 3` - Skipped

### Repository Contract

**IFixedExpenseRepository:**
- `GetFixedExpenseByIdAsync()`, `SaveFixedExpenseAsync()`, `DeleteFixedExpenseAsync()`

**IFixedExpenseRecordService:**
- `BindFixedExpenseToTransactionAsync()` / `UnbindFixedExpenseFromTransactionAsync()`
- `IgnoreFixedExpenseAsync()` / `UndoIgnoreFixedExpenseAsync()`
- `MarkFixedExpenseAsPaidAsync()` / `UnmarkFixedExpenseAsPaidAsync()`

## Infrastructure Layer (Valt.Infra/Modules/Budget/FixedExpenses/)

### Database Entities

**FixedExpenseEntity:**
- `Name`, `CategoryId`, `DefaultAccountId`, `Currency`
- `Ranges: List<FixedExpenseRangeEntity>` - Nested collection
- `Enabled`, `Version`

**FixedExpenseRangeEntity:**
- `FixedAmount`, `RangedAmountMin`, `RangedAmountMax`
- `PeriodId`, `PeriodStart`, `Day`

**FixedExpenseRecordEntity:**
- `FixedExpense` (BsonRef) - Parent expense
- `ReferenceDate` - The specific date
- `Transaction?` (BsonRef) - Linked transaction
- `FixedExpenseRecordStateId` - Current state

### FixedExpenseProvider

**File:** `FixedExpenseProvider.cs`

Core method: `GetFixedExpensesOfMonthAsync(DateOnly date)`

**Algorithm:**
1. Load enabled fixed expenses and ranges
2. Determine applicable ranges for month
3. Calculate dates based on period type:
   - **Monthly/Yearly:** Day-of-month, validate month for yearly
   - **Weekly/Biweekly:** Calculate all DayOfWeek occurrences
4. Adjust day if exceeds month max (e.g., 31 -> 28 for Feb)
5. Fetch FixedExpenseRecord data for state
6. Return sorted entries by day

### FixedExpenseProviderEntry (Output)

**File:** `FixedExpenseProviderEntry.cs`

```csharp
public record FixedExpenseProviderEntry {
    // Identity
    string Id, Name, CategoryId?, ReferenceDate, DefaultAccountId?

    // Amounts
    decimal? FixedAmount, RangedAmountMin, RangedAmountMax
    string Currency
    decimal MinimumAmount, MaximumAmount  // Calculated

    // State
    string? TransactionId
    FixedExpenseRecordState State
    bool Paid, Ignored, MarkedAsPaid, Empty  // Convenience
}
```

### Query Service

**IFixedExpenseQueries:**
- `GetFixedExpenseAsync(id)` - Single DTO with formatted amounts
- `GetFixedExpensesAsync()` - All DTOs
- `GetFixedExpenseNamesAsync()` - Simple names list
- `GetFixedExpenseHistoryAsync(id)` - Transaction + price history

### Record Service

**File:** `Services/FixedExpenseRecordService.cs`

Manages FixedExpenseRecord lifecycle:
- **Bind:** Create record with Paid state + Transaction ref
- **Unbind:** Delete matching record (date range query for UTC safety)
- **Ignore:** Create record with Ignored state
- **Mark as Paid:** Create record with ManuallyPaid state

### Event Handler

**File:** `Handlers/UpdateFixedExpenseRecordsEventHandler.cs`

Reacts to:
- `TransactionBoundToFixedExpenseEvent` -> Bind
- `TransactionUnboundFromFixedExpenseEvent` -> Unbind
- `FixedExpenseDeletedEvent` -> Unbind all
- `TransactionDeletedEvent` -> Delete related records

## UI Layer (Valt.UI/Views/Main/Modals/)

### FixedExpenseEditorViewModel

**File:** `FixedExpenseEditor/FixedExpenseEditorViewModel.cs`

Modal for creating/editing fixed expenses.

**Form Properties:**
- `Name` (required), `Category` (required)
- `IsAttachedToDefaultAccount` / `IsAttachedToCurrency` - Mutually exclusive
- `DefaultAccount` - Fiat accounts only
- `Currency` - Available currencies
- `IsFixedAmount` / `IsVariableAmount` - Amount type
- `FixedAmount`, `RangedAmountMin`, `RangedAmountMax`
- `Period`, `Day`, `PeriodStart`, `Enabled`

**Custom Validators:**
- `[RequiredIfAttachedToDefaultAccount]`, `[RequiredIfAttachedToCurrency]`
- `[RequiredIfFixedAmount]`, `[RequiredIfVariableAmount]`
- `[RangedAmountMinLessThanMax]`
- `[ValidPeriodStartForExistingExpense]` - Prevents start before last record

**Edit Mode:**
- Separate "change recurrence" mode
- Locks recurrence info in view-only
- Can cancel to restore original values

### ManageFixedExpensesViewModel

**File:** `ManageFixedExpenses/ManageFixedExpensesViewModel.cs`

Main CRUD interface.

**Features:**
- List of FixedExpenseListItemViewModel
- Monthly/yearly totals calculation (converted to main currency)

**Commands:**
- `AddFixedExpense`, `EditFixedExpense`, `ViewHistory`, `DeleteFixedExpense`

### FixedExpenseHistoryViewModel

**File:** `FixedExpenseHistory/FixedExpenseHistoryViewModel.cs`

Displays expense history:
- Transaction history (date, name, amount, account)
- Price history (period, amount, day)

### FixedExpensesEntryViewModel (Tab Display)

**File:** `Tabs/Transactions/Models/FixedExpensesEntryViewModel.cs`

Wraps `FixedExpenseProviderEntry` for Transactions tab.

**Display Properties:**
- `AmountDisplay` - Formatted (fixed or ranged)
- `DayFormatted` - Padded to 2 digits
- `IsLateOrCurrentDay` - Compares to current date
- `TextColor` - Gray if paid/ignored, red if late

## Transaction Integration

**TransactionFixedExpenseReference:**
```csharp
record TransactionFixedExpenseReference(FixedExpenseId FixedExpenseId, DateOnly ReferenceDate)
```

**Events:**
- `TransactionBoundToFixedExpenseEvent`
- `TransactionUnboundFromFixedExpenseEvent`

## Key Patterns

### Mutually Exclusive Account/Currency
- Either `DefaultAccountId` OR `Currency` (never both)
- Setters clear the other field

### Price Range History
- Multiple ranges can coexist
- `CurrentRange` returns latest by PeriodStart
- Adding new range validates start date > LastFixedExpenseRecordDate

### Recurrence Calculation
- Monthly: Day-of-month (1-31)
- Yearly: Day-of-month, specific month
- Weekly: All occurrences of DayOfWeek
- Biweekly: Every other occurrence of DayOfWeek

### State Tracking
- FixedExpenseRecord for each date tracks:
  - Paid (with transaction link)
  - ManuallyPaid (no transaction)
  - Ignored (skipped)
  - Empty (awaiting action)

## Testing

Use builder from `tests/Valt.Tests/Builders/FixedExpenseBuilder.cs`:

```csharp
FixedExpenseBuilder.AFixedExpense().Build();
FixedExpenseBuilder.AFixedExpenseWithAccount(accountId)
    .WithName("Rent")
    .WithFixedAmountRange(1000m, Monthly, new DateOnly(2025, 1, 1), 5)
    .Build();
FixedExpenseBuilder.AFixedExpenseWithCurrency(FiatCurrency.Usd).Build();
```

## File Structure

```
src/Valt.Core/Modules/Budget/FixedExpenses/
├── FixedExpense.cs (Aggregate Root)
├── FixedExpenseId.cs, FixedExpenseName.cs (Value Objects)
├── FixedExpensePeriods.cs, FixedExpenseRecordState.cs (Enums)
├── FixedExpenseRange.cs (Value Object)
├── Events/
│   └── FixedExpense*Event.cs
└── Contracts/
    └── IFixedExpenseRepository.cs, IFixedExpenseRecordService.cs

src/Valt.Infra/Modules/Budget/FixedExpenses/
├── FixedExpenseEntity.cs, FixedExpenseRangeEntity.cs
├── FixedExpenseRecordEntity.cs
├── FixedExpenseRepository.cs
├── FixedExpenseProvider.cs, FixedExpenseProviderEntry.cs
├── Extensions.cs
├── Handlers/
│   └── UpdateFixedExpenseRecordsEventHandler.cs
├── Services/
│   └── FixedExpenseRecordService.cs
└── Queries/
    └── FixedExpenseQueries.cs, DTOs/

src/Valt.UI/Views/Main/Modals/
├── FixedExpenseEditor/
├── FixedExpenseHistory/
└── ManageFixedExpenses/
```
