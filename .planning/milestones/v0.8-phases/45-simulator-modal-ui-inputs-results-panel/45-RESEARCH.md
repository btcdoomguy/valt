# Phase 45: Simulator Modal UI (Inputs + Results Panel) — Research

**Researched:** 2026-08-14  
**Domain:** Avalonia desktop UI, MVVM, live-recalculation modal, .NET/C#  
**Confidence:** HIGH

## Summary

Phase 45 surfaces the Phase 44 `BtcLoanSimulationCalculator` through a new chromeless modal mirroring the existing **Leverage Simulator** layout (inputs on the left, results on the right). The work is almost entirely in the `Valt.UI` presentation layer: a new `BtcLoanSimulatorView` + `BtcLoanSimulatorViewModel`, modal enum registration, DI wiring, a Tools-menu command, and unit tests. No persistence, App-layer commands, or MCP work is required in this phase.

The strongest precedent is `LeverageSimulatorView`/`LeverageSimulatorViewModel` — it already implements live recalculation via `ObservableProperty` partial methods, a currency dropdown, a placeholder prefill dropdown, fiat+sats formatting, and a `CustomTitleBar` chromeless window. The BTC loan simulator clones that shape and swaps the position/math inputs for loan inputs (`BtcInput` collateral, `FiatInput` amount taken/fees, `CalendarDatePicker` dates, `ToggleButton` interest mode).

The only material difference from the precedent is the math source (`BtcLoanSimulationCalculator` in `Valt.Core`) and the need to derive/display a live distance-to-liquidation from `RatesState.BitcoinPrice`.

**Primary recommendation:** Build the modal as a direct Leverage Simulator clone, reuse `BtcInput`, `FiatInput`, `CustomTitleBar`, `StringToColorBrushConverter`, and `RatesState`, and call `BtcLoanSimulationCalculator.Calculate` from a single `Recalculate()` method. Add English literals now per the approved UI-SPEC; defer full pt-BR/es localization and `language.Designer.cs` regeneration to Phase 48.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Modal chrome & layout | UI (`Valt.UI`) | — | Avalonia `Window`, `CustomTitleBar`, XAML |
| Live recalculation | UI (`Valt.UI`) | — | `BtcLoanSimulatorViewModel` reacts to input property changes and calls the Core calculator |
| Interest/fee math | Core (`Valt.Core`) | — | `BtcLoanSimulationCalculator` already shipped in Phase 44 |
| BTC/fiat conversion basis | UI (`Valt.UI`) | Infra (`RatesState`) | `RatesState` owns live BTC/USD and fiat rates; VM formats sats and basis label |
| Currency list | UI (`Valt.UI`) | Infra (`IConfigurationManager`) | Reuse `GetAvailableFiatCurrencies` pattern from Leverage Simulator |
| Tools menu entry | UI (`Valt.UI`) | — | `MainView.axaml` + `MainViewModel` command |
| Input validation / empty state | UI (`Valt.UI`) | — | VM guards non-parseable inputs and invalid date ranges before calling calculator |
| Unit tests | Tests (`Valt.Tests`) | — | Direct VM tests with NSubstitute, no UI automation needed |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Avalonia | 12.1.0 | Desktop UI framework | Version locked in `Directory.Packages.props`; build currently green [VERIFIED: Directory.Packages.props:11] |
| Avalonia.Themes.Fluent | 12.1.0 | Fluent theme / built-in controls | Used by every existing modal [VERIFIED: src/Valt.UI/Valt.UI.csproj:28] |
| CommunityToolkit.Mvvm | 8.4.2 | `ObservableProperty`, `RelayCommand`, MVVM | Used by every ViewModel [VERIFIED: Directory.Packages.props:18] |
| NUnit | 4.4.0 | Test framework | Existing test project target [VERIFIED: Directory.Packages.props:37] |
| NSubstitute | 5.3.0 | Mocking dependencies | Used by all VM tests [VERIFIED: Directory.Packages.props:36] |

