---
status: complete
---

# Summary: Rewrite Reset Button from Scratch

## Completed

### Changes
**Files**: 
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Rewrote the Reset button implementation to use the simplest possible approach:

1. **ViewModel**: Removed the `CanResetFixedPrice()` method and changed `[RelayCommand(CanExecute = nameof(CanResetFixedPrice))]` back to plain `[RelayCommand]`. The command is always executable.

2. **XAML**: Added `IsEnabled="{Binding IsCustomPriceActive}"` directly to the Button element. This uses Avalonia's built-in property binding which is more reliable than CommunityToolkit.Mvvm's CanExecute generation.

This approach:
- Avoids all CommunityToolkit.Mvvm CanExecute generation issues
- Uses simple property binding that updates correctly when `IsCustomPriceActive` changes
- The button is visible but disabled (grayed out) when no custom price is set
- The button becomes enabled as soon as `IsCustomPriceActive` becomes true

## Verification
- Build succeeded: 0 errors
- Tests passed: 49/49 (Reports filter)
