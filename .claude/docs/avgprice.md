# AvgPrice Module (Cost Basis Tracking)

Tracks cost basis for bitcoin holdings with multiple calculation methods.

## Domain Layer (Valt.Core/Modules/AvgPrice/)

### Core Aggregate: AvgPriceProfile

**File:** `AvgPriceProfile.cs`

Manages a cost basis tracking profile.

**Properties:**
- `Name: AvgPriceProfileName` - Profile name (max 30 chars)
- `Asset: AvgPriceAsset` - Asset definition (e.g., "BTC" with 8 decimals)
- `Visible: bool` - UI visibility
- `Icon: Icon` - Custom icon
- `Currency: FiatCurrency` - Currency for cost calculations
- `CalculationMethod: AvgPriceCalculationMethod` - Strategy (BrazilianRule or Fifo)
- `AvgPriceLines: IReadOnlyCollection<AvgPriceLine>` - Transaction lines

**Key Methods:**
- `AddLine()` - Add buy/sell/setup line with auto display order
- `RemoveLine()` - Remove line and recalculate
- `MoveLineUp/MoveLineDown()` - Reorder same-date lines
- `ChangeCalculationMethod()` - Switch strategy (triggers recalculation)

### AvgPriceLine Entity

**File:** `AvgPriceLine.cs`

Individual cost basis entry.

**Properties:**
- `Date: DateOnly` - Transaction date
- `DisplayOrder: int` - Order for same-date transactions
- `Type: AvgPriceLineTypes` - Buy, Sell, or Setup
- `Quantity: decimal` - Asset quantity
- `Amount: FiatValue` - Total cost
- `UnitPrice: FiatValue` - Calculated (Amount / Quantity)
- `Comment: string` - Optional notes
- `Totals: LineTotals` - Calculated cumulative totals

### Value Objects

| Type | Description |
|------|-------------|
| LineTotals | `record(AvgCostOfAcquisition, TotalCost, Quantity)` |
| AvgPriceAsset | `record(Name, Precision)` - e.g., Bitcoin = ("BTC", 8) |
| AvgPriceLineTypes | `enum { Buy, Sell, Setup }` |
| AvgPriceCalculationMethod | `enum { BrazilianRule, Fifo }` |

### Calculation Strategies

**Interface:** `IAvgPriceCalculationStrategy`
```csharp
void CalculateTotals(IEnumerable<AvgPriceLine> orderedLines);
```

Lines must be pre-ordered by Date, then DisplayOrder.

#### BrazilianRuleCalculationStrategy

Weighted average cost basis (Brazilian tax law).

**Algorithm:**
- **Buy:** `totalCost += amount`, `quantity += qty`, `avg = totalCost / quantity`
- **Sell:** `proportionSold = sellQty / currentQty`, `totalCost -= totalCost * proportionSold`
- **Setup:** Resets to specified quantity and unit cost

**Characteristics:**
- Simple weighted average
- Proportional cost reduction on partial sales
- Non-specific identification

#### FifoCalculationStrategy

First-In-First-Out accounting.

**Algorithm:**
- Maintains `Queue<CostLot>` where lot = (quantity, unitPrice)
- **Buy:** Enqueues new lot
- **Sell:** Dequeues from front until sell quantity satisfied
- **Setup:** Clears queue and enqueues single lot

**Characteristics:**
- Tracks individual cost lots
- More accurate for FIFO tax jurisdictions
- Helper record: `CostLot(decimal Quantity, decimal UnitPrice)`

## Infrastructure Layer (Valt.Infra/Modules/AvgPrice/)

### Database Entities

**AvgPriceProfileEntity:**
- Fields: `Name`, `AssetName`, `Precision`, `Visible`, `Icon`, `Currency`, `CalculationMethodId`
- BsonRef to `List<AvgPriceLineEntity>`

**AvgPriceLineEntity:**
- Fields: `ProfileId`, `Date`, `DisplayOrder`, `LineTypeId`, `Quantity`, `Amount`, `UnitPrice`, `Comment`
- Calculated: `AvgCostOfAcquisition`, `TotalCost`, `TotalQuantity`

### Repository

**File:** `AvgPriceRepository.cs`

- `GetAvgPriceProfileByIdAsync` - Fetches profile with lines
- `SaveAvgPriceProfileAsync` - Persists and publishes domain events
- `DeleteAvgPriceProfileAsync` - Cascade deletes lines and profile

### Totalizer

**File:** `AvgPriceTotalizer.cs`

Calculates profit/loss across profiles.

**Method:** `GetTotalsAsync(year, profileIds)`

**Validation:** All profiles must use same currency (throws `MixedCurrencyException`)

