---
quick_id: 260804-tyf
slug: restore-dashboarddata-right-text-foregro
mode: quick
status: planned
---

# Quick Task: Restore DashboardData right-text foreground color

## Task Boundary

The `RowItem.RightTextForeground` change in Phase 39 made the dashboard-card value text default to black, which is barely visible on the dark report background. Restore the previous `Text100Brush` foreground color while preserving the optional `RightTextForeground` override used by the burn-rate panel.

## Implementation Plan

- Add a `BrushOrDefaultConverter` in `DashboardDataConverters` that returns the bound brush when present, or falls back to the `Text100Brush` theme resource.
- Replace the `Foreground` binding on `RightTextBlock` in `DashboardDataUserControl.axaml` to use the converter instead of a `DynamicResource` fallback value (which Avalonia does not honor when the source is nullable).
- Fix the `ReportsViewModel.Initialize` task chain introduced by the Phase 39 review so panel refreshes are awaited (use `async` lambda + `Unwrap`).
- Update the two `ReportsViewModelTests.Initialize_*` assertions to wait for the fire-and-forget chain to run.
- Build and run `ReportsViewModelTests` to confirm the fix and tests.

## Verification

- `dotnet build Valt.sln` succeeds with 0 errors.
- `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` passes 9/9.
- Manual/visual check: dashboard-card value text is light (`Text100Brush`) again; the burn-rate vs-median row still shows green/red.
