---
status: complete
---

# Summary: Fix Reset Button Never Active

## Completed

### Changes
**File**: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

Changed the `ResetFixedPrice` command from:
```csharp
[RelayCommand(CanExecute = nameof(IsCustomPriceActive))]
```

To:
```csharp
private bool CanResetFixedPrice() => IsCustomPriceActive;

[RelayCommand(CanExecute = nameof(CanResetFixedPrice))]
```

The CommunityToolkit.Mvvm source generator properly invalidates the command's CanExecute when an observable property changes if CanExecute references a method. When referencing the property directly, the generated command was not re-evaluating correctly.

## Verification
- Build succeeded: 0 errors
- Tests passed: 49/49 (Reports filter)
