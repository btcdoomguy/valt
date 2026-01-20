# Conversion Calculator Module

Multi-currency calculator for converting between BTC, SATS, and fiat currencies.

## Components

### CurrencyConversionService
**File:** `src/Valt.Infra/Modules/Currency/Services/CurrencyConversionService.cs`

Stateless service for currency conversions using USD as intermediate currency.

**Methods:**
- `Convert(amount, from, to, btcPrice, fiatRates)` - Single conversion
- `ConvertToAll(amount, from, btcPrice, fiatRates)` - Convert to all currencies

**Supported Currencies:**
- BTC (Bitcoin, 8 decimals)
- SATS (Satoshis, 0 decimals) - 1 BTC = 100,000,000 SATS
- 32 fiat currencies via `FiatCurrency.GetAll()`

### ConversionCalculatorViewModel
**File:** `src/Valt.UI/Views/Main/Modals/ConversionCalculator/ConversionCalculatorViewModel.cs`

Modal ViewModel for calculator with expression evaluation and currency conversion.

**Properties:**
- `Expression` - Calculator input string
- `CalculatedValue` - Evaluated result
- `SelectedCurrency` - Currently selected currency
- `Currencies` - Available currency items
- `IsResponseMode` - When true, shows OK/Cancel and freezes currency selection

**Request/Response:**
```csharp
public record Request(bool ResponseMode = false, string? DefaultCurrencyCode = null);
public record Response(decimal? Result, string? SelectedCurrencyCode);
```

### CurrencyConversionItem
**File:** `src/Valt.UI/Views/Main/Modals/ConversionCalculator/CurrencyConversionItem.cs`

Observable item representing a currency in the list.

**Properties:**
- `CurrencyCode` - "BTC", "SATS", or fiat code
- `CurrencySymbol` - Display symbol (B, sats, $, etc.)
- `Decimals` - Formatting precision
- `IsBitcoin` - True for BTC
- `IsSats` - True for SATS
- `ConvertedValue` - Calculated value
- `FormattedValue` - Culture-formatted string

### BtcInput User Control
**File:** `src/Valt.UI/UserControls/BtcInput.axaml.cs`

Input control for Bitcoin values with BTC/Sats toggle.

**Properties:**
- `BtcValue` - Internal value (always in satoshis)
- `IsBitcoin` - Display mode (true=BTC decimal, false=sats integer)
- `CalculatorCommand` - Opens ConversionCalculator
- `CalculatorCommandParameter` - Field identifier (e.g., "FromBtc")

## Data Flow

### Standalone Mode
1. User opens calculator (not from BtcInput)
2. Enters expression, selects source currency
3. All conversions displayed in real-time
4. Close button exits

### Response Mode (from BtcInput)
1. BtcInput invokes CalculatorCommand with parameter
2. TransactionEditorViewModel creates Request with:
   - `ResponseMode: true`
   - `DefaultCurrencyCode: "BTC" or "SATS"` based on BtcInput.IsBitcoin
3. Calculator opens with pre-selected currency, list frozen
4. User enters value, clicks OK
5. Response returns `(Result, SelectedCurrencyCode)`
6. ViewModel interprets result based on currency code

## Testing

Tests in `tests/Valt.Tests/Currency/CurrencyConversionServiceTests.cs`

```csharp
// SATS conversions
_sut.Convert(100000m, "SATS", "BTC", 50000m, fiatRates) // => 0.001
_sut.Convert(0.001m, "BTC", "SATS", 50000m, fiatRates)  // => 100000
_sut.Convert(100000m, "SATS", "USD", 50000m, fiatRates) // => 50
```
