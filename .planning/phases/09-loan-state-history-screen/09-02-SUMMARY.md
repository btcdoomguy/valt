---
phase: 09-loan-state-history-screen
plan: 02
subsystem: ui
tags: [avalonia, mvvm, modal, context-menu, localization]

requires:
  - phase: 09-01
    provides: LoanStateHistoryView/ViewModel modal pair and ApplicationModalNames.LoanStateHistory
  - phase: 08-01
    provides: UpdateLoanStateView/ViewModel modal pair

provides:
  - Assets tab context-menu entry point for Loan State History on BTC loans
  - "View History" link inside Update Loan State modal
  - Correct asset ID forwarding to LoanStateHistoryViewModel.Request

affects:
  - 09-03 (calculations / verification)

tech-stack:
  added: []
  patterns:
    - Modal factory pattern with Request/Response records
    - Avalonia context menu command binding with CommandParameter
    - CommunityToolkit.Mvvm RelayCommand

key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Tabs/Assets/AssetsView.axaml
    - src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs
    - src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateView.axaml
    - src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateViewModel.cs
    - tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs

key-decisions:
  - Injected IModalFactory into UpdateLoanStateViewModel constructor to support OpenHistoryCommand, since the ViewModel did not already own a modal factory reference.

patterns-established: []

requirements-completed: [UI-07]

duration: 9min
completed: 2026-06-16
---

# Phase 09 Plan 02: Loan State History Entry Points Summary

**Wired the two entry points that open the Loan State History modal: the Assets tab BTC-loan context menu and the "View History" link inside the Update Loan State modal, forwarding the correct asset ID in both cases.**

## Performance

- **Duration:** 9 min
- **Started:** 2026-06-16T17:33:00Z
- **Completed:** 2026-06-16T17:42:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Added a "Loan State History" context-menu item to asset cards, visible only for BTC loans.
- Added `AssetsViewModel.OpenLoanStateHistoryCommand` with secure-mode and BTC-loan validation.
- Added a right-aligned "View History" link next to the Current Loan Context header in the Update Loan State modal.
- Added `UpdateLoanStateViewModel.OpenHistoryCommand` that opens the Loan State History modal without closing the update modal.
- Updated `UpdateLoanStateViewModel` constructor and tests to inject `IModalFactory`.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Assets tab context menu and command** - `be07afc` (feat)
2. **Task 2: Add View History link to Update Loan State modal** - `e221555` (feat)

## Files Created/Modified

- `src/Valt.UI/Views/Main/Tabs/Assets/AssetsView.axaml` - Added Loan State History MenuItem to asset card context menu
- `src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` - Added OpenLoanStateHistoryCommand and LoanStateHistory namespace import
- `src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateView.axaml` - Wrapped Current Loan Context header in Grid with View History button
- `src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateViewModel.cs` - Added IModalFactory injection and OpenHistoryCommand
- `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs` - Updated CreateViewModel helper to supply mocked IModalFactory

## Decisions Made

- Injected `IModalFactory` into `UpdateLoanStateViewModel` constructor because the existing ViewModel did not hold a modal factory reference. This was necessary to open the Loan State History modal from the new command.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added missing IModalFactory dependency to UpdateLoanStateViewModel**
- **Found during:** Task 2 (Add View History link)
- **Issue:** The plan assumed `UpdateLoanStateViewModel` already had an existing `_modalFactory` field, but the actual class only held `IQueryDispatcher` and `ICommandDispatcher`. Without `IModalFactory`, `OpenHistoryCommand` could not create the Loan State History modal.
- **Fix:** Added a private `IModalFactory? _modalFactory` field, added `IModalFactory modalFactory` to the constructor, updated the using directives, and updated the corresponding test helper to pass a substituted `IModalFactory`.
- **Files modified:** `src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateViewModel.cs`, `tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs`
- **Verification:** `dotnet build Valt.sln` succeeds and `dotnet test --filter "FullyQualifiedName~Valt.Tests.UI.Screens.UpdateLoanStateViewModelTests"` passes (9/9).
- **Committed in:** `e221555` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The deviation was required to make the planned command functional. No scope creep.

## Issues Encountered

- None — the build succeeded and all acceptance criteria passed after adding the missing `IModalFactory` dependency.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 09-03 can now build on top of the wired entry points; the Loan State History modal is reachable from both specified locations.
- All existing `UpdateLoanStateViewModel` tests continue to pass after the constructor change.

## Self-Check: PASSED

- [x] Modified files exist on disk
- [x] Task commits found in git log (`be07afc`, `e221555`)
- [x] `dotnet build Valt.sln` succeeds
- [x] `dotnet test --filter "FullyQualifiedName~Valt.Tests.UI.Screens.UpdateLoanStateViewModelTests"` passes (9/9)
- [x] Acceptance criteria verified via grep/file checks

---
*Phase: 09-loan-state-history-screen*
*Completed: 2026-06-16*
