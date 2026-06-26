---
phase: 15-throttle-account-totals-job
plan: 01
subsystem: infra
tags:
  - background-jobs
  - polling
  - NSubstitute
  - NetArchTest
  - IClock

requires:
  - phase: 14-event-driven-goal-updates
    provides: Pattern for raising background-job fallback intervals to 120s and using NSubstitute job tests.

provides:
  - AccountTotalsJob.Interval raised to 120 seconds.
  - Unit tests for AccountTotalsJob.RunAsync day-rollover behavior.
  - Architecture guard preventing AccountTotalsJob.Interval regression below 60 seconds.

affects:
  - Background job scheduling frequency
  - Account cache refresh safety net

tech-stack:
  added: []
  patterns:
    - Fake IClock with NSubstitute.Returns(...) for deterministic date jumps in job tests.
    - NetArchTest/Reflection-style assertion on IBackgroundJob.Interval value.

key-files:
  created:
    - tests/Valt.Tests/Infra/Budget/Accounts/AccountTotalsJobTests.cs
  modified:
    - src/Valt.Infra/Modules/Budget/Accounts/Services/AccountTotalsJob.cs
    - tests/Valt.Tests/Architecture/BackgroundJobsTests.cs

key-decisions:
  - "None - followed plan as specified."

requirements-completed:
  - JOB-01
  - JOB-03

# Metrics
duration: 3 min
completed: 2026-06-22
status: complete
---

# Phase 15 Plan 01: Throttle AccountTotalsJob Summary

**Raised `AccountTotalsJob` fallback polling interval to 120 seconds and locked the behavior with fake-clock unit tests and an architecture guard against interval regression.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-06-22T18:11:41Z
- **Completed:** 2026-06-22T18:14:55Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- `AccountTotalsJob.Interval` now returns `TimeSpan.FromSeconds(120)` instead of 5 seconds.
- New unit tests verify day-change refresh, no-change skip, database-closed skip, and the interval value.
- Extended architecture tests assert `AccountTotalsJob.Interval >= 60s` and `== 120s`.

## Task Commits

Each task was committed atomically:

1. **Task 1: Raise AccountTotalsJob fallback interval to 120 seconds** - `7c3501e` (feat)
2. **Task 2: Add unit tests for AccountTotalsJob day-rollover behavior** - `93c470e` (test)
3. **Task 3: Add architecture test to prevent AccountTotalsJob interval regression** - `379affa` (test)

## Files Created/Modified

- `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountTotalsJob.cs` - Updated `Interval` to 120 seconds.
- `tests/Valt.Tests/Infra/Budget/Accounts/AccountTotalsJobTests.cs` - New test fixture for `AccountTotalsJob.RunAsync` behavior and interval value.
- `tests/Valt.Tests/Architecture/BackgroundJobsTests.cs` - Added `AccountTotalsJob_Interval_Should_Be_At_Least_60_Seconds` architecture guard.

## Decisions Made

None - followed plan as specified.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- `dotnet test` across the full solution reported 2 failures, both unrelated to this plan:
  - `Valt.Tests.Infrastructure.Indicators.BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` — `403 Forbidden` from live API.
  - `Valt.Tests.LivePriceCrawlers.CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date` — `403 Forbidden` from live API.
  These are pre-existing live-API/network failures tracked under the broader `TEST-01` requirement to isolate live-API tests behind a category. They are outside the scope of this plan.

## Next Phase Readiness

- Phase 15 is complete. The AccountTotalsJob fallback interval is throttled and guarded by tests.
- The event-driven cache update path (`UpdateAccountTotalEventHandler`) remains unchanged and is still the primary refresh mechanism.

## Self-Check: PASSED

- [x] `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountTotalsJob.cs` modified and committed.
- [x] `tests/Valt.Tests/Infra/Budget/Accounts/AccountTotalsJobTests.cs` created and committed.
- [x] `tests/Valt.Tests/Architecture/BackgroundJobsTests.cs` modified and committed.
- [x] Commits `7c3501e`, `93c470e`, and `379affa` exist in git history.
- [x] Relevant filtered tests pass (`AccountTotalsJobTests`: 4 passed; `BackgroundJobsTests`: 2 passed).

---
*Phase: 15-throttle-account-totals-job*
*Completed: 2026-06-22*
