---
phase: 39-spending-analytics-reports-ui
plan: 07
subsystem: ui
tags: [avalonia, mvvm, localization, nunit, nsubstitute, reports, category-filter]

requires:
  - phase: 39-05
    provides: Reports analytics category filter persistence and query wiring
  - phase: 39-06
    provides: Centralized reports category filter modal and ViewModel wiring

provides:
  - Top-right, right-aligned centralized category filter button above all Reports sections
  - Statistics dashboard using the same centralized excluded-category set as Burn rate, Savings rate, and Fixed vs variable
  - Removal of the Statistics dashboard's own configuration gear button
  - Removal of StatisticsConfig modal activation points from the Reports tab
  - Updated UI tests covering the new wiring

affects:
  - Reports tab UI layout
  - Statistics dashboard behavior
  - ApplicationModalNames enum and modal factory registrations

tech-stack:
  added: []
  patterns:
    - Centralized filter state shared across multiple dashboard panels
    - Named record constructor arguments to omit optional command parameters

key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Extensions.cs
    - src/Valt.UI/Views/ApplicationModalNames.cs
    - src/Valt.UI/Lang/language.resx
    - src/Valt.UI/Lang/language.pt-BR.resx
    - src/Valt.UI/Lang/language.es.resx
    - src/Valt.UI/Lang/language.Designer.cs
    - tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs

key-decisions:
  - "Named the new tooltip key Reports_CategoryFilter_Tooltip and localized it in en/pt-BR/es."
  - "Updated the centralized filter description to include statistics analytics."
  - "Retained Statistics_Config_* language keys because the StatisticsConfig modal files are still compiled by the project even though the modal is no longer reachable."
  - "Commented out the StatisticsConfig enum value instead of deleting it to preserve numeric values for remaining modal names."

patterns-established:
  - "Centralized category filter is now consumed by Burn rate, Savings rate, Fixed vs variable, and Statistics."
  - "DashboardData can be constructed without a ConfigureCommand to suppress the gear icon."

requirements-completed: [SPA-01, SPA-02, SPA-03]

# Metrics
duration: 16min 31s
completed: 2026-08-05
status: complete
---

# Phase 39 Plan 07: Centralized Reports Category Filter UAT Adjustments Summary

**Centralized reports category filter moved to an independent top-right toolbar with the settings icon, Statistics dashboard wired to the same filter, and the legacy StatisticsConfig modal disconnected from the Reports tab.**

## Performance

- **Duration:** 16 min 31 s
- **Started:** 2026-08-05T18:10:56Z
- **Completed:** 2026-08-05T18:25:57Z
- **Tasks:** 4 (plus 1 corrective commit to restore pre-execution state files)
- **Files modified:** 9

## Accomplishments

- Moved the centralized filter button out of the Summary Expander header into a right-aligned top toolbar using the `&#xE8B8;` Material Design settings icon.
- Added a tooltip localized in English, Portuguese, and Spanish for the new filter button.
- Removed the Statistics dashboard's own configuration command and gear icon.
- Wired `FetchStatisticsDataAsync` to the same `_analyticsExcludedCategoryIds` set used by the other analytics panels.
- Refreshed the Statistics dashboard from the centralized filter success path.
- Removed the `StatisticsConfig` modal registration and factory case from `Extensions.cs` and preserved enum numbering in `ApplicationModalNames.cs`.
- Updated the centralized filter description to mention statistics and added missing pt-BR/es translations for the config strings.
- Added/updated UI tests asserting the Statistics dashboard has no configuration command and that the centralized excluded category IDs are passed to `IStatisticsReport.GetAsync`.

## Task Commits

Each task was committed atomically:

1. **Task 1: Move filter button to top-right toolbar with settings icon** — `7d67261` (feat)
2. **Task 2: Wire Statistics dashboard to centralized category filter** — `c7b5702` (feat)
3. **Task 3: Remove StatisticsConfig modal activation from Reports tab** — `327a328` (feat)
4. **Task 4: Update ReportsViewModel tests for centralized filter** — `c8ed061` (test)