**Note on Avalonia version:** `AGENTS.md` states Avalonia 11.3, but the repo actually builds with **Avalonia 12.1.0** [VERIFIED: Directory.Packages.props:11]. Phase 45 must follow the installed packages, not the stale docs.

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| LiveChartsCore.SkiaSharpView.Avalonia | 2.1.0-dev-365 | Charts | Not used in this phase; reserved for Phase 46 schedule visualization |
| LiteDB | 5.0.21 | Persistence | Not used in this phase; simulator is ephemeral |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `BtcInput` for collateral | Plain `TextBox` | Loses built-in BTC/sats toggle and sat parsing; `BtcInput` is the project standard |
| `FiatInput` for amount/fees | Plain `TextBox` | Loses currency symbol, decimal handling, and focus behavior; `FiatInput` is the project standard |
| Inline math in VM | Call `BtcLoanSimulationCalculator` | Keeps parity with Phase 44 and avoids duplicating act/365 logic |
| ReactiveUI instead of MVVM Toolkit | — | Would break project-wide `ObservableProperty`/`RelayCommand` convention |

**Installation:** No new packages required. All dependencies are already referenced.

## Package Legitimacy Audit

No new external packages are installed in Phase 45. The phase reuses existing Avalonia, CommunityToolkit.Mvvm, NUnit, and NSubstitute packages already in `Directory.Packages.props` and restored in the workspace.

**Packages removed due to [SLOP] verdict:** none  
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### System Architecture Diagram

```
User opens modal from Tools menu
         │
         ▼
MainViewModel.OpenBtcLoanSimulator() ──► IModalLauncher.ShowAsync(BtcLoanSimulator)
         │
         ▼
BtcLoanSimulatorView.axaml ──► BtcLoanSimulatorViewModel
         │
         ├─ BtcInput/FiatInput/CalendarDatePicker/ToggleButton two-way bindings
         │
         ├─ OnXChanged partial methods ──► Recalculate()
         │
         ├─ Builds BtcLoanSimulationInput (collateral sats, principal, APR, LTV, fees, dates, mode)
         │
         ├─ Calls BtcLoanSimulationCalculator.Calculate()
         │
         ├─ Reads RatesState.BitcoinPrice + FiatRates for conversion basis and sats
         │
         ▼
Results observable strings updated (total, principal, interest, fees,
liquidation price, effective APR, distance-to-liquidation, conversion basis)
```

### Recommended Project Structure

```
src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/
├── BtcLoanSimulatorView.axaml        # Modal XAML, CustomTitleBar, inputs/results grid
├── BtcLoanSimulatorView.axaml.cs     # Code-behind: Escape-to-close
├── BtcLoanSimulatorViewModel.cs      # Inputs, results, live recalc, formatting
└── BtcLoanSimulatorPositionItem.cs   # Phase 47 prefill list item (placeholder in Phase 45)

tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs
```

### Pattern 1: Live Recalculation with `ObservableProperty` Partial Methods

**What:** Mark input properties `[ObservableProperty]` and implement `partial void OnXChanged(...)` methods that call a single `Recalculate()`.

**When to use:** Anytime an input change must immediately update derived output, exactly like Leverage Simulator.

**Example:**

```csharp
// Source: src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs [VERIFIED: 216-221]
partial void OnIsLongChanged(bool value) => Recalculate();
partial void OnEntryPriceTextChanged(string value) => Recalculate();
partial void OnCollateralTextChanged(string value) => Recalculate();
partial void OnLeverageTextChanged(string value) => Recalculate();
partial void OnLiquidationPriceTextChanged(string value) => Recalculate();
partial void OnSimulatedPriceTextChanged(string value) => Recalculate();
```

### Pattern 2: Design-Time Constructor

**What:** A parameterless constructor that sets injected service fields to `null!` and initializes default collections/values so the Avalonia designer preview works.

