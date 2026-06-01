# Quick Task: Simulated BTC Price for Leveraged Positions

## Description
When a simulated BTC price is active, the leveraged positions panel in Reports > Summary should also reflect the simulated price. Currently, the P&L values remain based on the live BTC price even when a custom price is set.

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - Leveraged positions calculation logic

## Approach
- Add `CalculateSimulatedLeveragedPnl()` helper method that recalculates P&L using the simulated BTC price
- Only recalculate for BTC positions (Symbol starts with "BTC")
- Convert simulated price from USD to position's currency using fiat rates
- Use standard leveraged position formula: P&L = Collateral * ((Price - EntryPrice) / EntryPrice) * Leverage
- Update the P&L aggregation loop to use the helper
