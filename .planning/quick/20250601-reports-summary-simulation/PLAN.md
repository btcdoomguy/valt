# Quick Task: Reports Summary Simulation Bar & Loan LTV Updates

## Description
1. **UI Fix**: Improve the appearance of the fixed price simulation bar in Reports > Summary. Center-align all elements in the top bar.
2. **Logic Fix**: When BTC price is simulated, update loan information in the summary to reflect the new LTV (Loan-to-Value) ratios based on the simulated price. Currently, collateral fiat value updates but LTV values remain based on live price.

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - UI layout
- `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs` - LTV calculation logic

## Approach
- In XAML: Change the Grid column definitions and alignment properties to center all elements
- In Handler: Recalculate per-loan LTV when CustomBtcPriceUsd is provided: `LTV = Debt / (CollateralSats * SimulatedPriceUsd / SatsPerBtc * FiatRate)`
