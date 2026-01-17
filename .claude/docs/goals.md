# Goals Module

Financial goal tracking with automatic progress calculation.

## Domain Layer (Valt.Core/Modules/Goals/)

### Core Entity: Goal

**File:** `Goal.cs`

Aggregate root for managing financial goals.

**Properties:**
- `RefDate: DateOnly` - Reference month/year
- `Period: GoalPeriods` - Monthly or Yearly
- `GoalType: IGoalType` - Polymorphic goal configuration
- `Progress: decimal` - 0-100% completion
- `IsUpToDate: bool` - Stale flag for recalculation
- `State: GoalStates` - Open, Completed, MarkedAsCompleted, Closed

**Key Methods:**
- `Goal.New()` - Create new goal
- `UpdateProgress(progress, goalType, timestamp)` - Update calculated progress
- `MarkAsStale()` - Flag for recalculation
- `MarkAsCompleted()`, `Close()`, `Reopen()`, `Conclude()` - State transitions
- `GetPeriodRange()` - Calculate date range for period

### Goal Types (IGoalType implementations)

All types are sealed classes in `GoalTypes/`:

| Type | Target | Calculated | RequiresPriceData | Description |
|------|--------|------------|-------------------|-------------|
| StackBitcoinGoalType | `TargetSats` | `CalculatedSats` | `false` | Accumulate satoshis |
| SpendingLimitGoalType | `TargetAmount`, `Currency` | `CalculatedSpending` | `false` | Cap fiat spending |
| DcaGoalType | `TargetPurchaseCount` | `CalculatedPurchaseCount` | `false` | DCA purchases |
| IncomeFiatGoalType | `TargetAmount`, `Currency` | `CalculatedIncome` | `false` | Earn fiat target |
| IncomeBtcGoalType | `TargetSats` | `CalculatedSats` | `false` | Earn bitcoin target |
| ReduceExpenseCategoryGoalType | `TargetAmount`, `CategoryId` | `CalculatedSpending` | **`true`** | Reduce category spending |
| BitcoinHodlGoalType | `MaxSellableSats` | `CalculatedSoldSats` | `false` | Limit bitcoin sales |

**IGoalType Interface:**
```csharp
public interface IGoalType {
    GoalTypeNames TypeName { get; }
    bool RequiresPriceDataForCalculation { get; }  // Only true for ReduceExpenseCategory
}
```

**Immutability Pattern:**
All types use `With*()` builder methods:
```csharp
var updated = goalType.WithCalculatedSats(newValue);
```

### Enums

**GoalPeriods:** `Monthly = 0`, `Yearly = 1`

**GoalStates:** `Open`, `Completed`, `MarkedAsCompleted`, `Closed`

**GoalTypeNames:** `StackBitcoin`, `SpendingLimit`, `Dca`, `IncomeFiat`, `IncomeBtc`, `ReduceExpenseCategory`, `BitcoinHodl`

## Infrastructure Layer (Valt.Infra/Modules/Goals/)

### Database Entity

**GoalEntity** - LiteDB storage with JSON-serialized goal type:
- `RefDate`, `PeriodId`, `GoalTypeNameId`
- `GoalTypeJson` - Polymorphic serialization
- `Progress`, `IsUpToDate`, `LastUpdatedAt`, `StateId`

**GoalTypeDtos** - Serialization DTOs with `[JsonPropertyName]` attributes for each goal type.

### Progress Calculator System

**Strategy pattern** for calculating goal progress.

**Interface:** `IGoalProgressCalculator`
```csharp
GoalTypeNames SupportedType { get; }
Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input);
```

**Output:** `GoalProgressResult(decimal Progress, IGoalType UpdatedGoalType)`

#### Calculator Implementations

| Calculator | Logic |
|------------|-------|
| StackBitcoinProgressCalculator | FiatToBitcoin + Bitcoin income - BitcoinToFiat - Bitcoin expenses |
| SpendingLimitProgressCalculator | Fiat expenses + FiatToBitcoin amounts |
| DcaProgressCalculator | Count of FiatToBitcoin transactions |
| IncomeBtcProgressCalculator | Bitcoin transactions with positive FromSatAmount |
| IncomeFiatProgressCalculator | Positive Fiat + BitcoinToFiat amounts |
| BitcoinHodlProgressCalculator | `100 - (soldSats / maxSellable * 100)` |
| ReduceExpenseCategoryProgressCalculator | Category expenses with multi-currency conversion |

**GoalProgressCalculatorFactory** - Resolves calculator by `GoalTypeNames`

### Background Job

**GoalProgressUpdaterJob** - Runs every 5 seconds

**Process:**
1. Retrieves stale goals from `IGoalQueries`
2. Gets appropriate calculator from factory
3. Calculates progress asynchronously
4. Updates goal with new progress
5. Publishes `GoalProgressUpdated` message

### Event Handlers

**MarkGoalsStaleEventHandler:**
- Listens to: `TransactionCreatedEvent`, `TransactionEditedEvent`, `TransactionDeletedEvent`
- Marks goals stale for affected dates

