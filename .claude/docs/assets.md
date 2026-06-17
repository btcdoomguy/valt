# Assets Module

Tracks external investments (stocks, ETFs, crypto, real estate, leveraged positions, BTC-backed loans) separately from budget accounts, with automatic value calculations and multi-currency support.

## Domain Layer (Valt.Core/Modules/Assets/)

### Core Entity: Asset

**File:** `Asset.cs`

Aggregate root for managing external investments.

**Properties:**
- `Name: AssetName` - Display name
- `Details: IAssetDetails` - Polymorphic asset configuration
- `Icon: Icon` - Display icon
- `IncludeInNetWorth: bool` - Include in total net worth calculation
- `Visible: bool` - UI visibility
- `LastPriceUpdateAt: DateTime` - Timestamp of last price update
- `CreatedAt: DateTime` - Creation timestamp
- `DisplayOrder: int` - Sort order

**Key Methods:**
- `Asset.New()` - Create new asset (emits `AssetCreatedEvent`)
- `Edit()` - Update asset properties (emits `AssetUpdatedEvent`)
- `UpdatePrice(newPrice)` - Update current price (emits `AssetPriceUpdatedEvent`)
- `SetVisibility(bool)` - Toggle visibility
- `SetIncludeInNetWorth(bool)` - Toggle net worth inclusion
- `GetCurrentPrice()` - Get price from details
- `GetCurrentValue()` - Calculate value from details
- `GetCurrencyCode()` - Get currency from details

### Asset Types

**Enum:** `AssetTypes.cs`

| Value | Name | Details Class |
|-------|------|---------------|
| 0 | Stock | BasicAssetDetails |
| 1 | Etf | BasicAssetDetails |
| 2 | Crypto | BasicAssetDetails |
| 3 | RealEstate | RealEstateAssetDetails |
| 4 | Commodity | BasicAssetDetails |
| 5 | LeveragedPosition | LeveragedPositionDetails |
| 6 | Custom | BasicAssetDetails |
| 7 | BtcLoan | BtcLoanDetails |

### Asset Details (IAssetDetails implementations)

**Interface:** `IAssetDetails.cs`

```csharp
public interface IAssetDetails
{
    AssetTypes AssetType { get; }
    decimal CalculateCurrentValue(decimal currentPrice);
    IAssetDetails WithUpdatedPrice(decimal newPrice);
}
```

#### BasicAssetDetails

**File:** `Details/BasicAssetDetails.cs`

For stocks, ETFs, crypto, commodities, and custom assets.

**Properties:**
- `Quantity: decimal` - Number of units held
- `Symbol: string?` - Ticker symbol (e.g., "AAPL", "BTC")
- `PriceSource: AssetPriceSource` - Manual or YahooFinance
- `CurrentPrice: decimal` - Price per unit
- `CurrencyCode: string` - Currency (e.g., "USD", "BRL")

**Value Calculation:** `Quantity * CurrentPrice`

**Builder Methods:** `WithQuantity()`, `WithUpdatedPrice()`

#### RealEstateAssetDetails

**File:** `Details/RealEstateAssetDetails.cs`

For real estate investments.

**Properties:**
- `CurrentValue: decimal` - Estimated property value
- `CurrencyCode: string` - Currency code
- `Address: string?` - Property address
- `MonthlyRentalIncome: decimal?` - Optional rental income

**Value Calculation:** Returns `CurrentValue` directly (no quantity multiplication)

**Builder Methods:** `WithRentalIncome()`, `WithUpdatedPrice()`

#### LeveragedPositionDetails

**File:** `Details/LeveragedPositionDetails.cs`

For futures, margin trading, and perpetuals.

**Properties:**
- `Collateral: decimal` - Initial margin
- `EntryPrice: decimal` - Position open price
- `Leverage: decimal` - Multiplier (e.g., 2x, 10x)
- `LiquidationPrice: decimal` - Liquidation threshold
- `CurrentPrice: decimal` - Current underlying price
- `CurrencyCode: string` - Currency code
- `Symbol: string?` - Underlying asset symbol
- `PriceSource: AssetPriceSource` - Price source
- `IsLong: bool` - Long (true) or Short (false)

