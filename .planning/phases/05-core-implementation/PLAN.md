# Plan: Phase 5 — Core Implementation

**Phase:** 5  
**Name:** Core Implementation  
**Goal:** Refactor services to accept custom price, implement visual state changes, and ensure calculations use custom price only in Reports tab.  
**Requirements:** UI-04, UI-05, UI-06, DATA-01, DATA-02, DATA-03, DATA-04, DATA-05  
**Depends on:** Phase 4  

---

## Success Criteria

1. When custom price is active, "Alterar Preço" button replaces "Simular" ✓ (from Phase 4)
2. "Resetar" button appears when custom price is active and resets to real-time price ✓ (from Phase 4)
3. Visual indication clearly shows custom price is active (highlighted state, badge, or color change)
4. Dashboard calculations (total value, leveraged positions, etc.) use custom price when active
5. Custom price resets on app close (no persistence across sessions) ✓ (session-only by design)
6. Other app areas (transactions, goals, avg price, etc.) remain unaffected by custom price
7. Service layer refactored with optional custom price parameter without breaking existing consumers

---

## Tasks

### Task 1: Add visual indication for custom price active state
**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Add visual feedback when custom price is active:
- Change the fixed price bar border color or background when `IsCustomPriceActive` is true
- Add a small badge/indicator (e.g., "Simulação Ativa" badge)
- Change the BTC price text color to indicate it's simulated

**Implementation idea:**
```xml
<Border Classes="card" Margin="0,0,0,12" IsVisible="{Binding !IsSecureModeEnabled}">
    <Border.Background>
        <MultiBinding Converter="{x:Static BoolToBrushConverter.Instance}">
            <Binding Path="IsCustomPriceActive" />
            <Binding Source="{DynamicResource SimulationBackgroundBrush}" />
            <Binding Source="{DynamicResource CardBackgroundBrush}" />
        </MultiBinding>
    </Border.Background>
    ...
</Border>
```

Or simpler: use a `Border` with a different `Classes` when active:
```xml
<Border Classes.card="{Binding !IsCustomPriceActive}"
         Classes.card-simulation="{Binding IsCustomPriceActive}" ...
```

**Validation:** When custom price is set, the bar changes appearance. When reset, it returns to normal.

---

### Task 2: Create CustomBtcPriceState
**New file:** `src/Valt.UI/State/CustomBtcPriceState.cs`

Create a simple state object to hold the custom BTC price:
```csharp
public partial class CustomBtcPriceState : ObservableObject
{
    [ObservableProperty] private decimal? _customBtcPriceUsd;
    
    public bool IsActive => CustomBtcPriceUsd.HasValue;
    
    public decimal GetEffectiveBtcPriceUsd(decimal liveBtcPriceUsd)
    {
        return CustomBtcPriceUsd ?? liveBtcPriceUsd;
    }
}
```

Register in DI: `services.AddSingleton<CustomBtcPriceState>();`

**Validation:** State can be set and read. Returns live price when no custom price is set.

---

### Task 3: Modify GetAssetSummaryQuery to accept optional custom BTC price
**Files:**
- `src/Valt.App/Modules/Assets/Queries/GetAssetSummary/GetAssetSummaryQuery.cs`
- `src/Valt.App/Modules/Assets/Queries/GetAssetSummary/GetAssetSummaryHandler.cs`

Add optional parameter to the query:
```csharp
public sealed class GetAssetSummaryQuery : IQuery<AssetSummaryDto>
{
    public required string MainCurrencyCode { get; init; }
    public required decimal BtcPriceUsd { get; init; }
    public required IReadOnlyDictionary<string, decimal> FiatRates { get; init; }
    public decimal? CustomBtcPriceUsd { get; init; } // NEW
}
```

The handler should use `CustomBtcPriceUsd ?? BtcPriceUsd` for calculations.

**Validation:** Existing callers without `CustomBtcPriceUsd` continue to work. When provided, custom price is used.

---

### Task 4: Modify GetBtcLoansDashboardQuery to accept optional custom BTC price
**Files:**
- Find and update the BTC loans query files similarly

