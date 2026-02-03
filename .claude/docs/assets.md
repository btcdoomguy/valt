# Assets Module

Tracks external investments (stocks, ETFs, crypto, real estate, leveraged positions) separately from budget accounts, with automatic value calculations and multi-currency support.

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
- `PriceSource: AssetPriceSource` - Manual, YahooFinance, or CoinGecko
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

### Price Sources

**Enum:** `AssetPriceSource.cs`

| Value | Name | Description |
|-------|------|-------------|
| 0 | Manual | User-entered prices |
| 1 | YahooFinance | Auto-fetch from Yahoo Finance |
| 2 | CoinGecko | Auto-fetch from CoinGecko |

### Domain Events

**Files:** `Events/*.cs`

- `AssetCreatedEvent` - Emitted when a new asset is created
- `AssetUpdatedEvent` - Emitted when asset properties change
- `AssetDeletedEvent` - Emitted when asset is deleted
- `AssetPriceUpdatedEvent` - Emitted when price changes (includes old and new price)

## Infrastructure Layer (Valt.Infra/Modules/Assets/)

### Database Entity

**File:** `AssetEntity.cs`

LiteDB storage with JSON-serialized details:
- `Id`, `Name`, `DetailsJson`, `AssetTypeId`
- `Icon`, `IncludeInNetWorth`, `Visible`
- `LastPriceUpdateAt`, `CreatedAt`, `DisplayOrder`, `Version`

### Serialization

**File:** `AssetDetailsSerializer.cs`

Handles polymorphic JSON serialization of `IAssetDetails` implementations using type discriminators.

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
    Task<AssetSummaryDTO> GetSummaryAsync(
        string mainCurrencyCode,
        decimal? btcPriceUsd = null,
        IReadOnlyDictionary<string, decimal>? fiatRates = null);
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
| `UpdateAssetPrice` | Update current price |
| `UpdateAssetQuantity` | Update quantity (basic assets only) |
| `ToggleAssetVisibility` | Toggle visibility flag |
| `ToggleAssetNetWorthInclusion` | Toggle net worth inclusion |
| `DeleteAsset` | Delete an asset |

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

## File Structure

```
src/Valt.Core/Modules/Assets/
├── Asset.cs (Aggregate Root)
├── AssetId.cs, AssetName.cs
├── AssetTypes.cs, AssetPriceSource.cs
├── IAssetDetails.cs (Interface)
├── Details/
│   ├── BasicAssetDetails.cs
│   ├── RealEstateAssetDetails.cs
│   └── LeveragedPositionDetails.cs
├── Contracts/
│   └── IAssetRepository.cs
└── Events/
    ├── AssetCreatedEvent.cs
    ├── AssetUpdatedEvent.cs
    ├── AssetDeletedEvent.cs
    └── AssetPriceUpdatedEvent.cs

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
└── Modals/ManageAsset/
    ├── ManageAssetView.axaml
    ├── ManageAssetView.axaml.cs
    └── ManageAssetViewModel.cs
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