**Value Calculation:**
```
priceChange = (CurrentPrice - EntryPrice) / EntryPrice
leveragedChange = priceChange * Leverage
Long:  Collateral * (1 + leveragedChange)
Short: Collateral * (1 - leveragedChange)
```

**Additional Calculations:**
- `CalculatePnL(currentPrice)` - Unrealized P&L
- `CalculatePnLPercentage(currentPrice)` - P&L as percentage (rounded to 2 decimals)
- `CalculateDistanceToLiquidation(currentPrice)` - Distance to liquidation %
- `IsAtRisk(currentPrice)` - True if within 10% of liquidation

**Builder Methods:** `WithCollateral()`, `WithUpdatedPrice()`

#### BtcLoanDetails

**File:** `Details/BtcLoanDetails.cs`

For BTC-collateralized loans (borrowing fiat against BTC collateral).

**Properties:**
- `PlatformName: string` - Lending platform name (e.g., "HodlHodl", "Ledn")
- `CollateralSats: long` - BTC collateral amount in satoshis
- `LoanAmount: decimal` - Borrowed fiat amount
- `CurrencyCode: string` - Currency code for the loan (e.g., "USD", "BRL")
- `Apr: decimal` - Annual percentage rate (e.g., 0.12 for 12%)
- `InitialLtv: decimal` - LTV ratio at loan origination
- `LiquidationLtv: decimal` - LTV ratio that triggers liquidation
- `MarginCallLtv: decimal` - LTV ratio that triggers a margin call warning
- `Fees: decimal` - Fees paid for the loan
- `LoanStartDate: DateOnly` - When the loan started
- `RepaymentDate: DateOnly?` - When the loan is due (null = open-ended)
- `Status: LoanStatus` - Current loan status (`Active` or `Repaid`)
- `CurrentBtcPriceInLoanCurrency: decimal` - BTC price in the loan currency, used for LTV calculations
- `FixedTotalDebt: decimal?` - Optional predefined total debt (e.g., HodlHodl-style fixed repayment)
- `Snapshots: IReadOnlyList<LoanStateSnapshot>` - Ordered timeline of loan-state snapshots
- `HasFixedTotalDebt: bool` - True when `FixedTotalDebt` is set

**Value Calculation:**
- `CalculateCurrentValue(btcPrice)` returns `-CalculateTotalDebt()`; the loan is represented as a pure liability because the BTC collateral is tracked separately in a BTC account.
- `CalculateTotalDebt()` uses the latest `LoanStateSnapshot` when available. For APR-based snapshots it adds simple interest accrued from the snapshot's effective date to today. For fixed-debt snapshots it returns the recorded `CurrentTotalDebt`. When no snapshot exists, it falls back to the immutable setup values (`LoanAmount`, `Fees`, `Apr`, `LoanStartDate`, `FixedTotalDebt`).
- `CalculateCurrentLtv(btcPrice)` computes `LoanAmount / CollateralValue * 100` using the latest snapshot's `CollateralSats` and `LoanAmount` when available, falling back to setup values.
- `CalculateHealthStatus(btcPrice)` returns `Healthy`, `Warning`, or `Danger` based on current LTV versus the latest snapshot's or setup's margin-call and liquidation thresholds.
- `CalculateDistanceToLiquidation(btcPrice)` returns the percentage-point distance to the liquidation LTV.
- `CalculateAccruedInterest()` returns accrued interest from the effective snapshot's effective date to today. For fixed-debt snapshots (or fixed-debt setup with no snapshots) it returns the fixed-amount delta or 0.
- `CalculateDaysUntilRepayment()` returns the number of days until `RepaymentDate`, or `null` if unset.

**Snapshot Mutations:**
- `WithAddedSnapshot(snapshot)` returns a new `BtcLoanDetails` with the snapshot appended. Throws if a snapshot for the same `EffectiveDate` already exists.
- `WithoutSnapshot(effectiveDate)` returns a new `BtcLoanDetails` without the matching snapshot.

