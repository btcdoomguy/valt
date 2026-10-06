---
phase: 261006-kks
plan: 01
subsystem: Goals
tags: [goals, recalculate, context-menu, cqrs]
status: complete
dependency_graph:
  requires:
    - GoalsPanelView.axaml Recalculate MenuItem (pre-existing, binds CanRecalculate)
    - Goal.MarkAsStale domain method
    - GoalProgressUpdateRequested notification pipeline
  provides:
    - RecalculateGoalHandler accepts Open goals (stale-marking path)
    - CanRecalculate visible on all goal entries
  affects:
    - src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs
    - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
tech_stack:
  added: []
  patterns:
    - State-based branch in command handler (re-open vs mark-stale)
key_files:
  created: []
  modified:
    - src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs
    - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
    - tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs
decisions:
  - Open goals use MarkAsStale() (existing domain method) instead of a new mechanism — reuses the GoalProgressUpdaterJob stale-goal pickup path
  - Kept OnPropertyChanged(nameof(CanRecalculate)) in UpdateGoal as a harmless no-op per plan
metrics:
  duration: 8 min
  completed: 2026-10-06
  tasks: 2
actuals:
  tokens: 3000
  tasks: 2
  commits: 3
---

# Quick Task 261006-kks: Restore Recalculate (refresh totals) on goal entries — Summary

Recalculate is now offered on every goal entry in the Transactions tab Goals panel. Open goals are marked stale so `GoalProgressUpdaterJob` recomputes their progress/totals from current transaction and price data (UI animates via the existing `GoalProgressUpdated` → `WeakReferenceMessenger` pipeline); Completed/Failed goals keep the existing re-open-and-recalculate behavior. The handler no longer rejects any goal by state.

## What Was Built

**Task 1 — Handler + tests (TDD):**
- `RecalculateGoalHandler.HandleAsync`: replaced the `INVALID_STATE` guard with a state-based branch — Completed/Failed → `goal.Recalculate()`; otherwise (Open) → `goal.MarkAsStale()`. `SaveAsync` and `GoalProgressUpdateRequested` publication now run for all states.
- Test rewritten: `HandleAsync_WithOpenGoal_ReturnsError` → `HandleAsync_WithOpenGoal_MarksGoalStaleAndSucceeds` (RED commit `2add4dc`, GREEN commit `25f0e23`). Asserts success, state stays Open, `IsUpToDate` false, notification published. Completed/Failed/validation/not-found tests untouched.

**Task 2 — UI visibility:**
- `GoalEntryViewModel.CanRecalculate` is now `=> true`, so the existing `IsVisible="{Binding CanRecalculate}"` binding on the Recalculate MenuItem shows it for Open goals too. No AXAML, localization, domain, or MCP changes needed (command shape unchanged).

## Verification

- `dotnet build Valt.sln` — 0 errors (110 pre-existing warnings, out of scope)
- `dotnet test --filter "FullyQualifiedName~RecalculateGoalHandlerTests"` — 6/6 passed
- `dotnet test --filter "FullyQualifiedName~GoalsPanelViewModelTests|FullyQualifiedName~RecalculateGoalHandlerTests"` — 21/21 passed
- `dotnet test --filter "FullyQualifiedName~Goals"` — 207/207 passed (no regressions)

## Human-Check (deferred to user)

Run the app: right-click an Open goal in the Goals panel → "Recalculate" appears; clicking it refreshes progress/totals (animated bar updates after the job runs). Right-click a Completed goal → still recalculates and returns to Open.

## TDD Gate Compliance

RED gate: `test(261006-kks)` commit `2add4dc` — new Open-goal test failed as expected against the old handler. GREEN gate: `feat(261006-kks)` commit `25f0e23` — all tests pass. Compliant.

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Modified files exist: RecalculateGoalHandler.cs, GoalEntryViewModel.cs, RecalculateGoalHandlerTests.cs — all verified on disk
- Commits exist: `2add4dc` (test/RED), `25f0e23` (feat/GREEN), `65c0188` (feat/UI) — all in `git log`
