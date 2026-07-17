---
phase: quick
plan: 260714-exz
subsystem: Assets UI
status: complete
tags:
  - assets
  - sold-asset-history
  - icon
  - ui
requires:
  - HISTORY-GRID-ICON
provides: []
affects:
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
  - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
tech-stack:
  added: []
  patterns:
    - Avalonia DataGrid template column
    - Core.Common.Icon restoration from stored ID
key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs
    - src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml
decisions:
  - Auto-approved checkpoint:human-verify because workflow.auto_advance=true; the visual verification steps were captured for manual confirmation later.
metrics:
  duration: 6 min
  completed_date: 2026-07-14T00:00:00Z
  tasks_total: 3
  tasks_completed: 3
  files_modified: 2
---

# Phase quick Plan 260714-exz: Sold Asset History Icon Glyph Summary

Fixed the Sold Asset History modal so the Name column renders the asset's Material Design icon glyph instead of the raw serialized `Icon;Name;Unicode;Color` string.

## What Changed

- `SoldAssetItemViewModel.Icon` is now a `Valt.Core.Common.Icon` parsed from the DTO's stored icon ID via `Icon.RestoreFromId(dto.Icon)`.
- The design-time helper `CreateDesignTimeItem` accepts a `Core.Common.Icon` and passes `Icon.Empty` for sample rows.
- The `DataGrid` Name column's icon `TextBlock` now binds `Text="{Binding Icon.Unicode}"` so it displays the actual Unicode glyph.

## Verification

1. `dotnet build Valt.sln` passes with 0 errors.
2. The visual runtime check (open Sold Asset History modal) is documented in the checkpoint and should be confirmed manually.

## Deviations from Plan

None - plan executed exactly as written.

## Auto-Approved Checkpoint

- **Task 3** (`checkpoint:human-verify`) was auto-approved because `workflow.auto_advance=true`.
- The manual verification steps remain available for the user to run when convenient:
  1. `dotnet run --project src/Valt.UI/Valt.UI.csproj`
  2. Open the Assets tab, mark or open a sold asset, then open the Sold Asset History modal.
  3. Confirm the Name column shows the asset name preceded by the correct glyph, with no semicolon-separated icon string.

## Self-Check: PASSED

- `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryViewModel.cs` exists.
- `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml` exists.
- Commit `b62d734` exists.
- Commit `87a83ae` exists.
- `dotnet build Valt.sln` reports 0 errors.