**Other Methods:**
- `WithUpdatedPrice(newPrice)` returns a copy with `CurrentBtcPriceInLoanCurrency` updated.
- `WithStatus(newStatus)` returns a copy with `Status` updated.
- `DeriveAprFromFixedDebt(...)` computes an annualized APR from a fixed total debt and loan period.

**Latest-Snapshot Rule:**
`GetEffectiveSnapshot()` selects the snapshot with the maximum `EffectiveDate`. All current-state calculations use the latest snapshot's values as the source of truth. When the latest snapshot is deleted, the next-latest snapshot becomes effective; when no snapshots exist, the immutable initial setup values are used.

**Auto-Seeding:**
Existing loans that were created before the loan-state timeline feature may not have snapshots. The infrastructure serializer seeds an initial snapshot from the loan's setup values (`LoanStartDate` as the effective date, current calculated total debt, setup collateral, APR, fees, etc.) so every loan is queryable through the same timeline model.

#### LoanStateSnapshot

**File:** `Details/LoanStateSnapshot.cs`

A point-in-time snapshot of a BTC-backed loan's state.

**Properties:**
- `PlatformName: string` - Lending platform name
- `CollateralSats: long` - BTC collateral amount in satoshis
- `LoanAmount: decimal` - Borrowed fiat amount
- `CurrencyCode: string` - Currency code
- `Apr: decimal` - Annual percentage rate at the time of the snapshot
- `InitialLtv: decimal` - LTV ratio at loan origination
- `LiquidationLtv: decimal` - Liquidation LTV at the time of the snapshot
- `MarginCallLtv: decimal` - Margin-call LTV at the time of the snapshot
- `Fees: decimal` - Fees recorded in the snapshot
- `LoanStartDate: DateOnly` - Loan start date
- `RepaymentDate: DateOnly?` - Repayment date
- `Status: LoanStatus` - Loan status at the time of the snapshot
- `CurrentBtcPriceInLoanCurrency: decimal` - BTC price used for LTV calculations
- `FixedTotalDebt: decimal?` - Optional predefined total debt
- `CurrentTotalDebt: decimal` - Total debt recorded at the time of the snapshot
- `EffectiveDate: DateOnly` - Effective date of the snapshot. Only one snapshot is allowed per effective date on a loan.
- `Note: string?` - Optional note describing the snapshot

**Append-Only Semantics:**
Snapshots are immutable value objects. New state is recorded by adding a new snapshot with a later `EffectiveDate`. Deleting a snapshot removes it permanently; calculations automatically fall back to the previous snapshot or to the initial setup values.

### Price Sources

**Enum:** `AssetPriceSource.cs`

| Value | Name | Description |
|-------|------|-------------|
| 0 | Manual | User-entered prices |
| 1 | YahooFinance | Auto-fetch from Yahoo Finance |
| 2 | LivePrice | Auto-fetch from app's live BTC price (BTC/USD only) |

### Domain Events

**Files:** `Events/*.cs`

- `AssetCreatedEvent` - Emitted when a new asset is created
- `AssetUpdatedEvent` - Emitted when asset properties change
- `AssetDeletedEvent` - Emitted when asset is deleted
- `AssetPriceUpdatedEvent` - Emitted when price changes (includes old and new price)

## Application Layer (Valt.App/Modules/Assets/)

### Loan-State Commands

**AddLoanStateUpdateCommand** — `Commands/AddLoanStateUpdate/`

Adds a new state snapshot to a BTC-backed loan.

```csharp
public record AddLoanStateUpdateCommand : ICommand
{
    public required string AssetId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required decimal CurrentTotalDebt { get; init; }
    public required long CollateralSats { get; init; }
    public required decimal Apr { get; init; }
    public required decimal Fees { get; init; }
    public required string? Note { get; init; }
}
```