Add `CustomBtcPriceUsd` optional parameter. Use it when provided.

**Validation:** Same as Task 3.

---

### Task 5: Update ReportsViewModel to use custom price in calculations
**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

1. Inject `CustomBtcPriceState` into the constructor
2. When modal returns with a price:
   - Set `CustomBtcPriceState.CustomBtcPriceUsd = ConvertToUsd(price)`
   - Trigger recalculation of all affected dashboard cards
3. When reset:
   - Clear `CustomBtcPriceState.CustomBtcPriceUsd = null`
   - Trigger recalculation
4. Update `UpdateWealthData()` to recalculate with custom price:
   - Use the existing SimulatedPrices calculation pattern
   - `btcPortionInFiat = (wealthInSats / 100M) * customBtcPriceUsd * mainFiatRate`
   - `totalFiat = btcPortionInFiat + nonBtcWealth`
5. Update `UpdateLeveragePositionsDataAsync()` to pass custom price to the query
6. Update `UpdateBtcLoansDataAsync()` to pass custom price to the query

**Validation:** When custom price is set, Wealth dashboard shows recalculated values. Leverage and Loans dashboards also use custom price. Other tabs are unaffected.

---

### Task 6: Helper method for custom price conversion
**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

Add helper methods:
```csharp
private decimal GetBtcPriceInMainFiat()
{
    var btcPriceUsd = _customBtcPriceState.CustomBtcPriceUsd ?? _ratesState.BitcoinPrice ?? 0m;
    var mainCurrency = _currencySettings.MainFiatCurrency;
    var fiatRate = _ratesState.FiatRates?.GetValueOrDefault(mainCurrency, 1m) ?? 1m;
    return btcPriceUsd * fiatRate;
}

private decimal GetBtcPriceInUsd()
{
    return _customBtcPriceState.CustomBtcPriceUsd ?? _ratesState.BitcoinPrice ?? 0m;
}
```

**Validation:** Returns custom price when active, live price otherwise.

---

### Task 7: Update affected dashboard calculations

#### UpdateWealthData
Recalculate `totalInFiat`, `netWorthInSats`, and related values using custom BTC price.

#### UpdateLeveragePositionsDataAsync  
Pass custom price to `GetAssetSummaryQuery`.

#### UpdateBtcLoansDataAsync
Pass custom price to `GetBtcLoansDashboardQuery`.

#### UpdateAllTimeHighData (if applicable)
The ATH card shows "Required BTC Price to hit ATH" - this might need to be recalculated with custom price context.

**Validation:** All affected dashboards show values based on custom price when active.

---

## Test Strategy

- Unit tests: Verify `CustomBtcPriceState` returns correct effective price
- Integration tests: Verify `GetAssetSummaryQuery` uses custom price when provided
- Manual testing: 
  - Set custom price → verify Wealth dashboard changes
  - Set custom price → verify Leverage dashboard changes  
  - Set custom price → verify Loans dashboard changes
  - Reset price → verify all dashboards return to live price
  - Switch to Transactions tab → verify values unaffected

## Dependencies

- Phase 4 (UI Foundation) - must be complete
- `GetAssetSummaryQuery` and `GetBtcLoansDashboardQuery` handlers

## Estimated Effort

- Task 1 (visual indication): ~20 min
- Task 2 (CustomBtcPriceState): ~15 min
- Task 3 (GetAssetSummaryQuery): ~30 min
- Task 4 (GetBtcLoansDashboardQuery): ~20 min
- Task 5 (ViewModel updates): ~45 min
- Task 6 (helper methods): ~10 min
- Task 7 (dashboard calculations): ~30 min
- **Total: ~3 hours**

## Notes

- Historical reports (WealthOverview chart, AllTimeHigh history) use `IReportDataProvider` which loads historical BTC prices from the database. These should NOT be affected by custom price - they show actual history.
- Only the "current value" dashboards (Wealth, Leverage, Loans) should use custom price.
- The custom price should NOT be persisted to the database or configuration.
- The `CustomBtcPriceState` is a UI-layer state object (following the existing pattern of `RatesState`, `AccountsTotalState`).
