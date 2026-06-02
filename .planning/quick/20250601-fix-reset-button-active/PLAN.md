# Quick Task: Fix Reset Button Never Active

## Description
The Reset button introduced a bug where it never becomes active after setting a custom BTC price. The previous attempt used `[RelayCommand(CanExecute = nameof(IsCustomPriceActive))]` with an `[ObservableProperty]`, but the generated command was not properly re-evaluating CanExecute when the property changed.

## Affected Files
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

## Approach
Use a separate `CanResetFixedPrice()` method instead of referencing the property directly in `[RelayCommand(CanExecute)]`. The CommunityToolkit.Mvvm generator properly observes PropertyChanged events when CanExecute references a method rather than a generated property.