**When to use:** Required for every `ValtModalViewModel` so `Design.DataContext` in XAML can instantiate the VM.

**Example:**

```csharp
// Source: src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs [VERIFIED: 60-66]
public LeverageSimulatorViewModel()
{
    _currencySettings = null!;
    _ratesState = null!;
    _queryDispatcher = null;
    _configurationManager = null;
}
```

### Pattern 3: Chromeless Modal Window with `CustomTitleBar`

**What:** `Window` with `SystemDecorations="None"`, `ExtendClientAreaToDecorationsHint="True"`, fixed `Min/Max Width/Height`, and a top `CustomTitleBar` handling drag + close.

**When to use:** Every Valt modal.

**Example:**

```xml
<!-- Source: src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml [VERIFIED: 1-20] -->
<Window xmlns="https://github.com/avaloniaui"
        d:DesignWidth="700" d:DesignHeight="615"
        MinWidth="700" MinHeight="615"
        MaxWidth="700" MaxHeight="615"
        WindowStartupLocation="CenterOwner"
        ExtendClientAreaToDecorationsHint="True"
        WindowDecorations="None"
        ExtendClientAreaTitleBarHeightHint="0">
```

### Pattern 4: Two-Way Value Binding for `BtcInput` / `FiatInput`

**What:** Bind to the control's typed value properties (`BtcValue`, `FiatValue`) with `Mode=TwoWay`; the controls internally format display text and parse keystrokes.

**When to use:** Any numeric BTC or fiat input in Valt.

**Example:**

```xml
<!-- Source: src/Valt.UI/UserControls/BtcInput.axaml [VERIFIED: 13-15] -->
<TextBox Text="{Binding DisplayValue, Mode=TwoWay, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType={x:Type userControls:BtcInput}}}" />

<!-- Source: src/Valt.UI/UserControls/FiatInput.axaml [VERIFIED: 25-28] -->
<TextBox Text="{Binding DisplayValue, Mode=TwoWay, RelativeSource={RelativeSource Mode=FindAncestor, AncestorType={x:Type userControls:FiatInput}}}" />
```

### Anti-Patterns to Avoid

- **Re-implementing the loan math in the ViewModel:** The calculator lives in `Valt.Core`; the VM only formats outputs. Duplicating act/365 or compound logic would invalidate the Phase 44 parity test guarantee.
- **Using plain `TextBox` for BTC/fiat amounts:** Would bypass the project's standardized decimal/sat handling and currency symbol display.
- **Adding persistence or CQRS commands:** The simulator is ephemeral by design; writing to the database belongs to Asset creation, not this modal.
- **Calling `RatesState` directly from XAML:** Always expose formatted strings/visibility flags from the VM to keep XAML declarative and testable.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Loan interest/fee math | Custom VM calculations | `BtcLoanSimulationCalculator.Calculate` [VERIFIED: src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs:8-58] | Already parity-tested against `BtcLoanDetails`; handles simple, compound, edge cases, schedule |
| BTC/sat numeric input | Plain `TextBox` | `BtcInput` | Built-in BTC/sats toggle and sat parsing [VERIFIED: src/Valt.UI/UserControls/BtcInput.axaml.cs:16-317] |
| Fiat numeric input | Plain `TextBox` | `FiatInput` | Built-in currency symbol, decimal formatting, key filtering [VERIFIED: src/Valt.UI/UserControls/FiatInput.axaml.cs:16-317] |
| Live price data plumbing | Custom service calls | `RatesState` | Singleton registered in DI; receives `LivePriceUpdateMessage` and exposes `BitcoinPrice` + `FiatRates` [VERIFIED: src/Valt.UI/State/RatesState.cs:12-55] |
| Color-coded risk text | Inline hex colors | `StringToColorBrushConverter` | Converts hex string to `SolidColorBrush`; used by Leverage Simulator [VERIFIED: src/Valt.UI/Converters/StringToColorBrushConverter.cs:8-23] |
| Currency list | Hard-coded dropdown | `IConfigurationManager.GetAvailableFiatCurrencies()` | Already wired to user settings [VERIFIED: src/Valt.Infra/Modules/Configuration/IConfigurationManager.cs:26] |
| Modal chrome | Custom window decorations | `CustomTitleBar` + `ValtBaseWindow` | Project standard; handles drag, focus, Escape close [VERIFIED: src/Valt.UI/Base/ValtBaseWindow.cs:9-97] |

