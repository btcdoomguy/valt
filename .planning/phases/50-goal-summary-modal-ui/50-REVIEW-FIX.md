---
phase: 50-goal-summary-modal-ui
fixed_at: 2026-10-06T00:00:00Z
review_path: .planning/phases/50-goal-summary-modal-ui/50-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 4
skipped: 2
status: partial
---

# Phase 50: Code Review Fix Report

**Fixed at:** 2026-10-06
**Source review:** .planning/phases/50-goal-summary-modal-ui/50-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (WR-01, WR-02, IN-02, IN-03, IN-04, IN-05)
- Fixed: 4 (WR-01, WR-02, IN-02, IN-03)
- Skipped: 2 (IN-04 documented no-change, IN-05 intentional deferral)

**Verification:** `dotnet build Valt.sln` 0 errors; `dotnet test` full suite
**1818 passed, 0 failed** (main checkout — `workflow.use_worktrees=false`).

## Fixed Issues

### WR-01: ViewSummaryCommand opens the modal for a NotSupported query result

**Files modified:** `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs`, `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs`, `tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs`
**Commit:** d51d0f8
**Applied fix:** Added a `result.Value is not GoalContributingTransactionsResult.Supported → return` guard in `ViewSummaryCommand` (silent no-op; the menu item is hidden for those types — defense-in-depth). Converted `CanViewSummary` from the `is not NetWorthBtcGoalTypeOutputDTO` deny-list to an explicit allow-list of the 9 supported goal-type DTOs. Added `ViewSummary_NotSupportedResult_DoesNotOpenModal` test.

### WR-02: GoalEntryViewModel depends on the modal ViewModel's nested enum

**Files modified:** `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalStrategyUnit.cs` (new), `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs`, `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs`, `tests/Valt.Tests/UI/Screens/GoalSummaryViewModelTests.cs`, `tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs`
**Commit:** ca1621e
**Applied fix:** Moved the enum out of `GoalSummaryViewModel` to the neutral Models layer as `GoalStrategyUnit` in its own file (`Valt.UI.Views.Main.Tabs.Transactions.Models`), renamed per the reviewer's suggestion to reflect it is a property of the goal type. `GoalSummaryViewModel`, `GoalEntryViewModel`, and both test fixtures updated; the Modals→Models dependency inversion is gone (panel no longer imports `Modals.GoalSummary`). Kept public + UI-layer internal.

### IN-02: Request.GoalId never consumed

**Files modified:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs`, `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs`, `tests/Valt.Tests/UI/Screens/GoalSummaryViewModelTests.cs`, `tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs`
**Commit:** 69bbc5a
**Applied fix:** Verified via grep that `Request.GoalId` had no readers (only the call-site assignment). Removed the property and replaced it with a comment documenting that the id is intentionally omitted (query is pre-fetched by `ViewSummaryCommand`); updated the call site and tests.

### IN-03: DataGrid border missing — UI-SPEC color-table deviation

**Files modified:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml`
**Commit:** 7cb5e1e
**Applied fix:** Added `BorderBrush="{DynamicResource Background500Brush}" BorderThickness="1"` to the GoalSummary DataGrid, matching the header strip border and the 50-UI-SPEC.md color table.

## Skipped Issues

### IN-04: Close-button container Padding="10" (non-4px multiple)

**File:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml:174`
**Reason:** Documented no-change — `Padding="10"` is not an ad-hoc value: the global `Border.form-fields-area` style in `DefaultStyles.axaml` sets `Padding=10`, and the `LoanStateHistoryView` precedent this modal was copied from (plus `TransactionEditorView`, `FixedExpenseEditorView`, `AvgPriceLineEditorView`) all use `Padding="10"`. Substituting 8 only here would break visual consistency with every other modal using the class; the value is template-mandated, not a deviation.

### IN-05: pt-BR / es resx not updated

**File:** `src/Valt.UI/Lang/language.resx`
**Reason:** Intentional deferral — 50-UI-SPEC.md explicitly assigns pt-BR/es translations to Phase 51 (GOL-09); ResourceManager falls back to neutral strings. No action per fix scope.

_Note: IN-01 (sign-derivation flags) was out of the requested fix scope (left as-is; flags are asserted in tests)._

---

_Fixed: 2026-10-06_
_Fixer: the agent (gsd-code-fixer)_
_Iteration: 1_