**MarkGoalsStaleOnPriceUpdateHandler:**
- Listens to: `FiatHistoryPriceUpdatedMessage`, `BitcoinHistoryPriceUpdatedMessage`
- Only marks goals stale where `GoalType.RequiresPriceDataForCalculation == true`
- Currently only `ReduceExpenseCategoryGoalType` requires price data for multi-currency conversion

## UI Layer (Valt.UI/Views/Main/Modals/ManageGoal/)

### ManageGoalViewModel

Main modal for creating/editing goals.

**Properties:**
- `SelectedPeriod` - Monthly/Yearly dropdown
- `SelectedMonth`, `SelectedYear` - Date selection
- `SelectedGoalType` - Goal type dropdown
- `CurrentGoalTypeEditor` - Dynamic editor VM
- `IsEditMode` - Boolean flag

### Goal Type Editors

Each goal type has a dedicated editor implementing `IGoalTypeEditorViewModel`:

```csharp
public interface IGoalTypeEditorViewModel {
    IGoalType CreateGoalType();
    IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing);
    void LoadFrom(IGoalType goalType);
}
```

**Editors:**
- `StackBitcoinGoalTypeEditorViewModel` - BtcValue input
- `SpendingLimitGoalTypeEditorViewModel` - FiatValue + Currency
- `DcaGoalTypeEditorViewModel` - Integer count
- `IncomeFiatGoalTypeEditorViewModel` - FiatValue + Currency
- `IncomeBtcGoalTypeEditorViewModel` - BtcValue input
- `BitcoinHodlGoalTypeEditorViewModel` - BtcValue (0 = full HODL)
- `ReduceExpenseCategoryGoalTypeEditorViewModel` - FiatValue + Category dropdown

## Key Patterns

### Staleness-Based Invalidation
1. Goals marked stale on transaction changes (all goal types)
2. Background job recalculates only stale goals
3. Price updates only trigger recalculation for goals with `RequiresPriceDataForCalculation == true`

### Polymorphism via IGoalType
- Domain layer doesn't use serialization attributes
- Infrastructure DTOs handle JSON serialization
- Type discrimination via `TypeName` enum

### MVVM with Dynamic Views
- `ManageGoalViewModel` dynamically creates editors
- Preserves calculated values during edits

## Testing

Use builder from `tests/Valt.Tests/Builders/GoalBuilder.cs`:

```csharp
GoalBuilder.AGoal().Build();
GoalBuilder.AStackBitcoinGoal(targetSats: 1_000_000).Build();
GoalBuilder.AMonthlyGoal().Build();
GoalBuilder.AYearlyGoal().Build();
```

## Data Flow Example

**Stack Bitcoin Goal:**

1. **User creates goal:** "Stack 1 BTC this month"
   - Editor creates `StackBitcoinGoalType(100_000_000 sats, 0)`
   - `Goal.New()` creates domain object

2. **Transaction added:** User buys 0.1 BTC
   - `TransactionCreatedEvent` emitted
   - `MarkGoalsStaleEventHandler` marks goal stale

3. **Progress recalculation:**
   - `StackBitcoinProgressCalculator` sums all purchases
   - Progress: (10M / 100M) * 100 = 10%
   - Creates updated `StackBitcoinGoalType(100M, 10M)`

4. **Goal updates:**
   - `goal.UpdateProgress(10, updatedGoalType, now)`
   - `IsUpToDate = true`
   - UI receives `GoalProgressUpdated` message

## File Structure

```
src/Valt.Core/Modules/Goals/
├── Goal.cs (Aggregate Root)
├── GoalId.cs, GoalPeriods.cs, GoalStates.cs, GoalTypeNames.cs
├── IGoalType.cs (Interface)
├── GoalTypes/
│   ├── StackBitcoinGoalType.cs
│   ├── SpendingLimitGoalType.cs
│   ├── DcaGoalType.cs
│   ├── IncomeFiatGoalType.cs
│   ├── IncomeBtcGoalType.cs
│   ├── ReduceExpenseCategoryGoalType.cs
│   └── BitcoinHodlGoalType.cs
├── Contracts/
│   └── IGoalRepository.cs
└── Events/
    └── GoalCreatedEvent.cs, GoalUpdatedEvent.cs

src/Valt.Infra/Modules/Goals/
├── GoalEntity.cs, GoalTypeDtos.cs
├── Extensions.cs, GoalRepository.cs
├── GoalProgressUpdaterJob.cs
├── Handlers/
│   ├── MarkGoalsStaleEventHandler.cs
│   └── MarkGoalsStaleOnPriceUpdateHandler.cs
├── ProgressCalculators/
│   ├── IGoalProgressCalculator.cs
│   ├── GoalProgressCalculatorFactory.cs
│   └── *ProgressCalculator.cs (7 implementations)
└── Queries/
    └── GoalQueries.cs, DTOs/

src/Valt.UI/Views/Main/Modals/ManageGoal/
├── ManageGoalViewModel.cs, ManageGoalView.axaml
├── IGoalTypeEditorViewModel.cs
└── GoalTypeEditors/
    └── *GoalTypeEditorViewModel.cs (7 editors + views)
```
