# Goal Types Implementation Roadmap

This document outlines the planned goal types to implement, in priority order. Each goal type should be implemented, reviewed, and committed individually.

---

## 1. SpendingLimitGoalType (Budget Goal)

**Purpose**: Track that you don't exceed a spending limit in a period.

### Domain Model

```csharp
// src/Valt.Core/Modules/Goals/GoalTypes/SpendingLimitGoalType.cs
public class SpendingLimitGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.SpendingLimit;

    public FiatValue LimitAmount { get; }        // Maximum allowed spending
    public FiatCurrency Currency { get; }         // Currency for the limit
    public long CalculatedSpending { get; }       // Actual spending in cents (to avoid decimals)

    public FiatValue CalculatedSpendingValue => FiatValue.New(CalculatedSpending / 100m);
}
```

### Progress Calculation Logic

- Query all expense transactions (negative fiat amounts) in the period
- Filter by the specified currency
- Sum the absolute values
- Progress = 100 - ((spent / limit) * 100), capped between 0-100
- **100% = nothing spent, 0% = at or over limit**

### Transaction Types to Include

- `FiatDetails` with negative amount (expense)
- `FiatToBitcoinDetails` (buying bitcoin is spending fiat)
- `FiatToFiatDetails` where source account matches currency (transfer out)

### Files to Create/Modify

1. Add `SpendingLimit = 1` to `GoalTypeNames.cs`
2. Create `SpendingLimitGoalType.cs` in Core
3. Create `SpendingLimitGoalTypeDto.cs` in Infra
4. Create `SpendingLimitProgressCalculator.cs` in Infra
5. Update `Extensions.cs` with serialization mappings
6. Update `Extensions.cs` DI registration
7. Add localization strings for UI
8. Update `ManageGoalViewModel` to support the new type
9. Update `GoalEntryViewModel` for display

### Localization Keys Needed

- `GoalType_SpendingLimit` = "Spending Limit"
- `GoalDescription_SpendingLimit` = "Spent {0} of {1} limit"

---

## 2. DcaGoalType (Dollar Cost Averaging)

**Purpose**: Ensure consistent bitcoin purchases regardless of price.

### Domain Model

```csharp
// src/Valt.Core/Modules/Goals/GoalTypes/DcaGoalType.cs
public class DcaGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.Dca;

    public int TargetPurchaseCount { get; }       // Number of purchases to make
    public int CalculatedPurchaseCount { get; }   // Actual purchases made
}
```

### Progress Calculation Logic

- Query all `FiatToBitcoin` transactions in the period
- Count the number of transactions
- Progress = (count / target) * 100, capped at 100

### Transaction Types to Include

- `FiatToBitcoinDetails` only (bitcoin purchases)

### Files to Create/Modify

1. Add `Dca = 2` to `GoalTypeNames.cs`
2. Create `DcaGoalType.cs` in Core
3. Create `DcaGoalTypeDto.cs` in Infra
4. Create `DcaProgressCalculator.cs` in Infra
5. Update `Extensions.cs` with serialization mappings
6. Update `Extensions.cs` DI registration
7. Add localization strings
8. Update `ManageGoalViewModel`
9. Update `GoalEntryViewModel`

### Localization Keys Needed

- `GoalType_Dca` = "DCA"
- `GoalDescription_Dca` = "{0} of {1} purchases"

---

## 3. IncomeGoalType

**Purpose**: Track income targets for a period.

### Domain Model

```csharp
// src/Valt.Core/Modules/Goals/GoalTypes/IncomeGoalType.cs
public class IncomeGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.Income;

    public FiatValue TargetAmount { get; }        // Target income
    public FiatCurrency Currency { get; }         // Currency for tracking
    public long CalculatedIncome { get; }         // Actual income in cents

    public FiatValue CalculatedIncomeValue => FiatValue.New(CalculatedIncome / 100m);
}
```

### Progress Calculation Logic

- Query all income transactions (positive fiat amounts) in the period
- Filter by the specified currency
- Sum the values
- Progress = (income / target) * 100, capped at 100

### Transaction Types to Include

- `FiatDetails` with positive amount (income)
- `BitcoinToFiatDetails` (selling bitcoin is fiat income)

### Files to Create/Modify

1. Add `Income = 3` to `GoalTypeNames.cs`
2. Create `IncomeGoalType.cs` in Core
3. Create `IncomeGoalTypeDto.cs` in Infra
4. Create `IncomeProgressCalculator.cs` in Infra
5. Update `Extensions.cs` with serialization mappings
6. Update `Extensions.cs` DI registration
7. Add localization strings
8. Update `ManageGoalViewModel`
9. Update `GoalEntryViewModel`

### Localization Keys Needed

- `GoalType_Income` = "Income"
- `GoalDescription_Income` = "Earned {0} of {1}"

---

## 4. ReduceExpenseCategoryGoalType

**Purpose**: Reduce spending in a specific category.

### Domain Model