Validation rules:
- `AssetId` is required.
- `EffectiveDate` is required and must be after the latest existing snapshot (or after `LoanStartDate` when no snapshots exist).
- `CurrentTotalDebt` cannot be negative.
- `CollateralSats` must be greater than zero.
- `Apr` cannot be negative.
- `Fees` cannot be negative.

The handler copies immutable setup fields (`PlatformName`, `LoanAmount`, `InitialLtv`, `LiquidationLtv`, `MarginCallLtv`, `LoanStartDate`, `RepaymentDate`, `Status`, `CurrentBtcPriceInLoanCurrency`, `FixedTotalDebt`) from the loan or the latest snapshot, then creates a new `LoanStateSnapshot` with the supplied variable fields.

**DeleteLoanStateUpdateCommand** — `Commands/DeleteLoanStateUpdate/`

Deletes a state snapshot from a BTC-backed loan by its effective date.

```csharp
public record DeleteLoanStateUpdateCommand : ICommand
{
    public required string AssetId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
}
```

Validation rules:
- `AssetId` is required.
- `EffectiveDate` is required.

The handler removes the matching snapshot; calculations automatically fall back to the previous snapshot or to the immutable setup values.

### Loan-State Queries

**GetLoanStateTimelineQuery** — `Queries/GetLoanStateTimeline/`

Returns the full chronological snapshot timeline of a BTC-backed loan.

```csharp
public record GetLoanStateTimelineQuery : IQuery<IReadOnlyList<LoanStateSnapshotDTO>>
{
    public required string AssetId { get; init; }
}
```

Returns `IReadOnlyList<LoanStateSnapshotDTO>` where each DTO contains the same fields as `LoanStateSnapshot`, with `StatusId` in place of the enum.

**GetLatestLoanStateQuery** — `Queries/GetLatestLoanState/`

Returns the latest recorded state of a BTC-backed loan, including asset metadata.

```csharp
public record GetLatestLoanStateQuery : IQuery<LoanStateDTO?>
{
    public required string AssetId { get; init; }
}
```

Returns `LoanStateDTO?` with `AssetId`, `AssetName`, and all snapshot fields. Returns `null` when the asset is not found.

## Infrastructure Layer (Valt.Infra/Modules/Assets/)

### Database Entity

**File:** `AssetEntity.cs`

LiteDB storage with JSON-serialized details:
- `Id`, `Name`, `DetailsJson`, `AssetTypeId`
- `Icon`, `IncludeInNetWorth`, `Visible`
- `LastPriceUpdateAt`, `CreatedAt`, `DisplayOrder`, `Version`

### Serialization

**File:** `AssetDetailsSerializer.cs`

Handles polymorphic JSON serialization of `IAssetDetails` implementations using type discriminators. For legacy `BtcLoanDetails` loans without snapshots, the serializer seeds an initial `LoanStateSnapshot` from the loan's setup values so the timeline model is always populated.

### Repository

**File:** `AssetRepository.cs`

Implements `IAssetRepository`:
- `GetByIdAsync(AssetId)` - Retrieve by ID
- `GetAllAsync()` - Retrieve all assets
- `SaveAsync(Asset)` - Create or update
- `DeleteAsync(Asset)` - Delete with event

### Query Objects

**File:** `Queries/IAssetQueries.cs`

```csharp
public interface IAssetQueries
{
    Task<IReadOnlyList<AssetDTO>> GetAllAsync();
    Task<IReadOnlyList<AssetDTO>> GetVisibleAsync();
    Task<AssetDTO?> GetByIdAsync(string id);
    Task<IReadOnlyList<LoanStateSnapshotDTO>> GetLoanStateTimelineAsync(string assetId);
    Task<LoanStateDTO?> GetLatestLoanStateAsync(string assetId);
    Task<AssetSummaryDTO> GetSummaryAsync(
        string mainCurrencyCode,
        decimal? btcPriceUsd = null,
        IReadOnlyDictionary<string, decimal>? fiatRates = null,
        decimal? customBtcPriceUsd = null);
    Task<IReadOnlyList<AssetGroupDTO>> GetAssetGroupsAsync();
}
```

