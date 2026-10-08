# Phase 45: Simulator Modal UI (Inputs + Results Panel) - Pattern Map

**Mapped:** 2026-08-14
**Files analyzed:** 9
**Analogs found:** 9 / 9

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml` | component (view) | request-response | `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml` | exact |
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorView.axaml.cs` | component (code-behind) | event-driven | `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml.cs` | exact |
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulatorViewModel.cs` | component (viewmodel) | event-driven / transform | `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs` | exact |
| `src/Valt.UI/Views/Main/Modals/BtcLoanSimulator/BtcLoanSimulationItem.cs` | model (list item) | static config | `LeveragePositionItem` in `LeverageSimulatorViewModel.cs` | exact |
| `src/Valt.UI/Extensions.cs` | config (DI registration) | registration wiring | `src/Valt.UI/Extensions.cs` (`LeverageSimulator` lines) | role-match |
| `src/Valt.UI/Views/ApplicationModalNames.cs` | config (enum) | registration wiring | `src/Valt.UI/Views/ApplicationModalNames.cs` | exact |
| `src/Valt.UI/Views/Main/MainView.axaml` | component (view) | request-response | `src/Valt.UI/Views/Main/MainView.axaml` (Tools menu) | role-match |
| `src/Valt.UI/Views/Main/MainViewModel.cs` | component (viewmodel) | request-response | `src/Valt.UI/Views/Main/MainViewModel.cs` (`OpenLeverageSimulatorCommand`) | exact |
| `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` | test | verification | `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs` | role-match |

## Pattern Assignments

### `BtcLoanSimulatorView.axaml` (component, request-response)

**Analog:** `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml`

**Window / chromeless modal shell** (lines 1-20):
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:userControls="clr-namespace:Valt.UI.UserControls"
        xmlns:local="clr-namespace:Valt.UI.Lang"
        xmlns:vm="clr-namespace:Valt.UI.Views.Main.Modals.BtcLoanSimulator"
        mc:Ignorable="d"
        d:DesignWidth="800" d:DesignHeight="760"
        MinWidth="800" MinHeight="760"
        MaxWidth="800" MaxHeight="760"
        x:Class="Valt.UI.Views.Main.Modals.BtcLoanSimulator.BtcLoanSimulatorView"
        x:DataType="vm:BtcLoanSimulatorViewModel"
        Title="BTC Loan Simulator"
        WindowStartupLocation="CenterOwner"
        ExtendClientAreaToDecorationsHint="True"
        WindowDecorations="None"
        ExtendClientAreaTitleBarHeightHint="0">
```

**Custom title bar wiring** (lines 54-58):
```xml
<userControls:CustomTitleBar Title="{x:Static local:language.BtcLoanSimulator_Title}"
                             TitleBarPressed="CustomTitleBarButtonPressed"
                             CloseClick="CustomTitleBarCloseClicked" />
```

**Input/output grid layout** (line 60):
```xml
<Grid ColumnDefinitions="300, 24, *">
```

**Currency `ComboBox`** (lines 76-91):
```xml
<ComboBox ItemsSource="{Binding AvailableCurrencies}"
          SelectedItem="{Binding SelectedCurrency}"
          HorizontalAlignment="Stretch"
          MinHeight="32">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock>
                <Run Text="{Binding Code}" />
                <Run Text=" " />
                <Run Text="{Binding Symbol}" />
            </TextBlock>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

**Simple/Compound toggle buttons** (lines 95-104):
```xml
<StackPanel Orientation="Horizontal" Spacing="5">
    <ToggleButton Classes="position-toggle"
                  IsChecked="{Binding IsSimple}"
                  Command="{Binding SetSimpleCommand}"
                  Content="{x:Static local:language.BtcLoanSimulator_Simple}" />
    <ToggleButton Classes="position-toggle"
                  IsChecked="{Binding !IsSimple}"
                  Command="{Binding SetCompoundCommand}"
                  Content="{x:Static local:language.BtcLoanSimulator_Compound}" />
</StackPanel>
```

**Result rows with fiat + optional sats** (lines 164-185):
```xml
<StackPanel IsVisible="{Binding HasResults}" Spacing="18">
    <StackPanel Spacing="2">
        <TextBlock Classes="result-label" Text="{x:Static local:language.BtcLoanSimulator_TotalRepay}" />
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
</StackPanel>
```

**Result panel surface** (lines 146-151):
```xml
<Border Grid.Column="2"
        Background="{DynamicResource Background900Brush}"
        BorderBrush="{DynamicResource Background500Brush}"
        BorderThickness="1"
        CornerRadius="6"
        Padding="20, 15">
