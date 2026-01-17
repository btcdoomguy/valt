# Goal Types Implementation Roadmap

This document outlines the implemented goal types and their behavior. All goal types have been implemented.

---

## Progression Modes

Goals use a `ProgressionMode` enum to determine progress direction and state transitions:

| Mode | Progress Direction | At 100% | UI Color |
|------|-------------------|---------|----------|
| **ZeroToSuccess** | 0% → 100% (good) | `Completed` | Green |
| **DecreasingSuccess** | 0% → 100% (bad) | `Failed` | Red |

---

## 1. StackBitcoinGoalType ✅

**Purpose**: Accumulate satoshis over a period.

**ProgressionMode**: `ZeroToSuccess`

### Domain Model

```csharp
public class StackBitcoinGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.StackBitcoin;
    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public BtcValue TargetAmount { get; }         // Target sats to accumulate
    public long CalculatedSats { get; }           // Actual sats accumulated
}
```

### Progress Calculation Logic

- Query all bitcoin transactions in the period
- Sum: FiatToBitcoin + Bitcoin income - BitcoinToFiat - Bitcoin expenses
- Progress = (calculated / target) * 100, capped at 100
- **0% = nothing accumulated, 100% = goal reached (Completed)**

---

## 2. SpendingLimitGoalType ✅

**Purpose**: Track that you don't exceed a spending limit in a period.

**ProgressionMode**: `DecreasingSuccess`

### Domain Model

```csharp
public class SpendingLimitGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SpendingLimit;
    public ProgressionMode ProgressionMode => ProgressionMode.DecreasingSuccess;

    public decimal TargetAmount { get; }          // Maximum allowed spending
    public decimal CalculatedSpending { get; }    // Actual spending
}
```

### Progress Calculation Logic

- Query all expense transactions in the period
- Sum the absolute values of expenses
- Progress = (spent / limit) * 100, capped at 100
- **0% = nothing spent (good), 100% = at/over limit (Failed)**

### Transaction Types to Include

- `FiatDetails` with negative amount (expense)
- `FiatToBitcoinDetails` (buying bitcoin is spending fiat)

---

## 3. DcaGoalType ✅

**Purpose**: Ensure consistent bitcoin purchases regardless of price.

**ProgressionMode**: `ZeroToSuccess`

### Domain Model

```csharp
public class DcaGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.Dca;
    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public int TargetPurchaseCount { get; }       // Number of purchases to make
    public int CalculatedPurchaseCount { get; }   // Actual purchases made
}
```

### Progress Calculation Logic

- Query all `FiatToBitcoin` transactions in the period
- Count the number of transactions
- Progress = (count / target) * 100, capped at 100
- **0% = no purchases, 100% = target reached (Completed)**

---

## 4. IncomeFiatGoalType ✅

**Purpose**: Track fiat income targets for a period.

**ProgressionMode**: `ZeroToSuccess`

### Domain Model

```csharp
public class IncomeFiatGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.IncomeFiat;
    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public decimal TargetAmount { get; }          // Target income
    public decimal CalculatedIncome { get; }      // Actual income
}
```

### Progress Calculation Logic

- Query all positive fiat transactions + BitcoinToFiat in the period
- Sum the values
- Progress = (income / target) * 100, capped at 100
- **0% = no income, 100% = target reached (Completed)**

---

## 5. IncomeBtcGoalType ✅

**Purpose**: Track bitcoin income targets for a period.

**ProgressionMode**: `ZeroToSuccess`

### Domain Model

```csharp
public class IncomeBtcGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.IncomeBtc;
    public ProgressionMode ProgressionMode => ProgressionMode.ZeroToSuccess;

    public BtcValue TargetAmount { get; }         // Target sats
    public long CalculatedSats { get; }           // Actual sats received
}
```

### Progress Calculation Logic

- Query all bitcoin transactions with positive FromSatAmount
- Sum the values
- Progress = (income / target) * 100, capped at 100
- **0% = no income, 100% = target reached (Completed)**

---

