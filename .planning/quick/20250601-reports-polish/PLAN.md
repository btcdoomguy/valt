# Quick Task: Reports Polish Fixes

## Description
1. Remove the unused "BTC Price Simulator" caption and icon from Reports > Summary
2. Reduce the height of the FixedPriceConfig modal window (too much empty space)
3. Fix the Reset button that only works on the second click

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - Remove SimulatedPrices dashboard, fix Reset button
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml` - Reduce window height

## Approach
- Remove SimulatedPrices Grid from ReportsView.axaml
- Change window height from 400 to 280 in FixedPriceConfigView.axaml
- Change Reset button from IsVisible to IsEnabled to fix Avalonia binding issue on first click
