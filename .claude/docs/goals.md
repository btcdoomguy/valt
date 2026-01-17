# Goals Module

Financial goal tracking with automatic progress calculation and state transitions.

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
- `State: GoalStates` - Open, Completed, or Failed

**Key Methods:**
- `Goal.New()` - Create new goal (emits `GoalCreatedEvent`)
- `UpdateProgress(progress, goalType, timestamp)` - Update calculated progress, auto-transitions state at 100%
- `MarkAsStale()` - Flag for recalculation (emits `GoalUpdatedEvent`)
- `Recalculate()` - Reset to Open state for recalculation (from Completed or Failed)
- `GetPeriodRange()` - Calculate date range for period

### Goal Types (IGoalType implementations)

All types are sealed classes in `GoalTypes/`:

| Type | ProgressionMode | Target | Calculated | RequiresPriceData | Description |
|------|-----------------|--------|------------|-------------------|-------------|
| StackBitcoinGoalType | ZeroToSuccess | `TargetSats` | `CalculatedSats` | `false` | Accumulate satoshis |
| DcaGoalType | ZeroToSuccess | `TargetPurchaseCount` | `CalculatedPurchaseCount` | `false` | DCA purchases |
| IncomeFiatGoalType | ZeroToSuccess | `TargetAmount` | `CalculatedIncome` | `false` | Earn fiat target |
| IncomeBtcGoalType | ZeroToSuccess | `TargetSats` | `CalculatedSats` | `false` | Earn bitcoin target |
| SpendingLimitGoalType | DecreasingSuccess | `TargetAmount` | `CalculatedSpending` | `false` | Cap fiat spending |
| ReduceExpenseCategoryGoalType | DecreasingSuccess | `TargetAmount`, `CategoryId` | `CalculatedSpending` | **`true`** | Reduce category spending |
| BitcoinHodlGoalType | DecreasingSuccess | `MaxSellableSats` | `CalculatedSoldSats` | `false` | Limit bitcoin sales |

**IGoalType Interface:**
```csharp
public interface IGoalType {
    GoalTypeNames TypeName { get; }
    bool RequiresPriceDataForCalculation { get; }  // Only true for ReduceExpenseCategory
    ProgressionMode ProgressionMode { get; }       // ZeroToSuccess or DecreasingSuccess
}
```

**Immutability Pattern:**
All types use `With*()` builder methods:
```csharp
var updated = goalType.WithCalculatedSats(newValue);
```

### Enums

**GoalPeriods:** `Monthly = 0`, `Yearly = 1`

**GoalStates:** `Open`, `Completed`, `Failed`

**GoalTypeNames:** `StackBitcoin`, `SpendingLimit`, `Dca`, `IncomeFiat`, `IncomeBtc`, `ReduceExpenseCategory`, `BitcoinHodl`

**ProgressionMode:** `ZeroToSuccess`, `DecreasingSuccess`

### Progression Modes and State Transitions

Goals automatically transition state when progress reaches 100%:

| ProgressionMode | Progress Direction | At 100% | UI Color |
|-----------------|-------------------|---------|----------|
| ZeroToSuccess | 0% → 100% (good) | Completed | Green |
| DecreasingSuccess | 0% → 100% (bad) | Failed | Red |

**ZeroToSuccess** (stacking, DCA, income goals):
- Progress starts at 0% and increases as you make progress
- At 100%, goal automatically transitions to `Completed`
- Green progress bar

**DecreasingSuccess** (spending limits, hodl goals):
- Progress starts at 0% and increases as you spend/sell
- At 100%, goal automatically transitions to `Failed`
- Red progress bar

### Domain Events

**Files:** `Events/GoalCreatedEvent.cs`, `Events/GoalUpdatedEvent.cs`, `Events/GoalDeletedEvent.cs`

- `GoalCreatedEvent` - Emitted when a new goal is created
- `GoalUpdatedEvent` - Emitted when goal is updated (progress, state, staleness)
- `GoalDeletedEvent` - Emitted from repository when goal is deleted

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

| Calculator | ProgressionMode | Logic |
|------------|-----------------|-------|
| StackBitcoinProgressCalculator | ZeroToSuccess | FiatToBitcoin + Bitcoin income - BitcoinToFiat - Bitcoin expenses |
| DcaProgressCalculator | ZeroToSuccess | Count of FiatToBitcoin transactions |
| IncomeBtcProgressCalculator | ZeroToSuccess | Bitcoin transactions with positive FromSatAmount |
| IncomeFiatProgressCalculator | ZeroToSuccess | Positive Fiat + BitcoinToFiat amounts |
| SpendingLimitProgressCalculator | DecreasingSuccess | `(spent / limit) * 100` - 0% = nothing spent, 100% = at/over limit |
| ReduceExpenseCategoryProgressCalculator | DecreasingSuccess | Category expenses, 0% = nothing spent, 100% = at/over limit |
| BitcoinHodlProgressCalculator | DecreasingSuccess | `(soldSats / maxSellable) * 100` - 0% = no sales, 100% = limit reached |

**GoalProgressCalculatorFactory** - Resolves calculator by `GoalTypeNames`

### Background Job & State Management

**GoalProgressState** (`Services/GoalProgressState.cs`):
- In-memory flag-based state for efficient job triggering
- `HasStaleGoals` - Flag indicating stale goals need recalculation
- `BootstrapCompleted` - Flag for initial load completion
- `MarkAsStale()` - Set flag when goals need recalculation
- `ClearStaleFlag()` - Clear flag after processing

