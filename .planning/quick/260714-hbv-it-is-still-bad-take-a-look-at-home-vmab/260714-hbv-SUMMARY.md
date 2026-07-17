---
phase: quick
plan: 01
status: complete
completed_date: "2026-07-14"
duration_minutes: 5
tasks_completed: 1
tasks_total: 1
files_modified:
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
commits:
  - hash: 4607275
    message: "fix(quick-01): plain-text Name column and centered Restore Asset button in Sold Asset History"
---

# Phase quick Plan 01: Sold Asset History layout fix

Replaced the icon+name custom DataGrid column in the Sold Asset History modal with a plain text column and stopped the Restore Asset button from stretching full-width.

## What Changed

- `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml`
  - The Name column is now a `DataGridTextColumn` bound to `Name`, keeping the same header and `Width="*"`.
  - Removed the Material icon `TextBlock` binding to `Icon.Unicode` from the Name column.
  - The Restore Asset button now uses `HorizontalAlignment="Center"` instead of `Stretch`.

## Verification

- `dotnet build Valt.sln` completed successfully with 0 errors.
- XAML no longer contains a `TextBlock` binding to `Icon.Unicode` in the Name column.
- The Restore Asset button no longer declares `HorizontalAlignment="Stretch"`.

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

- [x] Modified XAML file exists and reflects the requested changes.
- [x] Commit 4607275 is in the repository history.
- [x] Solution builds without errors.

## Self-Check: PASSED
