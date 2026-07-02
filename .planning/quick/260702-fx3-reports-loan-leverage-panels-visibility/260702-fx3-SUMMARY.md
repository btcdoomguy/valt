---
status: complete
quick_id: 260702-fx3
date: 2026-07-02
---

# Quick Task 260702-fx3 Summary

## What was done

Fixed a regression in the Reports tab where the Loan and Leverage panels were not displayed after the panel VM refactor.

## Changes

- `src/Valt.UI/Views/Main/Tabs/Reports/Panels/DashboardPanelViewModel.cs`
  - Changed `_isVisible` default from `true` to `false`. This ensures that when a panel refresh finds relevant data and sets `IsVisible = true`, the `PropertyChanged` event is raised and `ReportsViewModel` forwards the new visibility to its bound properties.

- `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs`
  - Added `Initialize_Should_Refresh_LeveragePanel`.
  - Added `Initialize_Should_Refresh_BtcLoansPanel`.
  - Added `Panel_IsVisible_Changes_Are_Forwarded_To_ReportsViewModel`.
  - Added `Panel_Initial_Visibility_Is_Forwarded_To_ReportsViewModel`.

## Verification

- `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests|FullyQualifiedName~LeveragePositionsPanelViewModelTests|FullyQualifiedName~BtcLoansPanelViewModelTests"` passed (14 tests).
- `dotnet build Valt.sln` succeeded.
- Full test suite: 1611 passed, 3 failed (pre-existing CoinGecko API 403 errors and `UiViewModels_Should_Not_Contain_GetAwaiterGetResult_BlockingPattern` architecture test that flags the `.GetAwaiter().GetResult()` pattern introduced by the panel VM refactor, unrelated to this visibility fix).

## Commit

- Code/test changes: 0d43acb
- Docs: 95dad34
