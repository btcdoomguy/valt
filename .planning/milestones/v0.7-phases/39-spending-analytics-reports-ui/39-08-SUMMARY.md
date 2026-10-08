---
phase: 39-spending-analytics-reports-ui
plan: 08
subsystem: infra
tags: [configuration, migration, litedb, nunit, reports]

requires:
  - phase: 39-07
    provides: Centralized reports category filter button and Statistics dashboard wiring

provides:
  - Lazy migration from legacy StatisticsExcludedCategories to ReportsAnalyticsCategoryFilterExcluded
  - Idempotent read-time migration that preserves existing user settings
  - Tests proving migration copies legacy values once and respects new-key values

affects:
  - Reports tab category filter behavior
  - Statistics dashboard excluded-category handling

tech-stack:
  added: []
  patterns:
    - Read-time lazy migration for configuration key renames

key-files:
  created:
    - tests/Valt.Tests/Reports/ConfigurationManagerTests.cs
  modified:
    - src/Valt.Infra/Modules/Configuration/ConfigurationManager.cs

key-decisions:
  - "Preserved the legacy StatisticsExcludedCategories key instead of clearing it, so downgrades and external inspection still work."
  - "Made migration idempotent by triggering only when the new ReportsAnalyticsCategoryFilterExcluded key is empty."

patterns-established:
  - "Read-time migration pattern: migrate on getter when target is empty, persist to target, and never mutate source."

requirements-completed: [SPA-01, SPA-02, SPA-03]

# Metrics
duration: 6min
completed: 2026-08-05
status: complete
---

# Phase 39 Plan 08: Statistics-to-Reports Category Filter Migration Summary

**Existing Statistics excluded-category settings are now transparently migrated to the centralized ReportsAnalyticsCategoryFilterExcluded key so users do not lose their configuration when the Statistics dashboard uses the new filter.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-08-05T18:33:48Z
- **Completed:** 2026-08-05T18:39:48Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Implemented read-time migration in `ConfigurationManager.GetReportsAnalyticsCategoryFilterExcludedIds()` that copies legacy `StatisticsExcludedCategories` values to the new `ReportsAnalyticsCategoryFilterExcluded` key when the new key is empty.
- Ensured the migration is idempotent and preserves trim/distinct/skip-empty behavior.
- Left the legacy key intact so downgrades or external inspection still work.
- Added tests in `tests/Valt.Tests/Reports/ConfigurationManagerTests.cs` verifying the migration copies legacy values once and that the new key value takes precedence over the legacy value.

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement migration from StatisticsExcludedCategories to ReportsAnalyticsCategoryFilterExcluded** — `a80b3d8` (feat)
2. **Task 2: Add migration test** — `b48b069` (test)

## Files Created/Modified

- `src/Valt.Infra/Modules/Configuration/ConfigurationManager.cs` — Added lazy migration logic inside `GetReportsAnalyticsCategoryFilterExcludedIds()`.
- `tests/Valt.Tests/Reports/ConfigurationManagerTests.cs` — Created migration behavior tests covering both legacy-to-new copy and new-key precedence.

## Decisions Made

- **Preserved the legacy `StatisticsExcludedCategories` key** instead of deleting it after migration, so downgrades and external inspection still work.
- **Made the migration idempotent** by only triggering when the new key is empty; once the new key has values, the legacy key is ignored.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 39 UAT test 18 (existing Statistics excluded-category settings migrate to the centralized filter) can now be marked as pass.
- Phase 39 is fully complete and ready for Phase 40 work.

## Self-Check: PASSED

- [x] `src/Valt.Infra/Modules/Configuration/ConfigurationManager.cs` exists and was modified.
- [x] `tests/Valt.Tests/Reports/ConfigurationManagerTests.cs` exists and was created.
- [x] `dotnet build Valt.sln` succeeded with 0 errors.
- [x] `dotnet test --filter "FullyQualifiedName~ConfigurationManagerTests"` passed (8 tests).
- [x] Commit `a80b3d8` exists in git log.
- [x] Commit `b48b069` exists in git log.

---
*Phase: 39-spending-analytics-reports-ui*
*Completed: 2026-08-05*