### DTOs

**File:** `Queries/DTOs/AssetDTO.cs`

Flattened DTO with all possible asset fields:

| Common | Basic | RealEstate | Leveraged |
|--------|-------|------------|-----------|
| Id, Name, AssetTypeId, AssetTypeName | Quantity, Symbol, PriceSourceId | Address, MonthlyRentalIncome | Collateral, EntryPrice, Leverage |
| Icon, IncludeInNetWorth, Visible | | | LiquidationPrice, IsLong |
| LastPriceUpdateAt, CreatedAt, DisplayOrder | | | PnL, PnLPercentage |
| CurrentPrice, CurrentValue, CurrencyCode | | | DistanceToLiquidation, IsAtRisk |

**BTC loan specific fields:** `PlatformName`, `CollateralSats`, `LoanAmount`, `Apr`, `CurrentLtv`, `InitialLtv`, `LiquidationLtv`, `MarginCallLtv`, `Fees`, `LoanStartDate`, `RepaymentDate`, `LoanStatusId`, `LoanStatusName`, `LoanHealthStatusId`, `LoanHealthStatusName`, `AccruedInterest`, `TotalDebt`, `DistanceToLiquidationLtv`, `DaysUntilRepayment`, `FixedTotalDebt`, `HasFixedTotalDebt`.

**File:** `Queries/DTOs/AssetSummaryDTO.cs`

```csharp
public class AssetSummaryDTO
{
    public int TotalAssets { get; set; }
    public int VisibleAssets { get; set; }
    public int AssetsIncludedInNetWorth { get; set; }
    public List<AssetValueByCurrencyDTO> ValuesByCurrency { get; set; }
    public decimal TotalValueInMainCurrency { get; set; }
    public long TotalValueInSats { get; set; }
}
```

## UI Layer (Valt.UI/)

### AssetsViewModel

**File:** `Views/Main/Tabs/Assets/AssetsViewModel.cs`

Main tab ViewModel for the Assets page.

**Properties:**
- `Assets: AvaloniaList<AssetViewModel>` - List of assets
- `Summary: AssetSummaryDTO?` - Aggregated totals
- `SelectedAsset: AssetViewModel?` - Currently selected asset
- `IsLoading: bool` - Loading state
- `MainCurrencyCode: string` - User's main currency
- `TotalValueFormatted: string` - Summary total with currency symbol
- `TotalValueColor: string` - Color based on positive/negative
- `TotalSatsColor: string` - Color for sats value

**Commands:**
- `AddAsset` - Opens ManageAsset modal
- `EditAsset(asset)` - Opens ManageAsset modal in edit mode
- `DeleteAsset(asset)` - Deletes the asset
- `ToggleVisibility(asset)` - Toggles asset visibility
- `ToggleIncludeInNetWorth(asset)` - Toggles net worth inclusion

**Refresh Triggers:**
- `RatesState.PropertyChanged` - When BTC or fiat rates change
- `SettingsChangedMessage` - When main currency changes

### AssetViewModel

**File:** `Views/Main/Tabs/Assets/Models/AssetViewModel.cs`

Display model for individual assets.

**Formatted Properties:**
- `CurrentValueFormatted` - Value with currency symbol
- `CurrentPriceFormatted` - Price with currency symbol
- `QuantityFormatted` - Quantity with 4 decimals
- `PnLFormatted` - P&L with currency symbol
- `PnLPercentageFormatted` - P&L percentage with +/- sign
- `PnLCombinedFormatted` - Combined format: "$ -1,500.00 (-150%)"
- `DisplayValueFormatted` - **P&L for leveraged, Value for others**
- `LeverageFormatted` - "10x" format
- `DistanceToLiquidationFormatted` - "15%" format