**Output:**
```csharp
record TotalsDTO(int Year, IEnumerable<MonthlyTotalsDTO> MonthlyTotals, ValuesDTO YearlyTotals);
record ValuesDTO(decimal AmountBought, decimal AmountSold, decimal TotalProfitLoss, decimal Volume);
```

**Calculation:**
- For each SELL: `profitLoss = saleAmount - (sellQuantity * currentAvgCost)`
- Aggregates by month and year

### Queries

**IAvgPriceQueries:**
- `GetProfilesAsync(showHidden)` - Returns profile DTOs
- `GetLinesOfProfileAsync(id)` - Returns lines ordered by date + display order

## UI Layer (Valt.UI/Views/Main/)

### AvgPriceViewModel (Tab)

**File:** `Tabs/AvgPrice/AvgPriceViewModel.cs`

**Observables:**
- `Profiles` - List of profiles
- `SelectedProfile` - Current profile
- `Lines` - Lines for selected profile
- `TotalsRows` - Monthly/yearly profit/loss
- `CurrentPosition`, `CurrentAvgPrice` - Summary from last line

**Commands:**
- `ManageProfiles` - Opens profile manager
- `AddOperation`, `EditOperation`, `DeleteOperation` - Line CRUD
- `MoveUp`, `MoveDown` - Reorder lines

### AvgPriceLineEditorViewModel (Modal)

**File:** `Modals/AvgPriceLineEditor/AvgPriceLineEditorViewModel.cs`

**Form Fields:**
- `Date` - Transaction date (required)
- `LineType` - Buy/Sell/Setup
- `Quantity` - Asset amount (> 0)
- `Amount` - Fiat cost (for Setup: avg cost)
- `Comment` - Optional notes

### ManageAvgPriceProfilesViewModel (Modal)

**File:** `Modals/ManageAvgPriceProfiles/ManageAvgPriceProfilesViewModel.cs`

**Form Fields:**
- `Name`, `AssetName`, `Precision`, `Visible`
- `Currency`, `Icon`, `SelectedStrategy`

**Asset Presets:**
- `SetBitcoin()` - BTC with 8 decimals
- `SetCustomAsset()` - Custom name with 2 decimals

## Key Patterns

### Strategy Pattern
- Calculation strategy selected at profile level
- Switching strategies triggers full recalculation
- Both strategies implement identical interface

### Recalculation Flow
1. Any profile mutation triggers `Recalculate()`
2. All lines recalculated in order using active strategy
3. Ensures consistency across calculation methods

### Domain-Infra Separation
- Domain layer free of serialization attributes
- Infrastructure handles Entity <-> DTO conversion
- Queries return DTOs; Commands work with aggregates

## Testing

Use builders from `tests/Valt.Tests/Builders/`:

```csharp
// Profiles
AvgPriceProfileBuilder.AProfile().Build();
AvgPriceProfileBuilder.ABrazilianRuleProfile().Build();  // BRL currency
AvgPriceProfileBuilder.AFifoProfile().Build();          // USD, FIFO method

// Lines
AvgPriceLineBuilder.ABuyLine()
    .WithDate(new DateOnly(2024, 1, 1))
    .WithQuantity(0.1m)
    .WithAmount(FiatValue.New(5000m))
    .Build();

AvgPriceLineBuilder.ASellLine().Build();
AvgPriceLineBuilder.ASetupLine().Build();
```

## File Structure

```
src/Valt.Core/Modules/AvgPrice/
├── AvgPriceProfile.cs (Aggregate Root)
├── AvgPriceLine.cs (Entity)
├── LineTotals.cs, AvgPriceAsset.cs (Value Objects)
├── AvgPriceLineTypes.cs, AvgPriceCalculationMethod.cs (Enums)
├── CalculationStrategies/
│   ├── IAvgPriceCalculationStrategy.cs
│   ├── BrazilianRuleCalculationStrategy.cs
│   └── FifoCalculationStrategy.cs
├── Contracts/
│   └── IAvgPriceRepository.cs, IAvgPriceTotalizer.cs
└── Events/
    └── AvgPrice*Event.cs (Created, Updated, Deleted, Line variants)

src/Valt.Infra/Modules/AvgPrice/
├── AvgPriceProfileEntity.cs, AvgPriceLineEntity.cs
├── Extensions.cs (Mapping)
├── AvgPriceRepository.cs, AvgPriceTotalizer.cs
└── Queries/
    └── AvgPriceQueries.cs, DTOs/

src/Valt.UI/Views/Main/
├── Tabs/AvgPrice/
│   └── AvgPriceViewModel.cs, Models/
└── Modals/
    ├── AvgPriceLineEditor/
    └── ManageAvgPriceProfiles/
```