**Key insight:** The hardest parts of this phase (loan math, numeric input controls, live price state, modal chrome) already exist. The planner should treat this as assembly and wiring, not invention.

## Common Pitfalls

### Pitfall 1: Avalonia Version Drift

**What goes wrong:** `AGENTS.md` says Avalonia 11.3, but the repo ships Avalonia 12.1.0. Following outdated 11.3-specific APIs (e.g., changed event signatures) can break the build.

**Why it happens:** Docs are stale relative to the actual packages.

**How to avoid:** Use the installed version from `Directory.Packages.props` (12.1.0). Existing views like `LeverageSimulatorView` already compile against it and are the source of truth.

**Warning signs:** Build errors mentioning missing Avalonia APIs after copying snippets from older docs.

### Pitfall 2: `ObservableProperty` Partial Method Signature Mismatch

**What goes wrong:** The compiler error CS8795 because `partial void OnSelectedCurrencyChanged(FiatCurrency value)` does not match the nullable generated property `FiatCurrency? SelectedCurrency`.

**Why it happens:** Source generators create properties with the exact declared type; the partial method must match that type exactly.

**How to avoid:** Declare the partial method parameter as nullable when the property is nullable:

```csharp
partial void OnSelectedCurrencyChanged(FiatCurrency? value);
partial void OnEffectiveDateChanged(DateTime? value);
```

### Pitfall 3: `CalendarDatePicker.SelectedDate` vs `DateOnly`

**What goes wrong:** The calculator expects `DateOnly`, but `CalendarDatePicker` binds to `DateTime?`. Passing a `DateTime` directly or forgetting `.Value` causes type errors.

**Why it happens:** Avalonia date controls use `DateTime?`; domain model uses `DateOnly`.

**How to avoid:** Expose `DateTime?` properties in the VM and convert inside `Recalculate()`:

```csharp
var startDate = StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value) : DateOnly.MinValue;
```

### Pitfall 4: `FiatInput` / `BtcInput` Two-Way Binding Recursion

**What goes wrong:** Setting the bound VM property inside `UpdateDisplayValue` or `OnXChanged` can cause repeated recalculation or UI flicker.

**Why it happens:** These controls update `DisplayValue` themselves on each keystroke, which writes back to the VM value property.

**How to avoid:** Bind to `BtcValue`/`FiatValue`, not `DisplayValue`. Trigger `Recalculate()` from `OnCollateralBtcValueChanged` / `OnAmountTakenFiatValueChanged`. Keep formatting logic idempotent.

### Pitfall 5: Culture-Sensitive Number Parsing

**What goes wrong:** `decimal.TryParse` with `CurrentUICulture` fails for users with comma decimal separators, or invariant parsing ignores user locale.

**Why it happens:** The `TextBox` text may come from user typing or copy/paste in either current-culture or invariant format.

**How to avoid:** Mirror the Leverage Simulator helper that tries current culture first, then invariant:

```csharp
// Source: src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs [VERIFIED: 283-294]
private static bool TryParseDecimal(string text, out decimal value)
{
    if (string.IsNullOrWhiteSpace(text)) { value = 0; return false; }
    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentUICulture, out value)
           || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
```

### Pitfall 6: Sats Visibility Tied to Price Availability

**What goes wrong:** Sat `TextBlock`s render `NaN`, `Infinity`, or nonsensical values when `RatesState.BitcoinPrice` is null.