```csharp
// src/Valt.Core/Modules/Goals/GoalTypes/ReduceExpenseCategoryGoalType.cs
public class ReduceExpenseCategoryGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.ReduceExpenseCategory;

    public FiatValue LimitAmount { get; }         // Maximum for this category
    public FiatCurrency Currency { get; }         // Currency for tracking
    public long CategoryId { get; }               // Target category
    public long CalculatedSpending { get; }       // Actual spending in cents

    public FiatValue CalculatedSpendingValue => FiatValue.New(CalculatedSpending / 100m);
}
```

### Progress Calculation Logic

- Query all expense transactions in the period
- Filter by category ID and currency
- Sum absolute values
- Progress = 100 - ((spent / limit) * 100), capped between 0-100
- **100% = nothing spent in category, 0% = at or over limit**

### Transaction Types to Include

- `FiatDetails` with negative amount and matching category
- Other expense types if they have category assignments

### Files to Create/Modify

1. Add `ReduceExpenseCategory = 4` to `GoalTypeNames.cs`
2. Create `ReduceExpenseCategoryGoalType.cs` in Core
3. Create `ReduceExpenseCategoryGoalTypeDto.cs` in Infra
4. Create `ReduceExpenseCategoryProgressCalculator.cs` in Infra
5. Update `Extensions.cs` with serialization mappings
6. Update `Extensions.cs` DI registration
7. Add localization strings
8. Update `ManageGoalViewModel` (needs category picker)
9. Update `GoalEntryViewModel` (show category name)

### Localization Keys Needed

- `GoalType_ReduceExpenseCategory` = "Category Budget"
- `GoalDescription_ReduceExpenseCategory` = "{0}: Spent {1} of {2} limit"

### Special Considerations

- Need to load categories list in the ManageGoal modal
- Need to display category name in the goal entry
- Consider what happens if category is deleted (show "Unknown Category"?)

---

## 5. BitcoinHodlGoalType

**Purpose**: Track that you don't sell bitcoin (diamond hands challenge).

### Domain Model

```csharp
// src/Valt.Core/Modules/Goals/GoalTypes/BitcoinHodlGoalType.cs
public class BitcoinHodlGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.BitcoinHodl;

    public long MaxSellableSats { get; }          // 0 = no sales allowed, or max sats to sell
    public long CalculatedSoldSats { get; }       // Actual sats sold
}
```

### Progress Calculation Logic

- Query all `BitcoinToFiat` transactions in the period
- Sum the absolute sat amounts sold
- If MaxSellableSats == 0: Progress = soldSats == 0 ? 100 : 0
- If MaxSellableSats > 0: Progress = 100 - ((sold / max) * 100), capped between 0-100
- **100% = no sales (or under max), 0% = exceeded allowed sales**

### Transaction Types to Include

- `BitcoinToFiatDetails` only (bitcoin sales)

### Files to Create/Modify

1. Add `BitcoinHodl = 5` to `GoalTypeNames.cs`
2. Create `BitcoinHodlGoalType.cs` in Core
3. Create `BitcoinHodlGoalTypeDto.cs` in Infra
4. Create `BitcoinHodlProgressCalculator.cs` in Infra
5. Update `Extensions.cs` with serialization mappings
6. Update `Extensions.cs` DI registration
7. Add localization strings
8. Update `ManageGoalViewModel`
9. Update `GoalEntryViewModel`

### Localization Keys Needed

- `GoalType_BitcoinHodl` = "HODL"
- `GoalDescription_BitcoinHodl_NoSales` = "No bitcoin sold!"
- `GoalDescription_BitcoinHodl_WithLimit` = "Sold {0} of {1} max"
- `GoalDescription_BitcoinHodl_Failed` = "Sold {0} sats"

---

## Implementation Checklist Template

For each goal type, follow this checklist:

- [ ] Add enum value to `GoalTypeNames.cs`
- [ ] Create domain class in `src/Valt.Core/Modules/Goals/GoalTypes/`
- [ ] Create DTO class in `src/Valt.Infra/Modules/Goals/GoalTypeDtos.cs`
- [ ] Create calculator in `src/Valt.Infra/Modules/Goals/Services/`
- [ ] Add serialization in `src/Valt.Infra/Modules/Goals/Extensions.cs`
- [ ] Register calculator in DI in `Extensions.cs`
- [ ] Add strings to `language.resx` (English)
- [ ] Add strings to `language.pt-BR.resx` (Portuguese)
- [ ] Add strings to `language.es.resx` (Spanish)
- [ ] Update `language.Designer.cs`
- [ ] Update `ManageGoalViewModel.cs` for goal creation
- [ ] Update `ManageGoalView.axaml` if new input fields needed
- [ ] Update `GoalEntryViewModel.cs` for display
- [ ] Write unit tests for the calculator
- [ ] Write unit tests for the goal type
- [ ] Run all tests: `dotnet test`
- [ ] Run the app and test manually
- [ ] Commit with descriptive message

---

## Notes

- All goal types follow the same architectural pattern as `StackBitcoinGoalType`
- Domain classes must NOT have serialization attributes (use DTOs)
- Progress is always 0-100 (percentage)
- Use immutable patterns with `With*` factory methods for updates
- Background job will automatically recalculate progress every 5 seconds for stale goals
