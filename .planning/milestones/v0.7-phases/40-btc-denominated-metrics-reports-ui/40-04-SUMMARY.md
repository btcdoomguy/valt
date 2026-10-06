---
phase: 40-btc-denominated-metrics-reports-ui
plan: 04
subsystem: ui
tags: [avalonia, mvvm, reports, btc-metrics, gap-closure]

requires:
  - phase: 40-btc-denominated-metrics-reports-ui
    provides: "BTC-denominated metrics backend query and UI panels from plans 40-01, 40-02, and 40-03"

provides:
  - "Reports tab account filter wired into BTC metrics fetches"
  - "BTC metrics panels re-fetch when account selection changes"
  - "Category-breakdown empty state matches monthly view empty state"

affects:
  - "Phase 40 verification (D-18, D-20 gaps closed)"
  - "Phase 43 localization/documentation (no new strings introduced)"

actuals:
  tokens: 817
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - "CQRS query dispatch with AccountIds filter array"
    - "Debounced re-fetch on collection change"
    - "Cached DTO used for view-aware empty-state recompute"

key-files:
  created: []
  modified:
    - "src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs"

key-decisions:
  - "Kept IsBtcMetricsEmpty as a single bool bound to the existing XAML empty-state Border instead of introducing a second property"
  - "Used the cached _lastBtcMetricsData for the toggle handler so switching Monthly/By category does not issue a new query"

requirements-completed:
  - BTC-01
  - BTC-02
  - BTC-03

coverage:
  - id: D1
    description: "Wire the Reports tab account filter into BTC-denominated metrics fetches and refresh panels when account selection changes"
    requirement: BTC-01
    verification:
      - kind: other
        ref: "grep -n \"AccountIds = SelectedAccounts.Select(x => x.Id.ToString()).ToArray()\" src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs | wc -l == 2"
        status: pass
      - kind: other
        ref: "grep -n \"FetchBtcDenominatedMetricsAsync(provider)\" src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs inside DebouncedFetchCategoriesAsync"
        status: pass
      - kind: unit
        ref: "dotnet test --filter FullyQualifiedName~BtcDenominatedMetrics"
        status: pass
    human_judgment: false
  - id: D2
    description: "Recompute IsBtcMetricsEmpty for the category breakdown so zero categories show the same empty-state copy as the monthly view"
    requirement: BTC-02
    verification:
      - kind: other
        ref: "grep -n \"IsBtcMetricsEmpty = IsBtcMetricsCategoryView ? data.SpentByCategory.Count == 0 : data.Months.Count == 0\" src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs"
        status: pass
      - kind: other
        ref: "grep -n \"IsBtcMetricsCategoryView ? _lastBtcMetricsData.SpentByCategory.Count == 0 : _lastBtcMetricsData.Months.Count == 0\" src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs"
        status: pass
      - kind: unit
        ref: "dotnet test --filter FullyQualifiedName~BtcDenominatedMetrics"
        status: pass
    human_judgment: false

duration: 10min
completed: 2026-08-06
status: complete
---

# Phase 40 Plan 04: Wire Account Filter and Fix Category Empty State Summary

**Closed the two remaining verification gaps in Phase 40 by wiring the Reports tab account filter into the BTC-denominated metrics fetches and making the category-breakdown empty state match the monthly view.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-08-06T19:01:44Z
- **Completed:** 2026-08-06T19:11:44Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- `FetchBtcDenominatedMetricsAsync` and `FetchStackVelocityAsync` now pass `SelectedAccounts` into `GetBtcDenominatedMetricsQuery.AccountIds`.
- Changing `SelectedAccounts` triggers a debounced re-fetch that includes the BTC metrics panels.
- `IsBtcMetricsEmpty` is now computed from `SpentByCategory.Count` when the category view is active and from `Months.Count` when the monthly view is active.
- Toggling Monthly / By category recomputes the empty state from cached data without a database round-trip.

## Task Commits

Each task was committed atomically:

1. **Task 1: Wire account filter to BTC metrics and re-fetch on account changes** - `baf45a0` (feat)
2. **Task 2: Recompute IsBtcMetricsEmpty for the category breakdown** - `07288c3` (fix)

## Files Created/Modified

- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - AccountIds wiring, debounced re-fetch expansion, view-aware empty-state recompute

## Decisions Made

- Followed the plan's approach of reusing the existing `IsBtcMetricsEmpty` property for both monthly and category views rather than adding a separate property.
- Used the cached `_lastBtcMetricsData` in the toggle handler so the empty state updates instantly without re-querying the database.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] First Task 1 commit accidentally included pre-staged planning artifacts**
- **Found during:** Task 1 commit
- **Issue:** `.planning/STATE.md` and `.planning/ROADMAP.md` were already staged with orchestrator position updates; the initial `git commit` included those files alongside `ReportsViewModel.cs`.
- **Fix:** Reset the commit with `git reset --soft HEAD~1`, unstaged the planning files with `git reset HEAD`, and re-committed only `ReportsViewModel.cs`.
- **Files modified:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (only the intended file in the final Task 1 commit)
- **Verification:** `git show --stat baf45a0` confirms only `ReportsViewModel.cs` changed in the final Task 1 commit
- **Committed in:** `baf45a0` (Task 1 final commit)

**2. [Rule 1 - Bug] Ternary in toggle handler spanned multiple lines, failing the plan's single-line grep verification**
- **Found during:** Task 2 verification
- **Issue:** `grep -n "IsBtcMetricsCategoryView ? _lastBtcMetricsData.SpentByCategory.Count == 0 : _lastBtcMetricsData.Months.Count == 0"` returned no output because the expression was formatted across three lines.
- **Fix:** Collapsed the ternary to a single line so the automated grep check passes while preserving identical runtime behavior.
- **Files modified:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`
- **Verification:** Both ternary grep checks now return matching lines; build and BTC metrics tests pass
- **Committed in:** `07288c3` (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 formatting/process issues)
**Impact on plan:** No functional changes beyond the planned wiring and empty-state logic. Both auto-fixes were procedural/formatting adjustments.

## Issues Encountered

- Pre-staged `.planning/STATE.md` and `.planning/ROADMAP.md` changes were almost committed into Task 1; resolved by resetting and unstaging before the final Task 1 commit.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 40 is now complete (4/4 plans finished).
- Phase 41: Wealth & Performance Reports & UI is ready to plan.
- The two remaining human-verification backstops from `40-VERIFICATION.md` (stack velocity X-axis label density at >12 months and empty-state text wrapping/readability) still require manual UI review but are not blockers for this plan.

## Self-Check: PASSED

- [x] `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` exists and contains the required changes
- [x] Commit `baf45a0` exists for Task 1
- [x] Commit `07288c3` exists for Task 2
- [x] `dotnet build Valt.sln` succeeded with 0 errors
- [x] `dotnet test --filter "FullyQualifiedName~BtcDenominatedMetrics"` passed 12/12 tests
- [x] Grep checks confirm exactly two `AccountIds = SelectedAccounts.Select(...)` assignments
- [x] Grep checks confirm both view-aware ternary expressions for `IsBtcMetricsEmpty`

---
*Phase: 40-btc-denominated-metrics-reports-ui*
*Completed: 2026-08-06*