**Why it happens:** Division by zero or formatting a nullable decimal that is null.

**How to avoid:** Expose `bool IsBtcPriceAvailable` and bind `IsVisible` of every sat value to it. Show the fallback copy in the conversion basis label when unavailable, per the UI-SPEC.

### Pitfall 7: Forgetting the Modal Result/Close Pattern

**What goes wrong:** Escape key does not close the modal, or the VM close command is not wired.

**Why it happens:** Chromeless windows do not get a system close button; the code-behind must handle `KeyDown`.

**How to avoid:** Add `KeyDown += OnKeyDown` in the code-behind constructor and close on `Key.Escape`, exactly like `LeverageSimulatorView` [VERIFIED: src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml.cs:8-22].

## Code Examples

### Example 1: Minimal `BtcLoanSimulatorViewModel` Skeleton

```csharp
// Pattern derived from src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs
public partial class BtcLoanSimulatorViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly IConfigurationManager? _configurationManager;

    // Inputs
    [ObservableProperty] private BtcValue _collateralBtcValue = BtcValue.Empty;
    [ObservableProperty] private FiatValue _amountTakenFiatValue = FiatValue.Empty;
    [ObservableProperty] private FiatValue _feesFiatValue = FiatValue.Empty;
    [ObservableProperty] private string _liquidationLtvText = string.Empty;
    [ObservableProperty] private string _aprText = string.Empty;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private bool _isSimple = true;

    // Currency
    [ObservableProperty] private FiatCurrency? _selectedCurrency;
    public ObservableCollection<FiatCurrency> AvailableCurrencies { get; } = new();

    // Results
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private string _totalRepayFiat = string.Empty;
    [ObservableProperty] private string _totalRepaySats = string.Empty;
    [ObservableProperty] private string _principalFiat = string.Empty;
    [ObservableProperty] private string _principalSats = string.Empty;
    [ObservableProperty] private string _interestFiat = string.Empty;
    [ObservableProperty] private string _interestSats = string.Empty;
    [ObservableProperty] private string _feesFiat = string.Empty;
    [ObservableProperty] private string _feesSats = string.Empty;
    [ObservableProperty] private string _liquidationPriceFiat = string.Empty;
    [ObservableProperty] private string _distanceToLiquidation = string.Empty;
    [ObservableProperty] private string _effectiveAprText = string.Empty;
    [ObservableProperty] private string _conversionBasis = string.Empty;
    [ObservableProperty] private bool _isBtcPriceAvailable;

    public string CurrencyCode => SelectedCurrency?.Code ?? _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
    public string CurrencySymbol => SelectedCurrency?.Symbol ?? "$";

    public BtcLoanSimulatorViewModel() { /* design-time nulls */ }
    public BtcLoanSimulatorViewModel(CurrencySettings cs, RatesState rs, IConfigurationManager cm) { /* init */ }

    partial void OnCollateralBtcValueChanged(BtcValue value) => Recalculate();
    partial void OnAmountTakenFiatValueChanged(FiatValue value) => Recalculate();
    partial void OnFeesFiatValueChanged(FiatValue value) => Recalculate();
    partial void OnLiquidationLtvTextChanged(string value) => Recalculate();
    partial void OnAprTextChanged(string value) => Recalculate();
    partial void OnStartDateChanged(DateTime? value) => Recalculate();
    partial void OnEndDateChanged(DateTime? value) => Recalculate();
    partial void OnIsSimpleChanged(bool value) => Recalculate();
    partial void OnSelectedCurrencyChanged(FiatCurrency? value)
    {
        OnPropertyChanged(nameof(CurrencyCode));
        OnPropertyChanged(nameof(CurrencySymbol));
        Recalculate();
    }

    private void Recalculate()
    {
        // parse, validate, build input, call calculator, format outputs
    }
}
```

### Example 2: Result Row XAML

