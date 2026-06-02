# Quick Task: Fix Reset Button, Modal Layout, and Input Focus

## Description
1. Fix the Reset button that doesn't work on first click after setting a custom price
2. Fix the FixedPriceConfig modal border overflowing at the bottom after height reduction
3. Auto-focus and select all text in the price input when the FixedPriceConfig modal opens

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - Add CanExecute to ResetFixedPrice command
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - Remove IsEnabled binding from Reset button
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml` - Remove hardcoded border height, add x:Name to TextBox
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml.cs` - Add focus and select all on open

## Approach
- Use `[RelayCommand(CanExecute = nameof(IsCustomPriceActive))]` to properly enable/disable the command
- Remove hardcoded `Height="360"` from the Border to allow auto-sizing
- Handle `Opened` event in code-behind to focus TextBox and select all text
