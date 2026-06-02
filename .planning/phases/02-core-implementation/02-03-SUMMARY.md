---
phase: 02-core-implementation
plan: 03
subsystem: ui
tags: [avalonia, livecharts, mvvm, treeview, checkbox, debounce, indicators]

requires:
  - phase: 02-02
    provides: SpendingEvolutionChartData, CategorySelectionItem, ViewModel foundation

provides:
  - Complete SpendingEvolutionView.axaml with TreeView, dual-axis chart, time range dropdown, indicators, warning banner
  - ViewModel with cost of living calculations (fiat + BTC percentage increase)
  - Pre-selection logic for right-click (single category) vs menu (all categories) entry points
  - Parent-child category tree with checkbox propagation
  - Debounced category selection change handling to prevent rapid re-queries

affects:
  - 02-core-implementation
  - SpendingEvolution modal UI

tech-stack:
  added: []
  patterns:
    - "Brush-based color coding computed in ViewModel instead of value converter"
    - "150ms debounce via CancellationTokenSource for rapid checkbox change events"
    - "Parent-child tree built from flat DTO list with recursive SubNodes wiring"

key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml
    - src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs

key-decisions:
  - "Used FiatIncreasePercentText/BtcIncreasePercentText with FiatIncreaseBrush/BtcIncreaseBrush instead of IncreaseToColorConverter (converter didn't exist)"
  - "Used SemanticWarning800Brush for warning banner background and SemanticWarning200Brush for icon, Text100Brush for text"
  - "Added 150ms debounce on category checkbox changes to prevent cascading re-queries when parent propagates to children"
  - "Built recursive category tree from flat CategoryDTO list with two-pass approach (create items, then wire parent-child)"

patterns-established:
  - "ViewModel-computed brushes: color coding determined in ViewModel and bound as SolidColorBrush properties"
  - "CTS debounce: use CancellationTokenSource with Task.Delay for lightweight UI debouncing"

requirements-completed: [UI-03, UI-06, UI-08, UI-09, DATA-04, DATA-05, DATA-06]

duration: 25min
completed: 2025-05-27
---

# Phase 02 Plan 03: Spending Evolution Modal Layout and Integration Summary

**Complete Spending Evolution modal UI with category TreeView, dual-axis LiveCharts chart, time range selector, cost of living indicators, and missing data warning banner — all wired to ViewModel with pre-selection logic.**

## Performance

- **Duration:** 25 min
- **Started:** 2025-05-27T16:17:00Z
- **Completed:** 2025-05-27T16:42:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Replaced placeholder layout with full Grid-based modal (title bar, content area, warning banner)
- Implemented category selector with TreeView showing checkboxes, Material Design icons, and names
- Added dual-axis CartesianChart binding to SpendingEvolutionChartData (fiat left Y, sats right Y)
- Added time range ComboBox with 12/24/36/48/60 month options
- Implemented cost of living indicators showing percentage increase for fiat and BTC with color coding
- Added missing PriceInSats warning banner with semantic warning colors
- Implemented pre-selection logic: right-click opens with single category, menu opens with all categories
- Built parent-child category tree from flat DTO list with recursive SubNodes
- Added 150ms debounce on checkbox changes to prevent cascading re-queries
- Added Escape key binding to CloseCommand

## Task Commits

Each task was committed atomically:

1. **Task 1: Update SpendingEvolutionView.axaml with Full Layout** - `de3963e` (feat)
2. **Task 2: Add Cost of Living Indicators, Warnings, and Pre-selection Logic to ViewModel** - `a9269c2` (feat)

**Plan metadata:** `a9269c2` (docs: complete plan)

## Files Created/Modified
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml` - Full modal layout with TreeView, CartesianChart, ComboBox, indicators, warning banner
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs` - ViewModel with cost of living calculations, tree building, debounced reloads, pre-selection, CloseCommand

## Decisions Made
- **No IncreaseToColorConverter exists** — computed SolidColorBrush properties in ViewModel (FiatIncreaseBrush/BtcIncreaseBrush) instead of creating a new converter. This keeps color logic testable and avoids adding a one-off converter.
- **Warning banner uses SemanticWarning800Brush background** — chosen over non-existent `WarningBackground` dynamic resource for consistency with existing semantic color system.
- **150ms debounce via CancellationTokenSource** — instead of `_isDataLoading` flag alone, added CTS debounce to handle rapid checkbox changes (especially parent-child propagation) more gracefully. Only the last change triggers a reload.
- **Recursive tree building from flat list** — two-pass approach: first create all CategorySelectionItem objects, then wire parent-child SubNodes relationships. Only root items are added to the top-level ObservableCollection.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

| File | Line | Description | Reason |
|------|------|-------------|--------|
| SpendingEvolutionViewModel.cs | ~93 | `PreSelectedCategoryId` setter only handles string parameter type | Future plan may need to handle other parameter types (e.g., array of category IDs) |

## Threat Flags

No new threat surface introduced beyond what was already in the threat model. All mitigations applied:
- T-02-08 (DoS via rapid re-queries): Mitigated via 150ms debounce and `_isDataLoading` flag

## Issues Encountered

- `IncreaseToColorConverter` referenced in plan did not exist in codebase. Solved by computing brushes in ViewModel.
- `WarningBackground`/`WarningForeground` dynamic resources did not exist. Solved by using existing `SemanticWarning800Brush` and `SemanticWarning200Brush`.

## Next Phase Readiness

- Spending Evolution modal UI is fully wired and ready for integration testing
- Next phase should focus on: menu/right-click wiring in MainViewModel, integration with transaction grid context menu, and end-to-end testing

## Self-Check: PASSED

- [x] File `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml` exists and contains all required elements
- [x] File `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs` exists and has all required properties/methods
- [x] Commit `de3963e` exists in git log
- [x] Commit `a9269c2` exists in git log
- [x] Build succeeds (`dotnet build src/Valt.UI/Valt.UI.csproj` exits 0)

---
*Phase: 02-core-implementation*
*Completed: 2025-05-27*
