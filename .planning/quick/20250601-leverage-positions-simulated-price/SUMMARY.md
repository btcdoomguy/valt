---
status: complete
---

# Summary: Simulated BTC Price for Leveraged Positions

## Completed

### Changes
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

Added `CalculateSimulatedLeveragedPnl()` helper method that:
1. Checks if a custom BTC price is active
2. Verifies if the position is a BTC position (Symbol starts with "BTC")
3. Converts the simulated BTC price from USD to the position's currency
4. Recalculates P&L using the leveraged position formula:
   - For long: `PnL = Collateral * ((SimulatedPrice - EntryPrice) / EntryPrice) * Leverage`
   - For short: `PnL = -Collateral * ((SimulatedPrice - EntryPrice) / EntryPrice) * Leverage`

Modified the P&L aggregation loop in `UpdateLeveragePositionsDataAsync()` to use the helper, ensuring that:
- Total P&L in main fiat currency reflects simulated prices
- P&L in BTC (sats) is also updated accordingly
- Non-BTC leveraged positions remain unaffected

## Verification
- Build succeeded: 0 errors
- Tests passed: 49/49 (Reports filter)