```xml
<!-- Pattern derived from LeverageSimulatorView.axaml -->
<StackPanel Spacing="2">
    <TextBlock Classes="result-label" Text="{x:Static lang:language.BtcLoanSimulator_TotalRepay}" />
    <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBlock Classes="result-value" Text="{Binding TotalRepayFiat}" />
        <TextBlock Text="{Binding CurrencyCode}" VerticalAlignment="Center"
                   Foreground="{DynamicResource Text400Brush}" FontSize="12" />
    </StackPanel>
    <StackPanel Orientation="Horizontal" Spacing="6" IsVisible="{Binding IsBtcPriceAvailable}">
        <TextBlock Classes="result-value" Text="{Binding TotalRepaySats}" FontSize="14" />
        <TextBlock Text="sats" VerticalAlignment="Center"
                   Foreground="{DynamicResource Text400Brush}" FontSize="12" />
    </StackPanel>
</StackPanel>
```

### Example 3: Distance-to-Liquidation Formatting

```csharp
// Pattern: percentage distance, red when current price <= liquidation price
private void FormatDistance(decimal currentBtcPriceInCurrency, decimal liquidationPrice)
{
    if (liquidationPrice <= 0)
    {
        DistanceToLiquidation = string.Empty;
        return;
    }

    var pct = (currentBtcPriceInCurrency - liquidationPrice) / liquidationPrice;
    var sign = pct >= 0 ? "+" : "";
    DistanceToLiquidation = $"{sign}{pct:P2}";
    // bind Foreground via a string color property and StringToColorBrushConverter
    DistanceToLiquidationColor = pct < 0 ? "#F44336" : "#4CAF50";
}
```

### Example 4: NSubstitute-Based ViewModel Test

