---
status: complete
---

# Summary: Fix Reset Button, Modal Layout, and Input Focus

## Completed

### 1. Fixed Reset Button First-Click Issue
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

Changed `[RelayCommand]` to `[RelayCommand(CanExecute = nameof(IsCustomPriceActive))]` on the `ResetFixedPrice` method. This properly wires the command's `CanExecute` to the `IsCustomPriceActive` property, so the button is only enabled when a custom price is active.

**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Removed the redundant `IsEnabled="{Binding IsCustomPriceActive}"` binding from the Reset button, since the command now handles its own enabled state.

### 2. Fixed Modal Border Overflow
**File**: `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml`

Removed the hardcoded `Height="360"` from the Border element. The border now auto-sizes to fit its content, preventing it from overflowing the smaller window (280px height).

### 3. Auto-Focus and Select Text on Modal Open
**File**: `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml`

Added `x:Name="PriceTextBox"` to the TextBox for code-behind access.

**File**: `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml.cs`

Added `Opened` event handler that:
1. Focuses the price input TextBox
2. Selects all text so the user can immediately type a new value

The focus is posted with `DispatcherPriority.Render` to ensure the visual tree is ready.

## Verification
- Build succeeded: 0 errors
- Tests passed: 49/49 (Reports filter)
