---
phase: 39-spending-analytics-reports-ui
plan: 06
subsystem: ui
tags: [avalonia, mvvm, reports, category-filter, localization, nunit]

requires:
  - phase: 39-05
    provides: IConfigurationManager.GetReportsAnalyticsCategoryFilterExcludedIds / SetReportsAnalyticsCategoryFilterExcludedIds

provides:
  - ReportsCategoryFilterConfig modal (AXAML + ViewModel + code-behind)
  - ApplicationModalNames.ReportsCategoryFilterConfig enum value
  - Modal DI registration and factory mapping in Extensions.cs
  - Centralized category filter icon button in Reports Summary header
  - ReportsViewModel command, persistence, and query wiring
  - BurnRatePanelViewModel.SetCategoryFilter and CategoryIds propagation
  - English resx strings for the modal
  - ReportsViewModelTests covering command, filter computation, and propagation

affects:
  - 39-spending-analytics-reports-ui
  - reports-tab
  - spending-analytics

tech-stack:
  added: []
  patterns:
    - "Mirror StatisticsConfig modal pattern for a new Reports-specific category filter modal"
    - "ReportsViewModel loads excluded category IDs once and computes included IDs on demand"
    - "BurnRatePanelViewModel receives selected category IDs via SetCategoryFilter before every RefreshAsync"

key-files:
  created:
    - src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigView.axaml
    - src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigView.axaml.cs
    - src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigViewModel.cs
  modified:
    - src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigViewModel.cs
    - src/Valt.UI/Views/ApplicationModalNames.cs
    - src/Valt.UI/Extensions.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.Designer.cs
    - tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs

key-decisions:
  - "Mirrored StatisticsConfig modal exactly for the new ReportsCategoryFilterConfig modal to keep UI/UX consistent"
  - "Used the exact IConfigurationManager method names from 39-05: GetReportsAnalyticsCategoryFilterExcludedIds / SetReportsAnalyticsCategoryFilterExcludedIds"
  - "English-only resx keys per D-21; pt-BR/es left for Phase 43 localization pass"
  - "Kept the centralized filter independent from the existing Categories-section filter (SelectedCategories/OnSelectedFiltersChanged)"
  - "Set category filter on BurnRatePanelViewModel before every RefreshAsync so the panel never uses stale IDs"

patterns-established:
  - "New category filter modals mirror StatisticsConfig: same CheckBoxListSelector, same Response(bool Ok) result, same save/cancel commands"
  - "ReportsViewModel caches excluded analytics category IDs and computes included IDs from AvailableCategories on demand"
  - "Any caller refreshing _burnRatePanel must first call _burnRatePanel.SetCategoryFilter(GetSelectedAnalyticsCategoryIds())"

requirements-completed: [SPA-01, SPA-02, SPA-03]

duration: 24min
completed: 2026-08-05
status: complete
---

# Phase 39 Plan 06: Centralized Reports Category Filter UI Summary

**Added a centralized Reports tab category exclusion filter that opens a configuration modal, persists excluded categories via IConfigurationManager, and feeds selected IDs into Savings rate, Burn rate, and Fixed vs variable analytics.**

## Performance

- **Duration:** 24 min
- **Started:** 2026-08-05T00:00:00Z
- **Completed:** 2026-08-05T00:24:00Z
- **Tasks:** 3
- **Files modified:** 11 (effective logical units: 9)

## Accomplishments

- Created `ReportsCategoryFilterConfigView` and `ReportsCategoryFilterConfigViewModel` mirroring `StatisticsConfig`, using the 39-05 `IConfigurationManager` methods.
- Registered the new modal enum value, ViewModel, and factory case in `ApplicationModalNames.cs` and `Extensions.cs`.
- Added three English-only resx strings and regenerated `language.Designer.cs`.
- Added a filter icon button in the Reports Summary section header bound to `OpenReportsCategoryFilterConfigCommand`.
- Wired `ReportsViewModel` to load excluded IDs on construction/initialize, compute included IDs, and pass them to `GetSavingsRateQuery`, `GetFixedVsVariableQuery`, and `BurnRatePanelViewModel`.
- Added `SetCategoryFilter` to `BurnRatePanelViewModel` so `RefreshAsync` dispatches `GetBurnRateQuery` with the selected `CategoryIds`.
- Extended `ReportsViewModelTests` with three tests verifying the command, filter computation, and propagation to the burn-rate query.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ReportsCategoryFilterConfig modal** - `725688b` (feat)
2. **Task 2: Add centralized filter button and ReportsViewModel wiring** - `914c35c` (feat)
3. **Task 3: Wire BurnRatePanelViewModel and verify with UI tests** - `ce12980` (test)

## Files Created/Modified

- `src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigView.axaml` - New modal UI mirroring StatisticsConfig
- `src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigView.axaml.cs` - Code-behind
- `src/Valt.UI/Views/Main/Modals/ReportsCategoryFilterConfig/ReportsCategoryFilterConfigViewModel.cs` - Modal logic using `GetReportsAnalyticsCategoryFilterExcludedIds` / `SetReportsAnalyticsCategoryFilterExcludedIds`
- `src/Valt.UI/Views/ApplicationModalNames.cs` - Added `ReportsCategoryFilterConfig` enum value
- `src/Valt.UI/Extensions.cs` - Registered transient ViewModel and factory case
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` - Added filter icon button to Summary section header
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - Added command, load/save helpers, and query wiring
- `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BurnRatePanelViewModel.cs` - Added `SetCategoryFilter` and `CategoryIds` propagation
- `src/Valt.UI/Lang/language.resx` - Added English keys
- `src/Valt.UI/Lang/language.Designer.cs` - Generated static properties
- `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` - Added filter command, computation, and propagation tests

## Decisions Made

- Mirrored `StatisticsConfig` exactly for consistent modal UX.
- Used the exact `IConfigurationManager` method names exposed by 39-05.
- Left `language.pt-BR.resx` and `language.es.resx` untouched per D-21; Phase 43 will localize.
- Kept the centralized analytics filter independent from the existing expense category filter to avoid confusing the two scopes.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Build failed initially because `BurnRatePanelViewModel` was missing `using System.Collections.Generic;` and `using System.Linq;` for the new `IReadOnlyList<string>` field and `.ToArray()` call. Added the missing usings and the build succeeded.
- Test `GetSelectedAnalyticsCategoryIds_Returns_Available_Minus_Excluded` failed first run because placeholder ObjectId strings were invalid; switched to valid 24-hex ObjectId strings and aligned the excluded ID with the actual category ID.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All three SPA requirements are satisfied.
- Phase 39 spending-analytics UI is complete; ready for Phase 40 (BTC-Denominated Metrics Reports & UI).
- No blockers.

## Self-Check: PASSED

- All created/modified files exist on disk.
- Task commits `725688b`, `914c35c`, and `ce12980` exist in git history.
- `dotnet build Valt.sln` succeeded with 0 errors.
- `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` passed (12/12).
- No new stub patterns or security-relevant surface beyond the planned UI filter.

## Threat Flags

None beyond the phase threat model: category names shown in the modal are local user data only (T-39-06-01 accepted), and the modal is registered in the existing factory switch expression (T-39-06-02 mitigated).

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-05*