```csharp
// Pattern derived from tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs
[TestFixture]
public class BtcLoanSimulatorViewModelTests
{
    private RatesState _ratesState;
    private CurrencySettings _currencySettings;
    private IConfigurationManager _configManager;

    [SetUp]
    public void SetUp()
    {
        _ratesState = new RatesState();
        _currencySettings = new CurrencySettings(Substitute.For<ILocalDatabase>(),
                                                 Substitute.For<INotificationPublisher>());
        _configManager = Substitute.For<IConfigurationManager>();
        _configManager.GetAvailableFiatCurrencies().Returns(new List<string> { "USD", "BRL" });
    }

    private BtcLoanSimulatorViewModel CreateViewModel()
        => new(_currencySettings, _ratesState, _configManager);

    [Test]
    public void Recalculate_WithValidInputs_ShowsTotalRepay()
    {
        var vm = CreateViewModel();
        _ratesState.BitcoinPrice = 100_000m;
        _ratesState.FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m };

        vm.SelectedCurrency = FiatCurrency.Usd;
        vm.CollateralBtcValue = BtcValue.New(100_000_000); // 1 BTC
        vm.AmountTakenFiatValue = FiatValue.New(25_000m);
        vm.LiquidationLtvText = "80";
        vm.AprText = "12";
        vm.FeesFiatValue = FiatValue.New(100m);
        vm.StartDate = new DateTime(2024, 1, 1);
        vm.EndDate = new DateTime(2024, 1, 31);
        vm.IsSimple = true;

        Assert.That(vm.HasResults, Is.True);
        Assert.That(vm.TotalRepayFiat, Does.Contain("25"));
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Custom window decorations | `ValtBaseWindow` + `CustomTitleBar` | v0.4+ | Consistent chromeless modals across the app |
| Inline decimal math in VMs | Pure `Valt.Core` calculator (Phase 44) | Phase 44 | UI only formats; math is testable without UI/DI |
| Plain `TextBox` for money | `BtcInput`/`FiatInput` | v0.3+ | Standardized sat/fiat handling and currency symbols |
| WPF-style `{Binding}` | Compiled bindings (`x:DataType`) | Avalonia 11+ | Type-safe XAML; requires correct namespaces and types |

**Deprecated/outdated:**
- `AGENTS.md` Avalonia 11.3 mention: actual version is 12.1.0; follow `Directory.Packages.props`.
- Adding all localization strings in every phase: Phase 45 UI-SPEC explicitly allows English literals with Phase 48 handling full localization.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Phase 45 may use English literals in XAML and defer full pt-BR/es localization to Phase 48, per approved `45-UI-SPEC.md` | Standard Stack / Architecture | If localization must be added now, the plan needs extra tasks for resx/pt-BR/es/Designer.cs updates |
| A2 | "Distance to liquidation" should be shown as a percentage appended to the liquidation price row, consistent with the Leverage Simulator | Summary / Code Examples | If the user expects a separate fiat/sats row, the layout will need redesign |
| A3 | `RatesState.BitcoinPrice` is in USD and `RatesState.FiatRates` are relative to USD, matching the Leverage Simulator conversion logic | Common Pitfalls | If rates semantics differ, all sat/fiat conversions will be wrong |

**If this table is empty:** Not applicable — assumptions are listed above.

## Open Questions

1. **Distance-to-liquidation display format**
   - What we know: The Leverage Simulator shows a percentage distance.
   - What's unclear: Whether the BTC Loan Simulator should show percentage only, or also a fiat/sats price-difference row.
   - Recommendation: Implement percentage distance appended to the liquidation price row (e.g., "$31,250.00 (+25.00%)”) as the lowest-risk interpretation of the UI-SPEC. Add a planner note to confirm with user during `/gsd-verify-work`.

2. **Localization timing**
   - What we know: The approved UI-SPEC says English literals are acceptable in Phase 45, with Phase 48 adding all strings to `language.resx`, `language.pt-BR.resx`, `language.es.resx`, and `language.Designer.cs`.
   - What's unclear: Whether the implementation team will prefer to add English keys to the resx now to reduce Phase 48 work.
   - Recommendation: Follow the UI-SPEC literally for Phase 45 (English literals in XAML), but keep a list of the 20+ keys defined in the Copywriting Contract so Phase 48 can copy-paste them.

3. **Default end date**
   - What we know: UI-SPEC says default to today + 30 days.
   - What's unclear: Whether to use `DateTime.Today` or an injected `IClock` for testability.
   - Recommendation: Use an injected `IClock` (the project already uses `Valt.Core.Kernel.Abstractions.Time.IClock`) and default to `clock.Now.Date.AddDays(30)`; this keeps VM tests deterministic.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build, tests | Yes | 10.0.101 | — |
| Avalonia packages | UI layer | Yes | 12.1.0 | — |
| CommunityToolkit.Mvvm | ViewModels | Yes | 8.4.2 | — |
| NUnit / NSubstitute | Tests | Yes | 4.4.0 / 5.3.0 | — |
| LiteDB (in-memory) | Tests | Yes | 5.0.21 | — |

**Missing dependencies with no fallback:** none

**Missing dependencies with fallback:** none

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | NUnit 4.4.0 + NSubstitute 5.3.0 |
| Config file | none — configured via project references |
| Quick run command | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SIM-01 | User can input loan parameters (collateral, amount, LTV, dates, APR, fees, interest mode) | unit | `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"` | ❌ Wave 0 |
| SIM-02 | Results recalculate live as any input changes | unit | same filter | ❌ Wave 0 |
| SIM-05 | Total to repay + interest/fees breakdown shown | unit | same filter | ❌ Wave 0 |
| SIM-06 | Fiat + sats values with current BTC price conversion basis | unit | same filter | ❌ Wave 0 |
| SIM-07 | Liquidation BTC price derived from LTV and total debt | unit | same filter | ❌ Wave 0 |
| SIM-08 | Effective fee-inclusive APR displayed | unit | same filter | ❌ Wave 0 |
| SIM-09 | Distance to liquidation vs live BTC price displayed | unit | same filter | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test --filter "FullyQualifiedName~BtcLoanSimulatorViewModelTests"`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` — covers SIM-01/02/05/06/07/08/09
- [ ] `BtcLoanSimulatorViewModel` design-time constructor and live recalc behavior
- [ ] Build verification that new modal registers correctly in `ApplicationModalNames` + `Extensions.cs`

