---
phase: 261006-kks
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs
  - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
  - tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs
autonomous: true
requirements: []

estimate:
  tokens: 25000
  raw_tokens: 20000
  tasks: 2
  confidence: low

must_haves:
  truths:
    - Right-clicking ANY goal entry in the Goals panel (Transactions tab) shows the "Recalculate" context menu item, including Open goals
    - Recalculating a Completed/Failed goal keeps its existing behavior (state resets to Open and progress is recomputed by the background job)
    - Recalculating an Open goal marks it stale so GoalProgressUpdaterJob recomputes its progress/totals from current transaction and price data, and the UI updates after the job publishes GoalProgressUpdated
    - All existing RecalculateGoal handler and GoalsPanelViewModel tests pass, with the Open-goal test rewritten to assert the new success path
  artifacts:
    - src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs (state guard relaxed, Open-goal path added)
    - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs (CanRecalculate always true)
    - tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs (Open-goal test updated)
  key_links:
    - GoalsPanelView.axaml Recalculate MenuItem IsVisible binds to GoalEntryViewModel.CanRecalculate — no AXAML change needed, the property change alone makes the item visible for Open goals
    - Handler publishes GoalProgressUpdateRequested → GoalProgressUpdateRequestedHandler triggers GoalProgressUpdater manually → job recalculates stale goals → GoalProgressUpdatedUIHandler → WeakReferenceMessenger → GoalsPanelViewModel.UpdateGoalProgress animates the bar
---

<objective>
Restore the "Recalculate" (refresh totals) feature on goal entries in the Goals panel of the Transactions tab so it is available for the selected goal regardless of state.