**Display Helpers:**
- `IsBasicAsset` - True for Stock, Etf, Crypto, Commodity, Custom
- `IsRealEstate` - True for RealEstate type
- `IsLeveragedPosition` - True for LeveragedPosition type
- `PositionDirection` - "Long" or "Short"
- `PnLColor` - Green (#4CAF50) or Red (#F44336)
- `ValueColor` - White or Red based on positive/negative
- `AtRiskIndicator` - "!" if IsAtRisk

### ManageAssetViewModel

**File:** `Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs`

Modal for creating/editing assets.

**Form Modes:**
- `ShowBasicFields` - For Stock, Etf, Crypto, Commodity, Custom
- `ShowRealEstateFields` - For RealEstate
- `ShowLeveragedFields` - For LeveragedPosition

**Common Fields:**
- `Name`, `SelectedAssetType`, `SelectedCurrency`
- `IncludeInNetWorth`, `Visible`

**Type-Specific Fields:**
- Basic: `Symbol`, `Quantity`, `CurrentPrice`, `SelectedPriceSource`
- RealEstate: `Address`, `CurrentValue`, `MonthlyRentalIncome`
- Leveraged: `Symbol`, `Collateral`, `EntryPrice`, `CurrentPrice`, `Leverage`, `LiquidationPrice`, `IsLong`, `SelectedPriceSource`

### Loan-State Modals

**UpdateLoanStateView** — `Views/Main/Modals/UpdateLoanState/UpdateLoanStateView.axaml`

- Dimensions: 550×650 (`MinWidth="550" MinHeight="650" MaxWidth="550" MaxHeight="650"`).
- Header: custom title bar with the loan name.
- Current Loan Context section: platform name, loan amount, initial LTV, margin-call LTV, liquidation LTV, loan start date, repayment date. Includes a "View History" link that opens the history modal.
- New State section: effective date (`CalendarDatePicker`, defaulting to today), current total debt (`FiatInput`), collateral sats (`TextBox`), APR percentage (`TextBox`), fees (`FiatInput`), and an optional note (`TextBox`).
- Actions: `Save Snapshot` (OK) and `Discard Changes` (Cancel).
- The modal is prefilled from the latest snapshot when one exists; otherwise it falls back to the asset's setup values.

**LoanStateHistoryView** — `Views/Main/Modals/LoanStateHistory/LoanStateHistoryView.axaml`

- Dimensions: 550×650.
- Header: custom title bar with the loan name.
- Toolbar: `Add New State` button and `Delete Selected` button (delete guard prevents deleting without a selection).
- DataGrid columns: Effective Date, Current Total Debt, Collateral (sats), APR, Fees.
- Actions: `Close`. Double-click / Add New State navigation opens `UpdateLoanStateView` to append a new snapshot.

### AssetsView Grid

**File:** `Views/Main/Tabs/Assets/AssetsView.axaml`

DataGrid columns:
| Column | Binding | Width |
|--------|---------|-------|
| Name | `Name` | 150 |
| Type | `AssetTypeName` | 100 |
| Symbol | `Symbol` | 80 |
| Quantity | `QuantityFormatted` | 100 |
| Price | `CurrentPriceFormatted` | 120 |
| Value | `DisplayValueFormatted` | 200 |
| Visible | `Visible` | 70 |
| Net Worth | `IncludeInNetWorth` | 90 |

**Value Column Logic:**
- Leveraged positions show P&L combined format (e.g., "$ -1,500.00 (-150%)")
- Other assets show current value

**Summary Panel:**
- Total Assets count
- Assets in Net Worth count
- Total Value in main currency (with currency symbol)
- Total Value in sats

## MCP Server Integration

**File:** `Valt.Infra/Mcp/Tools/AssetTools.cs`

### Available Tools

| Tool | Description |
|------|-------------|
| `GetAssets` | Get all tracked assets |
| `GetVisibleAssets` | Get only visible assets |
| `GetAsset` | Get single asset by ID |
| `GetAssetsSummary` | Get totals in main currency and sats |
| `CreateBasicAsset` | Create stock/ETF/crypto/commodity/custom |
| `CreateRealEstateAsset` | Create real estate asset |
| `CreateLeveragedPosition` | Create leveraged trading position |
| `CreateBtcLoan` | Create a BTC-collateralized loan |
| `CreateBtcLending` | Create a BTC/fiat lending position |
| `RepayLoan` | Mark a BTC loan or lending position as repaid |
| `UpdateAssetPrice` | Update current price |
| `UpdateAssetQuantity` | Update quantity (basic assets only) |
| `ToggleAssetVisibility` | Toggle visibility flag |
| `ToggleAssetNetWorthInclusion` | Toggle net worth inclusion |
| `DeleteAsset` | Delete an asset |
| `GetAssetGroups` | Get all asset groups |
| `CreateAssetGroup` | Create an asset group |
| `UpdateAssetGroup` | Update an asset group name and description |
| `DeleteAssetGroup` | Delete an asset group |
| `MoveAssetToGroup` | Move an asset to a group |
| `RemoveAssetFromGroup` | Remove an asset from its group |
| `AddLoanStateUpdate` | Add a new state snapshot to a BTC-backed loan |
| `DeleteLoanStateUpdate` | Delete a state snapshot from a BTC-backed loan by effective date |
| `GetLoanStateTimeline` | Get the full chronological snapshot timeline of a BTC-backed loan |
| `GetLatestLoanState` | Get the latest recorded state of a BTC-backed loan |

## Key Patterns

### Polymorphism via IAssetDetails
- Domain layer uses interface without serialization attributes
- Infrastructure layer handles JSON serialization with type discriminators
- Type discrimination via `AssetType` enum

### Value Display Logic
- Leveraged positions: Show P&L combined format in Value column
- Other assets: Show current value

### Multi-Currency Support
- Each asset has its own `CurrencyCode`
- Summary converts all values to main currency using fiat rates
- BTC conversion uses current BTC price in USD

### Color Coding
- P&L Color: Green (#4CAF50) for profit, Red (#F44336) for loss
- Value Color: White for positive, Red for negative
- At Risk indicator for leveraged positions within 10% of liquidation

### Loan-State Timeline
- Latest snapshot (by `EffectiveDate`) wins for all current-value calculations
- Snapshots are append-only; deleting a snapshot falls back to the previous snapshot or initial setup values
- Existing loans are auto-seeded with a snapshot derived from their immutable setup values

## File Structure

```
src/Valt.Core/Modules/Assets/
├── Asset.cs (Aggregate Root)
├── AssetId.cs, AssetName.cs
├── AssetTypes.cs, AssetPriceSource.cs
├── LoanStatus.cs, LoanHealthStatus.cs
├── IAssetDetails.cs (Interface)
├── Details/
│   ├── BasicAssetDetails.cs
│   ├── RealEstateAssetDetails.cs
│   ├── LeveragedPositionDetails.cs
│   ├── BtcLoanDetails.cs
│   └── LoanStateSnapshot.cs
├── Contracts/
│   └── IAssetRepository.cs
└── Events/
    ├── AssetCreatedEvent.cs
    ├── AssetUpdatedEvent.cs
    ├── AssetDeletedEvent.cs
    └── AssetPriceUpdatedEvent.cs

src/Valt.App/Modules/Assets/
├── Commands/
│   ├── AddLoanStateUpdate/
│   │   ├── AddLoanStateUpdateCommand.cs
│   │   ├── AddLoanStateUpdateHandler.cs
│   │   └── AddLoanStateUpdateValidator.cs
│   ├── DeleteLoanStateUpdate/
│   │   ├── DeleteLoanStateUpdateCommand.cs
│   │   ├── DeleteLoanStateUpdateHandler.cs
│   │   └── DeleteLoanStateUpdateValidator.cs
│   └── ...
├── Queries/
│   ├── GetLoanStateTimeline/
│   │   ├── GetLoanStateTimelineQuery.cs
│   │   └── GetLoanStateTimelineHandler.cs
│   ├── GetLatestLoanState/
│   │   ├── GetLatestLoanStateQuery.cs
│   │   └── GetLatestLoanStateHandler.cs
│   └── ...
├── DTOs/
│   ├── LoanStateSnapshotDTO.cs
│   └── LoanStateDTO.cs
└── Contracts/
    └── IAssetQueries.cs

src/Valt.Infra/Modules/Assets/
├── AssetEntity.cs
├── AssetDetailsSerializer.cs
├── AssetRepository.cs
├── Extensions.cs
└── Queries/
    ├── IAssetQueries.cs
    ├── AssetQueries.cs
    └── DTOs/
        ├── AssetDTO.cs
        └── AssetSummaryDTO.cs

src/Valt.Infra/Mcp/Tools/
└── AssetTools.cs

src/Valt.UI/Views/Main/
├── Tabs/Assets/
│   ├── AssetsView.axaml
│   ├── AssetsView.axaml.cs
│   ├── AssetsViewModel.cs
│   └── Models/
│       └── AssetViewModel.cs
└── Modals/
    ├── ManageAsset/
    │   ├── ManageAssetView.axaml
    │   ├── ManageAssetView.axaml.cs
    │   └── ManageAssetViewModel.cs
    ├── UpdateLoanState/
    │   ├── UpdateLoanStateView.axaml
    │   ├── UpdateLoanStateView.axaml.cs
    │   └── UpdateLoanStateViewModel.cs
    └── LoanStateHistory/
        ├── LoanStateHistoryView.axaml
        ├── LoanStateHistoryView.axaml.cs
        └── LoanStateHistoryViewModel.cs
```

## Data Flow Example

**Creating a Leveraged Position:**

1. **User creates position:** "BTC Long 10x"
   - Modal creates `LeveragedPositionDetails(collateral: 1000, entryPrice: 50000, leverage: 10, ...)`
   - `Asset.New()` creates domain object, emits `AssetCreatedEvent`

2. **Price updates:** BTC moves to $55,000
   - `asset.UpdatePrice(55000)` called
   - `LeveragedPositionDetails.CalculateCurrentValue(55000)`:
     - priceChange = (55000 - 50000) / 50000 = 0.10
     - leveragedChange = 0.10 * 10 = 1.00
     - value = 1000 * (1 + 1.00) = $2,000
   - P&L = $2,000 - $1,000 = $1,000 (100%)
   - Emits `AssetPriceUpdatedEvent`

3. **UI displays:**
   - Value column shows: "$ 1,000.00 (+100%)" (P&L combined format)
   - Summary updates total value in main currency

**Negative P&L Example:**

BTC drops to $45,000:
- priceChange = (45000 - 50000) / 50000 = -0.10
- leveragedChange = -0.10 * 10 = -1.00
- value = 1000 * (1 + (-1.00)) = $0 (liquidation)
- P&L = $0 - $1,000 = -$1,000 (-100%)
- Value column shows: "$ -1,000.00 (-100%)" in red

**Recording a BTC Loan State Update:**

1. **Initial loan:** "HodlHodl loan" created with `BtcLoanDetails`.
   - Serializer auto-seeds a snapshot with `EffectiveDate = LoanStartDate` if none exists.

2. **User updates loan state:** Additional capital is drawn, so debt and collateral change.
   - `UpdateLoanStateView` is prefilled from the latest snapshot (or setup values if empty).
   - User sets `EffectiveDate` to today and enters new `CurrentTotalDebt`, `CollateralSats`, `Apr`, and `Fees`.
   - `AddLoanStateUpdateCommand` creates a new `LoanStateSnapshot` and appends it via `BtcLoanDetails.WithAddedSnapshot`.

3. **Calculations use the latest snapshot:**
   - `BtcLoanDetails.GetEffectiveSnapshot()` returns the new snapshot.
   - `CalculateTotalDebt()` uses the snapshot's `CurrentTotalDebt` plus accrued interest from the snapshot's date.
   - `CalculateCurrentLtv()` uses the snapshot's collateral and loan amount.

4. **User deletes the latest snapshot:**
   - `DeleteLoanStateUpdateCommand` removes it via `BtcLoanDetails.WithoutSnapshot`.
   - Calculations automatically fall back to the previous snapshot, or to the immutable setup values when no snapshots remain.