## Security Domain

This phase does not cross a trust boundary: the modal is ephemeral, performs no persistence, makes no network calls, and accepts only local numeric/date input. There are no secrets, authentication, or authorization surfaces.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-------------------|
| V2 Authentication | no | n/a |
| V3 Session Management | no | n/a |
| V4 Access Control | no | n/a |
| V5 Input Validation | yes | VM guards non-numeric / non-positive inputs; calculator throws on negative APR/fees and is wrapped in `try/catch` |
| V6 Cryptography | no | n/a |

### Known Threat Patterns for the Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Divide-by-zero / null price causing malformed sat values | Denial of Service (UI) | Hide sat `TextBlock`s when `RatesState.BitcoinPrice` is null; format defensively |
| Invalid numeric input from user | Tampering | `decimal.TryParse` current+invariant; silently clear results instead of crashing |
| Negative APR/fees reaching Core calculator | Tampering | VM validates or catches `ArgumentException` from `BtcLoanSimulationCalculator` |

## Sources

### Primary (HIGH confidence)
- `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml` — chromeless modal shell, styles, result rows [VERIFIED]
- `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs` — live recalc, currency/prefill loading, fiat/sats formatting [VERIFIED]
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationCalculator.cs` — input contract, math, output shape [VERIFIED]
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationInput.cs` [VERIFIED]
- `src/Valt.Core/Modules/Assets/Simulation/BtcLoanSimulationResult.cs` [VERIFIED]
- `src/Valt.UI/State/RatesState.cs` — live BTC price + fiat rates [VERIFIED]
- `src/Valt.UI/UserControls/BtcInput.axaml` / `.axaml.cs` — BTC/sat input behavior [VERIFIED]
- `src/Valt.UI/UserControls/FiatInput.axaml` / `.axaml.cs` — fiat input behavior [VERIFIED]
- `src/Valt.UI/Converters/StringToColorBrushConverter.cs` — color binding [VERIFIED]
- `src/Valt.UI/Extensions.cs` — modal DI registration and factory wiring [VERIFIED]
- `src/Valt.UI/Views/ApplicationModalNames.cs` — enum registration [VERIFIED]
- `src/Valt.UI/Base/ValtBaseWindow.cs` — chromeless window focus/close behavior [VERIFIED]
- `src/Valt.UI/Views/Main/MainView.axaml` + `MainViewModel.cs` — Tools menu wiring [VERIFIED]
- `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs` — VM test pattern with NSubstitute [VERIFIED]
- `.planning/phases/45-simulator-modal-ui-inputs-results-panel/45-UI-SPEC.md` — approved visual/interaction/copy contract [VERIFIED]
- `.planning/phases/44-core-loan-simulation-calculator/44-CONTEXT.md` — locked Phase 44 decisions affecting Phase 45 [VERIFIED]
- `Directory.Packages.props` — actual package versions [VERIFIED]

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` — SIM-01/02/05-09 scope [VERIFIED]
- `.planning/ROADMAP.md` — Phase 45 goal, success criteria, dependency on Phase 44 [VERIFIED]
- `AGENTS.md` — coding conventions, modal rules, localization instruction (with version caveat noted above) [VERIFIED]

### Tertiary (LOW confidence)
- None — all findings were verified against the repository.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versions verified from `Directory.Packages.props` and successful build
- Architecture: HIGH — direct precedent in `LeverageSimulatorView`/`LeverageSimulatorViewModel`
- Pitfalls: HIGH — all derived from verified code patterns

**Research date:** 2026-08-14  
**Valid until:** 2026-09-14 (stable Avalonia/MVVM stack)