**Corrective cleanup:** `08e4a39` (chore) — restored `.planning/ROADMAP.md` and `.planning/STATE.md` to their pre-execution baseline because they were already staged from prior plans and were not intended to be modified by this plan.

## Files Created/Modified

- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — Added top-right toolbar with centralized filter button; removed filter button from Summary header.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — Removed `OpenStatisticsConfig` command; `FetchStatisticsDataAsync` now uses `_analyticsExcludedCategoryIds`; Statistics refreshed from centralized filter success path.
- `src/Valt.UI/Extensions.cs` — Removed `StatisticsConfigViewModel` registration and factory case.
- `src/Valt.UI/Views/ApplicationModalNames.cs` — Commented out `StatisticsConfig` enum value to preserve numbering.
- `src/Valt.UI/Lang/language.resx` — Added tooltip, updated centralized filter description.
- `src/Valt.UI/Lang/language.pt-BR.resx` — Added tooltip and centralized filter config translations.
- `src/Valt.UI/Lang/language.es.resx` — Added tooltip and centralized filter config translations.
- `src/Valt.UI/Lang/language.Designer.cs` — Added tooltip property.
- `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` — Added tests for Statistics command absence and centralized filter wiring.

## Decisions Made

- **Named the new tooltip key `Reports_CategoryFilter_Tooltip`** to keep it distinct from the existing modal config strings and updated all three language files plus the designer.
- **Kept the `Statistics_Config_*` language keys** because the `StatisticsConfig` view files are still part of the project and would fail to compile if the keys were removed.
- **Commented out the `StatisticsConfig` enum value** rather than deleting it, so the numeric values of the remaining modal names stay unchanged.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking] Removed language keys broke the build**
- **Found during:** Task 3 (remove StatisticsConfig modal usage)
- **Issue:** Removing `Statistics_Config_Title`, `Statistics_Config_Description`, and `Statistics_Config_ExcludedCategories` from the resx files caused Avalonia compilation errors because `StatisticsConfigView.axaml` still references them.
- **Fix:** Reverted the key removals and kept the language keys in place. The modal is no longer reachable at runtime, but the files still compile.
- **Files modified:** `src/Valt.UI/Lang/language.resx`, `src/Valt.UI/Lang/language.pt-BR.resx`, `src/Valt.UI/Lang/language.es.resx`, `src/Valt.UI/Lang/language.Designer.cs`
- **Verification:** `dotnet build Valt.sln` succeeded with zero errors.
- **Committed in:** `327a328` (Task 3)

**2. [Rule 1 — Bug] Pre-staged ROADMAP/STATE were included in the first task commit**
- **Found during:** Task 1 commit review
- **Issue:** `.planning/ROADMAP.md` and `.planning/STATE.md` were already staged in the git index when execution started and were accidentally committed with the first task.
- **Fix:** Restored both files to the pre-execution baseline (`ce12980`) in a dedicated cleanup commit.
- **Files modified:** `.planning/ROADMAP.md`, `.planning/STATE.md`
- **Verification:** `git status --short` shows no pending changes; the files match the baseline.
- **Committed in:** `08e4a39`

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 bug)
**Impact on plan:** Both corrections were necessary to keep the build green and respect the instruction not to modify planning files. No additional feature scope was added.

## Issues Encountered

- The initial `StatisticsConfig` language-key removal caused a build failure; the keys were retained as described above.
- Two unrelated integration tests failed during the full test run (`CoinGeckoProviderTests` and `BitcoinDominanceProviderTests`) due to HTTP 403 responses from external APIs. These failures are pre-existing and not caused by this plan's changes.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 39 Reports UI now satisfies the UAT feedback: centralized filter is in the top-right, Statistics uses the centralized filter, and the legacy per-dashboard config button is gone.
- The codebase builds successfully and the targeted UI tests pass.
- Recommended follow-up: confirm whether the now-unreachable `StatisticsConfig` modal files should be removed in a separate cleanup task.

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-05*