**GoalProgressUpdaterJob** - Runs every 1 second (flag-based, not polling DB)

**Process:**
1. On bootstrap: Queries DB for any stale goals, marks `BootstrapCompleted`
2. After bootstrap: Only runs when `HasStaleGoals` flag is set
3. Retrieves stale goals from `IGoalQueries`
4. Gets appropriate calculator from factory
5. Calculates progress asynchronously
6. Updates goal with new progress (auto-transitions state at 100%)
7. Publishes `GoalProgressUpdated` message
8. Clears stale flag

### Event Handlers

**GoalEventHandler:**
- Listens to: `GoalCreatedEvent`, `GoalUpdatedEvent`, `GoalDeletedEvent`
- Marks `GoalProgressState` as stale to trigger recalculation

**MarkGoalsStaleEventHandler:**
- Listens to: `TransactionCreatedEvent`, `TransactionEditedEvent`, `TransactionDeletedEvent`
- Marks goals stale for affected dates
- Marks `GoalProgressState` as stale

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

### Goal Display (GoalEntryViewModel)

**State Display:**
- `ShowSuccessIcon` - Shows "SUCCESS" badge (green background) for Completed goals
- `ShowFailedIcon` - Shows "FAILED" badge (red background) for Failed goals
- `ShowProgressBar` - Shows progress bar only for Open state

**Progress Bar Styling:**
- `IsZeroToSuccess` - Green progress bar (`.complete` class)
- `IsDecreasingSuccess` - Red progress bar (`.danger` class)

**Context Menu:**
- `CanRecalculate` - Available for Completed or Failed goals (resets to Open)

## Key Patterns

### Flag-Based Staleness Invalidation
1. Event handlers call `GoalProgressState.MarkAsStale()`
2. Background job checks flag every 1 second (efficient, no DB polling)
3. Only processes when flag is set
4. Price updates only trigger for goals with `RequiresPriceDataForCalculation == true`

### Automatic State Transitions
1. Progress updated via `UpdateProgress()`
2. At 100%:
   - ZeroToSuccess goals → `Completed`
   - DecreasingSuccess goals → `Failed`
3. `Recalculate()` resets to `Open` for manual re-evaluation

### Polymorphism via IGoalType
- Domain layer doesn't use serialization attributes
- Infrastructure DTOs handle JSON serialization
- Type discrimination via `TypeName` enum
- `ProgressionMode` determines progress direction and auto-transition

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
GoalBuilder.AGoal().WithState(GoalStates.Failed).Build();
GoalBuilder.AGoal().WithGoalType(new SpendingLimitGoalType(1000m)).Build();
```

## Data Flow Example

**Stack Bitcoin Goal (ZeroToSuccess):**

1. **User creates goal:** "Stack 1 BTC this month"
   - Editor creates `StackBitcoinGoalType(100_000_000 sats, 0)`
   - `Goal.New()` creates domain object, emits `GoalCreatedEvent`
   - `GoalEventHandler` marks `GoalProgressState` as stale

2. **Transaction added:** User buys 0.1 BTC
   - `TransactionCreatedEvent` emitted
   - `MarkGoalsStaleEventHandler` marks goal stale in DB
   - `GoalProgressState.MarkAsStale()` called

3. **Progress recalculation:**
   - `GoalProgressUpdaterJob` detects `HasStaleGoals` flag
   - `StackBitcoinProgressCalculator` sums all purchases
   - Progress: (10M / 100M) * 100 = 10%
   - Creates updated `StackBitcoinGoalType(100M, 10M)`

4. **Goal updates:**
   - `goal.UpdateProgress(10, updatedGoalType, now)`
   - At 100%: auto-transitions to `Completed`
   - `IsUpToDate = true`
   - UI receives `GoalProgressUpdated` message

**Spending Limit Goal (DecreasingSuccess):**

1. **User creates goal:** "Limit spending to $1000 this month"
   - Progress starts at 0% (nothing spent = good)

2. **User spends $500:**
   - Progress: (500 / 1000) * 100 = 50%
   - Red progress bar at 50%

3. **User reaches limit ($1000 spent):**
   - Progress: 100%
   - Auto-transitions to `Failed`
   - Shows "FAILED" badge

## File Structure

```
src/Valt.Core/Modules/Goals/
├── Goal.cs (Aggregate Root)
├── GoalId.cs, GoalPeriods.cs, GoalStates.cs, GoalTypeNames.cs
├── ProgressionMode.cs
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
    ├── GoalCreatedEvent.cs
    ├── GoalUpdatedEvent.cs
    └── GoalDeletedEvent.cs

src/Valt.Infra/Modules/Goals/
├── GoalEntity.cs, GoalTypeDtos.cs
├── Extensions.cs, GoalRepository.cs
├── Services/
│   ├── GoalProgressState.cs
│   ├── GoalProgressUpdaterJob.cs
│   ├── IGoalProgressCalculator.cs
│   ├── GoalProgressCalculatorFactory.cs
│   └── *ProgressCalculator.cs (7 implementations)
├── Handlers/
│   ├── GoalEventHandler.cs
│   ├── MarkGoalsStaleEventHandler.cs
│   └── MarkGoalsStaleOnPriceUpdateHandler.cs
└── Queries/
    └── GoalQueries.cs, DTOs/

src/Valt.UI/Views/Main/Modals/ManageGoal/
├── ManageGoalViewModel.cs, ManageGoalView.axaml
├── IGoalTypeEditorViewModel.cs
└── GoalTypeEditors/
    └── *GoalTypeEditorViewModel.cs (7 editors + views)
```
