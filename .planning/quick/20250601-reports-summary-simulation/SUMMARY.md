---
status: complete
---

# Summary: Reports Summary Simulation Bar & Loan LTV Updates

## Completed

### 1. UI Fix - Simulation Bar Center Alignment
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Changed the Fixed Price simulation bar layout from `ColumnDefinitions="Auto,*,Auto,Auto"` to `ColumnDefinitions="*,Auto,Auto,Auto,*"` and reorganized elements into 3 centered StackPanels:
- Column 1: Title + simulation badge
- Column 2: Current price display (with horizontal margins for spacing)
- Column 3: Reset button + Simulate button

All elements are now properly centered within the card.

### 2. Logic Fix - Simulated LTV Recalculation
**File**: `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs`

Added `CalculateSimulatedLtv()` helper that computes LTV based on the simulated BTC price:
```
LTV = DebtInMainCurrency / (CollateralSats * SimulatedPriceUsd / SatsPerBtc * MainFiatRate) * 100
```

When `CustomBtcPriceUsd` is provided, the following metrics are now recalculated:
- **Debt-weighted average LTV** - uses simulated LTVs weighted by debt
- **Highest LTV** - maximum simulated LTV across all loans
- **Closest distance to liquidation** - based on simulated LTVs
- **Health breakdown** - healthy/warning/danger counts recalculated using `MarginCallLtv` and `LiquidationLtv` thresholds against simulated LTVs

When no custom price is active, original persisted values are used (no behavior change).

## Verification
- Build succeeded: 0 errors
- Tests passed: 14/14 (GetBtcLoansDashboard filter)