Purpose: The context menu item already exists (GoalsPanelView.axaml binds a Recalculate MenuItem to GoalsPanelViewModel.RecalculateGoalCommand → RecalculateGoalCommand → RecalculateGoalHandler), but it is hidden by `IsVisible="{Binding CanRecalculate}"` and `CanRecalculate` is true only for Completed/Failed goals. The handler additionally rejects Open goals with an INVALID_STATE error. So for ordinary Open goals there is no way to force a totals refresh, which is what the user is missing. No XAML, localization (Goals_Recalculate exists in en/pt-BR/es + Designer), domain, or MCP changes are needed — the RecalculateGoalCommand shape is unchanged, so no MCP tool update is required per the AGENTS.md MCP checklist.
Output: Recalculate works for Open goals (marks stale, background job recomputes, UI animates) while Completed/Failed goals keep their existing re-open behavior.
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
@/home/vmabellini/.config/opencode/gsd-core/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@.planning/STATE.md
@src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs
@src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
@src/Valt.Core/Modules/Goals/Goal.cs
@src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml
@tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Allow RecalculateGoal for Open goals (mark stale) and update handler tests</name>
  <files>src/Valt.App/Modules/Goals/Commands/RecalculateGoal/RecalculateGoalHandler.cs, tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs</files>
  <behavior>
    - Open goal → success: goal state stays Open, IsUpToDate becomes false, GoalProgressUpdateRequested is published
    - Completed goal → unchanged: state resets to Open, IsUpToDate false, notification published
    - Failed goal → unchanged: state resets to Open, IsUpToDate false, notification published
    - Empty GoalId → unchanged: VALIDATION_FAILED; unknown id → unchanged: GOAL_NOT_FOUND
  </behavior>
  <action>
    In RecalculateGoalHandler.HandleAsync, replace the INVALID_STATE guard block (currently lines 41-44, comment "Only allow recalculation of Completed or Failed goals") with a state-based branch: if goal.State is Completed or Failed, call goal.Recalculate() (existing re-open behavior); otherwise (Open) call goal.MarkAsStale() — the domain method at Goal.cs line 63 that sets IsUpToDate = false and raises GoalUpdatedEvent, already used by MarkGoalsStaleOnPriceUpdateHandler. Keep the SaveAsync and the PublishAsync(new GoalProgressUpdateRequested()) calls after the branch for ALL states. MarkAsStale() is a no-op when the goal is already stale, which is fine — the published notification still triggers the job manually via GoalProgressUpdateRequestedHandler, and GetStaleGoalsAsync picks the goal up.
    In tests/Valt.Tests/Application/Goals/RecalculateGoalHandlerTests.cs, rewrite HandleAsync_WithOpenGoal_ReturnsError into HandleAsync_WithOpenGoal_MarksGoalStaleAndSucceeds: build an up-to-date Open goal (follow the existing builder usage in that file, e.g. GoalBuilder.AGoal() with state Open and IsUpToDate true — check the builder's available With* methods first), dispatch, then assert result.IsSuccess, loaded goal State is still Open, IsUpToDate is false, and the notification publisher received GoalProgressUpdateRequested (mirror the assertion style of the existing PublishesGoalProgressUpdateRequested test). Do not alter the Completed/Failed/validation/not-found tests. Run this task's tests before touching the ViewModel (RED for the new Open-goal test, GREEN after the handler change).
  </action>
  <verify>
    <automated>dotnet build Valt.sln && dotnet test --filter "FullyQualifiedName~RecalculateGoalHandlerTests"</automated>
  </verify>
  <done>Handler accepts Open goals (marks stale, publishes notification) and rejects nothing by state; RecalculateGoalHandlerTests all green including the rewritten Open-goal success test.</done>
</task>

<task type="auto" tdd="false">
  <name>Task 2: Make CanRecalculate always visible and verify full build + UI test suite</name>
  <files>src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs</files>
  <action>
    In GoalEntryViewModel (line 195), change `public bool CanRecalculate => _goal.State == (int)GoalStates.Completed || _goal.State == (int)GoalStates.Failed;` to `public bool CanRecalculate => true;`. Keep the OnPropertyChanged(nameof(CanRecalculate)) call in UpdateGoal — it becomes a no-op but harmless; do not remove it. No AXAML change: GoalsPanelView.axaml already binds the Recalculate MenuItem's IsVisible to CanRecalculate, so the item now appears on every goal entry including Open ones. Do not add secure-mode IsEnabled gating — the item previously had none and recalculation does not display hidden data (matches existing behavior for Completed/Failed goals). No localization changes: Goals_Recalculate already exists in language.resx, language.pt-BR.resx, language.es.resx, and language.Designer.cs.
  </action>
  <verify>
    <automated>dotnet build Valt.sln && dotnet test --filter "FullyQualifiedName~GoalsPanelViewModelTests|FullyQualifiedName~RecalculateGoalHandlerTests"</automated>
    <human-check>Run the app, open the Transactions tab Goals panel, right-click an Open goal — "Recalculate" appears in the context menu; click it and the goal's progress/totals refresh (animated bar updates after a short delay); right-click a Completed goal — it still recalculates and returns to Open.</human-check>
  </verify>
  <done>CanRecalculate is state-independent; Recalculate menu item visible on all goal entries; GoalsPanelViewModelTests and RecalculateGoalHandlerTests pass; full solution builds.</done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| UI → App command dispatcher | RecalculateGoalCommand carries only a GoalId supplied by the UI |

## STRIDE Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation Plan |
|-----------|----------|-----------|----------|-------------|-----------------|
| T-261006-kks-01 | Tampering | RecalculateGoalHandler | low | accept | GoalId is resolved server-side via IGoalRepository; invalid ids return GOAL_NOT_FOUND; no new input surface introduced |
| T-261006-kks-02 | Denial of Service | GoalProgressUpdaterJob manual trigger | low | accept | Existing trigger path already used by multiple handlers; one extra stale goal adds negligible work |
</threat_model>

<verification>
- dotnet build Valt.sln succeeds
- dotnet test --filter "FullyQualifiedName~RecalculateGoalHandlerTests|FullyQualifiedName~GoalsPanelViewModelTests" all green
- Manual: right-click on an Open goal shows Recalculate; invoking it refreshes progress via the background job
</verification>

<success_criteria>
Right-clicking any goal in the Transactions tab Goals panel offers "Recalculate"; Open goals get stale-marked and recomputed, Completed/Failed goals keep the re-open behavior; no regressions in goal-related tests.
</success_criteria>

<output>
Create `.planning/quick/261006-kks-i-cannot-see-the-feature-to-refresh-the-/261006-kks-SUMMARY.md` when done
</output>