## 6. ReduceExpenseCategoryGoalType ✅

**Purpose**: Reduce spending in a specific category.

**ProgressionMode**: `DecreasingSuccess`

**RequiresPriceData**: `true` (for multi-currency conversion)

### Domain Model

```csharp
public class ReduceExpenseCategoryGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.ReduceExpenseCategory;
    public ProgressionMode ProgressionMode => ProgressionMode.DecreasingSuccess;
    public bool RequiresPriceDataForCalculation => true;

    public decimal TargetAmount { get; }          // Maximum for this category
    public string CategoryId { get; }             // Target category
    public string CategoryName { get; }           // Display name
    public decimal CalculatedSpending { get; }    // Actual spending
}
```

### Progress Calculation Logic

- Query all expense transactions in the period for the specific category
- Convert to main fiat currency using historical prices
- Sum absolute values
- Progress = (spent / limit) * 100, capped at 100
- **0% = nothing spent in category (good), 100% = at/over limit (Failed)**

### Special Considerations

- Requires price data for multi-currency conversion
- Goals with this type are marked stale when price data updates
- Shows category name in the goal entry display

---

## 7. BitcoinHodlGoalType ✅

**Purpose**: Track that you don't sell bitcoin (diamond hands challenge).

**ProgressionMode**: `DecreasingSuccess`

### Domain Model

```csharp
public class BitcoinHodlGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.BitcoinHodl;
    public ProgressionMode ProgressionMode => ProgressionMode.DecreasingSuccess;

    public long MaxSellableSats { get; }          // 0 = no sales allowed, or max sats to sell
    public long CalculatedSoldSats { get; }       // Actual sats sold
}
```

### Progress Calculation Logic

- Query all `BitcoinToFiat` transactions in the period
- Sum the absolute sat amounts sold
- If MaxSellableSats == 0: Progress = soldSats == 0 ? 0 : 100 (full HODL mode)
- If MaxSellableSats > 0: Progress = (sold / max) * 100, capped at 100
- **0% = no sales (good), 100% = exceeded allowed sales (Failed)**

---

## State Transitions

Goals automatically transition based on progress and progression mode:

```
                    ZeroToSuccess                    DecreasingSuccess
                         │                                   │
    Progress < 100%:   Open                               Open
                         │                                   │
    Progress >= 100%: Completed                           Failed
                         │                                   │
    User Recalculate:  Open                               Open
```

### Recalculate Action

- Available for `Completed` or `Failed` goals
- Resets state to `Open`
- Marks goal as stale for recalculation
- Useful when user wants to re-evaluate after editing transactions

---

## Implementation Summary

| Goal Type | ProgressionMode | Progress Bar | At 100% | Status |
|-----------|-----------------|--------------|---------|--------|
| StackBitcoin | ZeroToSuccess | Green | Completed | ✅ |
| DCA | ZeroToSuccess | Green | Completed | ✅ |
| IncomeFiat | ZeroToSuccess | Green | Completed | ✅ |
| IncomeBtc | ZeroToSuccess | Green | Completed | ✅ |
| SpendingLimit | DecreasingSuccess | Red | Failed | ✅ |
| ReduceExpenseCategory | DecreasingSuccess | Red | Failed | ✅ |
| BitcoinHodl | DecreasingSuccess | Red | Failed | ✅ |

---

## Background Processing

**GoalProgressUpdaterJob** runs every 1 second using a flag-based approach:

1. Event handlers (transaction changes, goal events) call `GoalProgressState.MarkAsStale()`
2. Job checks `HasStaleGoals` flag (no DB polling)
3. If stale, retrieves and recalculates affected goals
4. Auto-transitions state at 100% based on progression mode
5. Publishes `GoalProgressUpdated` message for UI updates

---

## Notes

- All goal types follow the same architectural pattern
- Domain classes must NOT have serialization attributes (use DTOs)
- Progress is always 0-100 (percentage)
- Use immutable patterns with `With*` factory methods for updates
- `ProgressionMode` determines both progress direction and auto-transition behavior