```

---

### `BtcLoanSimulatorView.axaml.cs` (component code-behind, event-driven)

**Analog:** `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorView.axaml.cs`

**Escape-to-close pattern** (lines 1-22):
```csharp
using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.BtcLoanSimulator;

public partial class BtcLoanSimulatorView : ValtBaseWindow
{
    public BtcLoanSimulatorView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
```

---

### `BtcLoanSimulatorViewModel.cs` (component viewmodel, event-driven / transform)

**Analog:** `src/Valt.UI/Views/Main/Modals/LeverageSimulator/LeverageSimulatorViewModel.cs`

**Imports block** (lines 1-18):
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Assets.Simulation;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.State;
```

**Fields and observable inputs** (lines 23-34):
```csharp
public partial class BtcLoanSimulatorViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly RatesState _ratesState;
    private readonly IConfigurationManager? _configurationManager;
    private readonly IClock _clock;

    // Inputs
    [ObservableProperty] private BtcValue _collateralBtcValue = BtcValue.Empty;
    [ObservableProperty] private FiatValue _amountTakenFiatValue = FiatValue.Empty;
    [ObservableProperty] private FiatValue _feesFiatValue = FiatValue.Empty;
    [ObservableProperty] private string _liquidationLtvText = string.Empty;
    [ObservableProperty] private string _aprText = string.Empty;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private bool _isSimple = true;
```

**Currency dropdown** (lines 37-42):
```csharp
    [ObservableProperty] private FiatCurrency? _selectedCurrency;
    public ObservableCollection<FiatCurrency> AvailableCurrencies { get; } = new();
```

**Result strings** (lines 45-53):
```csharp
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
    [ObservableProperty] private string _distanceToLiquidationColor = "#4CAF50";
```

**Design-time constructor** (lines 60-66):
```csharp
    public BtcLoanSimulatorViewModel()
    {
        _currencySettings = null!;
        _ratesState = null!;
        _configurationManager = null;
        _clock = null!;
    }
```

**Runtime constructor** (lines 68-78):
```csharp
    public BtcLoanSimulatorViewModel(
        CurrencySettings currencySettings,
        RatesState ratesState,
        IConfigurationManager configurationManager,
        IClock clock)
    {
        _currencySettings = currencySettings;
        _ratesState = ratesState;
        _configurationManager = configurationManager;
        _clock = clock;
    }
```

**OnBindParameterAsync + currency loading + defaults** (lines 80-111):
```csharp
    public override async Task OnBindParameterAsync()
    {
        LoadAvailableCurrencies();
        var today = _clock.GetCurrentLocalDate();
        StartDate = today.ToDateTime(TimeOnly.MinValue);
        EndDate = today.AddDays(30).ToDateTime(TimeOnly.MinValue);
        FeesFiatValue = FiatValue.New(0m);
    }

    private void LoadAvailableCurrencies()
    {
        var currencyCodes = _configurationManager?.GetAvailableFiatCurrencies()
            ?? new List<string> { FiatCurrency.Usd.Code };

        AvailableCurrencies.Clear();
        foreach (var code in currencyCodes)
        {
            try { AvailableCurrencies.Add(FiatCurrency.GetFromCode(code)); }
            catch { /* skip invalid */ }
        }

        if (AvailableCurrencies.Count == 0)
            AvailableCurrencies.Add(FiatCurrency.Usd);

        var mainCode = _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
        SelectedCurrency = AvailableCurrencies.FirstOrDefault(c => c.Code == mainCode)
            ?? AvailableCurrencies.First();
    }
```

**Partial-method live recalc wiring** (lines 216-221):
```csharp
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

    public string CurrencyCode => SelectedCurrency?.Code ?? _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;
    public string CurrencySymbol => SelectedCurrency?.Symbol ?? "$";
```

**Recalculate core** (derived from LeverageSimulator `Recalculate` lines 223-269 and `BtcLoanSimulationCalculator`):
```csharp
    private void Recalculate()
    {
        if (CollateralBtcValue.Sats <= 0 ||
            AmountTakenFiatValue.Value <= 0 ||
            !TryParseDecimal(LiquidationLtvText, out var liquidationLtv) || liquidationLtv <= 0 || liquidationLtv > 100 ||
            !TryParseDecimal(AprText, out var apr) || apr < 0 ||
            StartDate is null || EndDate is null || EndDate <= StartDate)
        {
            ClearResults();
            return;
        }

        try
        {
            var input = new BtcLoanSimulationInput
            {
                CollateralSats = CollateralBtcValue.Sats,
                PrincipalAmount = AmountTakenFiatValue.Value,
                CurrencyCode = CurrencyCode,
                Apr = apr / 100m,
                LiquidationLtv = liquidationLtv,
                Fees = FeesFiatValue.Value,
                StartDate = DateOnly.FromDateTime(StartDate.Value),
                EndDate = DateOnly.FromDateTime(EndDate.Value),
                InterestMode = IsSimple ? BtcLoanInterestMode.Simple : BtcLoanInterestMode.Compound
            };

            var result = BtcLoanSimulationCalculator.Calculate(input);
            FormatResults(result);
        }
        catch
        {
            ClearResults();
        }
    }
```

**Result formatting helpers** (inspired by LeverageSimulator lines 249-263 and `CurrencyDisplay`):
```csharp
    private void FormatResults(BtcLoanSimulationResult result)
    {
        var culture = CultureInfo.CurrentUICulture;
        var btcPriceInCurrency = GetBtcPriceInCurrency();
        IsBtcPriceAvailable = btcPriceInCurrency > 0;

        TotalRepayFiat = CurrencyDisplay.FormatFiat(result.TotalRepay, CurrencyCode);
        PrincipalFiat = CurrencyDisplay.FormatFiat(result.Principal, CurrencyCode);
        InterestFiat = CurrencyDisplay.FormatFiat(result.Interest, CurrencyCode);
        FeesFiat = CurrencyDisplay.FormatFiat(result.Fees, CurrencyCode);
        LiquidationPriceFiat = CurrencyDisplay.FormatFiat(result.LiquidationPrice, CurrencyCode);
        EffectiveAprText = (result.EffectiveApr * 100m).ToString("N2", culture) + "%";

        if (IsBtcPriceAvailable)
        {
            TotalRepaySats = FormatSats(result.TotalRepay, btcPriceInCurrency);
            PrincipalSats = FormatSats(result.Principal, btcPriceInCurrency);
            InterestSats = FormatSats(result.Interest, btcPriceInCurrency);
            FeesSats = FormatSats(result.Fees, btcPriceInCurrency);
            FormatDistance(btcPriceInCurrency, result.LiquidationPrice);
            ConversionBasis = $"1 BTC = {CurrencyDisplay.FormatFiat(btcPriceInCurrency, CurrencyCode)}";
        }
        else
        {
            TotalRepaySats = PrincipalSats = InterestSats = FeesSats = string.Empty;
            DistanceToLiquidation = string.Empty;
            ConversionBasis = "Current BTC price unavailable — sats values hidden";
        }

        HasResults = true;
    }

    private decimal GetBtcPriceInCurrency()
    {
        if (_ratesState?.BitcoinPrice is not {} btcPriceUsd || btcPriceUsd <= 0)
            return 0m;

        if (CurrencyCode == FiatCurrency.Usd.Code)
            return btcPriceUsd;

        if (_ratesState.FiatRates?.TryGetValue(CurrencyCode, out var fiatRate) == true)
            return btcPriceUsd * fiatRate;

        return 0m;
    }

    private static string FormatSats(decimal fiatAmount, decimal btcPriceInCurrency)
    {
        if (btcPriceInCurrency <= 0) return string.Empty;
        var sats = (long)(fiatAmount / btcPriceInCurrency * 100_000_000m);
        return CurrencyDisplay.FormatSatsAsNumber(sats);
    }

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
        DistanceToLiquidationColor = pct < 0 ? "#F44336" : "#4CAF50";
    }
```

**Clear results + toggle commands + parse helper** (from LeverageSimulator lines 271-306 and 283-294):
```csharp
    private void ClearResults()
    {
        TotalRepayFiat = TotalRepaySats = string.Empty;
        PrincipalFiat = PrincipalSats = string.Empty;
        InterestFiat = InterestSats = string.Empty;
        FeesFiat = FeesSats = string.Empty;
        LiquidationPriceFiat = string.Empty;
        DistanceToLiquidation = string.Empty;
        EffectiveAprText = string.Empty;
        ConversionBasis = string.Empty;
        IsBtcPriceAvailable = false;
        HasResults = false;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0; return false; }
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentUICulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    [RelayCommand] private void SetSimple() => IsSimple = true;
    [RelayCommand] private void SetCompound() => IsSimple = false;
```

---

### `BtcLoanSimulationItem.cs` (model, static config)

**Analog:** `LeveragePositionItem` class at bottom of `LeverageSimulatorViewModel.cs`

**Placeholder item pattern** (lines 315-328):
```csharp
public class BtcLoanSimulationItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AssetId { get; init; }
    public bool IsNewSimulation { get; init; }

    // Phase 47 fields (leave in place, unused in Phase 45)
    public long CollateralSats { get; init; }
    public decimal PrincipalAmount { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal Apr { get; init; }
    public decimal LiquidationLtv { get; init; }
    public decimal Fees { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public BtcLoanInterestMode InterestMode { get; init; }

    public override string ToString() => DisplayName;
}
```

---

### `src/Valt.UI/Extensions.cs` (config, registration wiring)

**Analog:** `src/Valt.UI/Extensions.cs` (`LeverageSimulator` lines)

**Add using** (after line 46):
```csharp
using Valt.UI.Views.Main.Modals.BtcLoanSimulator;
```

**Register ViewModel** (after line 148):
```csharp
services.AddTransient<BtcLoanSimulatorViewModel>();
```

**Add factory case** (after line 269):
```csharp
ApplicationModalNames.BtcLoanSimulator => new BtcLoanSimulatorView()
{
    DataContext = services.GetRequiredService<BtcLoanSimulatorViewModel>(),
},
```

---

### `src/Valt.UI/Views/ApplicationModalNames.cs` (config enum)

**Analog:** `src/Valt.UI/Views/ApplicationModalNames.cs`

**Add enum member** (after the highest existing value, currently 41):
```csharp
BtcLoanSimulator = 42,
```

---

### `src/Valt.UI/Views/Main/MainView.axaml` (component, request-response)

**Analog:** `src/Valt.UI/Views/Main/MainView.axaml` Tools `MenuFlyout` (lines 232-255)

**Add menu item** (inside the Tools `MenuFlyout` after Leverage Simulator):
```xml
<MenuItem Header="BTC Loan Simulator"
          Command="{Binding OpenBtcLoanSimulatorCommand}" />
```

---

### `src/Valt.UI/Views/Main/MainViewModel.cs` (component viewmodel, request-response)

**Analog:** `OpenLeverageSimulatorCommand` (lines 378-382)

**Add command**:
```csharp
[RelayCommand]
private async Task OpenBtcLoanSimulator()
{
    await _modalLauncher.ShowAsync(ApplicationModalNames.BtcLoanSimulator, Window!);
}
```

---

### `tests/Valt.Tests/UI/Screens/BtcLoanSimulatorViewModelTests.cs` (test, verification)

**Analog:** `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs`

**Test skeleton** (lines 17-36):
```csharp
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.BtcLoanSimulator;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class BtcLoanSimulatorViewModelTests
{
    private RatesState _ratesState;
    private CurrencySettings _currencySettings;
    private IConfigurationManager _configManager;
    private IClock _clock;

    [SetUp]
    public void SetUp()
    {
        _ratesState = new RatesState();
        _currencySettings = new CurrencySettings(Substitute.For<ILocalDatabase>(),
                                                 Substitute.For<INotificationPublisher>());
        _configManager = Substitute.For<IConfigurationManager>();
        _configManager.GetAvailableFiatCurrencies().Returns(new List<string> { "USD", "BRL" });
        _clock = Substitute.For<IClock>();
        _clock.GetCurrentLocalDate().Returns(new DateOnly(2024, 1, 1));
    }

    private BtcLoanSimulatorViewModel CreateViewModel()
        => new(_currencySettings, _ratesState, _configManager, _clock);
```

**Live recalc / results test** (pattern from lines 64-99):
```csharp
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
```

## Shared Patterns

### Chromeless Modal Shell + Escape-to-Close
**Apply to:** `BtcLoanSimulatorView.axaml` and `.axaml.cs`
**Sources:** `ValtBaseWindow.cs` lines 20-41, `LeverageSimulatorView.axaml.cs` lines 8-21
- Window extends client area, `WindowDecorations="None"`, uses `CustomTitleBar`.
- Code-behind handles `KeyDown` and closes on `Key.Escape`.

### Custom Title Bar Drag / Close
**Apply to:** `BtcLoanSimulatorView.axaml`
**Source:** `ValtBaseWindow.cs` lines 75-96
- Wire `TitleBarPressed="CustomTitleBarButtonPressed"` and `CloseClick="CustomTitleBarCloseClicked"`.
- These protected methods are inherited from `ValtBaseWindow`.

### `ObservableProperty` Live Recalc
**Apply to:** `BtcLoanSimulatorViewModel.cs`
**Source:** `LeverageSimulatorViewModel.cs` lines 216-221
- Mark inputs `[ObservableProperty]` and implement `partial void OnXChanged(...) => Recalculate();`.
- For nullable properties use nullable parameter types (`FiatCurrency?`, `DateTime?`).

### Currency List from `IConfigurationManager`
**Apply to:** `BtcLoanSimulatorViewModel.cs`
**Source:** `LeverageSimulatorViewModel.cs` lines 87-111
- Read `GetAvailableFiatCurrencies()`, map to `FiatCurrency.GetFromCode(code)`, default to USD.
- Select main currency from `CurrencySettings.MainFiatCurrency`.

### `BtcInput` / `FiatInput` Two-Way Binding
**Apply to:** `BtcLoanSimulatorView.axaml`
**Sources:** `BtcInput.axaml` line 14, `FiatInput.axaml` lines 19-34, `UpdateLoanStateView.axaml` lines 124-126
- Bind `BtcValue="{Binding CollateralBtcValue, Mode=TwoWay}"`.
- Bind `FiatValue="{Binding AmountTakenFiatValue, Mode=TwoWay}"` plus `CurrencySymbol` / `SymbolOnRight`.
- Do **not** bind to `DisplayValue`; VM reacts to `OnCollateralBtcValueChanged` / `OnAmountTakenFiatValueChanged`.

### `CalendarDatePicker` Binding
**Apply to:** `BtcLoanSimulatorView.axaml`
**Source:** `UpdateLoanStateView.axaml` lines 114-118
```xml
<CalendarDatePicker HorizontalAlignment="Left"
                    SelectedDateFormat="Short"
                    IsTodayHighlighted="True"
                    Height="36"
                    SelectedDate="{Binding StartDate}" />
```

### Live BTC/Fiat Conversion via `RatesState`
**Apply to:** `BtcLoanSimulatorViewModel.cs`
**Source:** `RatesState.cs` lines 12-19, `LeverageSimulatorViewModel.cs` lines 167-189
- `RatesState.BitcoinPrice` is USD; `FiatRates[currencyCode]` converts to selected currency.
- Hide sat values when price is unavailable; show conversion-basis fallback copy.

### Color-Coded Risk Text
**Apply to:** `BtcLoanSimulatorViewModel.cs` + `.axaml`
**Source:** `StringToColorBrushConverter.cs` lines 8-17
```xml
<TextBlock Foreground="{Binding DistanceToLiquidationColor, Converter={StaticResource StringToColorBrushConverter}}" />
```
- VM exposes hex color strings; converter creates `SolidColorBrush`.

### Culture-Safe Decimal Parsing
**Apply to:** `BtcLoanSimulatorViewModel.cs`
**Source:** `LeverageSimulatorViewModel.cs` lines 283-294
```csharp
private static bool TryParseDecimal(string text, out decimal value)
{
    if (string.IsNullOrWhiteSpace(text)) { value = 0; return false; }
    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentUICulture, out value)
           || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
```

### Design-Time vs Runtime Constructors
**Apply to:** `BtcLoanSimulatorViewModel.cs`
**Source:** `LeverageSimulatorViewModel.cs` lines 57-78
- Parameterless constructor sets injected fields to `null!` / `null` for Avalonia designer.
- Runtime constructor takes `CurrencySettings`, `RatesState`, `IConfigurationManager`, `IClock`.

## No Analog Found

None. All Phase 45 files have direct codebase precedents.

## Metadata

**Analog search scope:**
- `src/Valt.UI/Views/Main/Modals/LeverageSimulator/`
- `src/Valt.UI/Views/Main/Modals/UpdateLoanState/`
- `src/Valt.UI/UserControls/`
- `src/Valt.UI/Base/`
- `src/Valt.UI/State/`
- `src/Valt.UI/Converters/`
- `src/Valt.Core/Modules/Assets/Simulation/`
- `src/Valt.UI/Extensions.cs`
- `src/Valt.UI/Views/ApplicationModalNames.cs`
- `src/Valt.UI/Views/Main/MainView.axaml` + `MainViewModel.cs`
- `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs`

**Files scanned:** 18
**Pattern extraction date:** 2026-08-14
