---
phase: 12-remove-blocking-calls
plan: 01
subsystem: ui
tags: [async, fire-and-forget, blocking-calls, avalonia, viewmodel, nunit, nsubstitute]

requires:
  - phase: 11-safe-fire-and-forget
    provides: IFireAndForgetTaskRunner and FireAndForgetSafeAsync extension

provides:
  - Architecture regex guard blocking `.GetAwaiter().GetResult()` reintroduction in `src/Valt.UI`
  - `ReportsViewModel.Initialize()` no longer blocks the UI thread on indicator refresh
  - Dead `LoadCachedIndicatorsData()` method removed from `ReportsViewModel`

affects:
  - future UI async hardening
  - Reports tab initialization and refresh behavior

tech-stack:
  added: []
  patterns:
    - Regex-based architecture test for anti-patterns
    - NSubstitute substitution of concrete panel ViewModels
    - Fire-and-forget runner for UI-thread async work

key-files:
  created:
    - tests/Valt.Tests/Architecture/BlockingCallTests.cs
    - tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs
  modified:
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - .planning/phases/12-remove-blocking-calls/12-VALIDATION.md
    - .gitignore
    - .planning/REQUIREMENTS.md

key-decisions:
  - Kept `ReportsViewModel.Initialize()` as `public void Initialize()` per D-01.
  - Used a completed `TaskCompletionSource` task in `ReportsViewModelTests` so the test is red-but-not-hanging while the production code still blocks; after the fix the same test passes.
  - Left `UpdateIndicatorsData()` private helper in place even though it is currently unused; it is not a blocking call and was outside the plan's deletion scope.

requirements-completed:
  - ASYNC-02

duration: 23min
completed: 2026-06-19
status: complete
---

# Phase 12 Plan 01: Remove Blocking Calls Summary

**Converted the `ReportsViewModel` indicator refresh to the existing fire-and-forget runner, deleted the dead `LoadCachedIndicatorsData()` helper, and added a regex-based architecture guard that prevents `.GetAwaiter().GetResult()` from being reintroduced to the UI layer.**

## Performance

- **Duration:** 23 min
- **Started:** 2026-06-19T20:54:27Z
- **Completed:** 2026-06-19T21:17:38Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments

- Added `BlockingCallTests` regex guard covering every `.cs` file under `src/Valt.UI`.
- Added `ReportsViewModelTests` verifying `Initialize()` schedules `_indicatorsPanel.RefreshAsync()` through `IFireAndForgetTaskRunner`.
- Removed the UI-thread blocking call in `ReportsViewModel.Initialize()` and the unused `LoadCachedIndicatorsData()` method.
- Signed off `12-VALIDATION.md` with `nyquist_compliant: true` and `wave_0_complete: true`.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ASYNC-02 guard tests** - `0d12b9c` (test)
2. **Task 2: Remove blocking calls from ReportsViewModel** - `349ecc2` (fix)
3. **Task 3: Run full phase verification** - `2578846` (docs)

## Files Created/Modified

- `tests/Valt.Tests/Architecture/BlockingCallTests.cs` - Regex architecture guard for `.GetAwaiter().GetResult()` in `src/Valt.UI`.
- `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` - Verifies `Initialize()` routes indicators refresh through `IFireAndForgetTaskRunner`.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` - `Initialize()` now uses `FireAndForgetSafeAsync`; removed `LoadCachedIndicatorsData()`.
- `.planning/phases/12-remove-blocking-calls/12-VALIDATION.md` - Signed off with final verification results.
- `.planning/REQUIREMENTS.md` - Marked ASYNC-02 complete.
- `.gitignore` - Added `TestResults/`.

## Decisions Made

- Followed D-01 and kept `Initialize()` synchronous (`void`) while moving the async work to the injected runner.
- Used a completed `TaskCompletionSource` task in `ReportsViewModelTests` so the test fails cleanly before the production fix and passes afterward, avoiding an indefinite hang from the pre-fix blocking call.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- `ReportsViewModelTests` initially hung when using a non-completed `TaskCompletionSource` task because the pre-fix `Initialize()` called `.GetAwaiter().GetResult()`. Switching to a completed TCS task made the test red-but-runnable before the fix and green after it.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- ASYNC-02 is complete; the UI layer no longer contains `.GetAwaiter().GetResult()` blocking calls.
- Ready for Phase 13 (HTTP client factory migration) or the next planned phase.

## Self-Check: PASSED

- [x] `12-01-SUMMARY.md` exists on disk.
- [x] Task commits `0d12b9c`, `349ecc2`, `2578846` exist in git history.
- [x] Metadata commit for SUMMARY.md exists in git history.
- [x] `dotnet test` passed 1509 tests with 0 failures before final commit.
- [x] Blocking-pattern grep returned 0 matches across `src/Valt.UI/**/*.cs`.

---
*Phase: 12-remove-blocking-calls*
*Completed: 2026-06-19*
