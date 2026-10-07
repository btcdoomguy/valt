---
phase: 50-goal-summary-modal-ui
reviewed: 2026-10-06T00:00:00Z
depth: phase-level
files_reviewed: 11
files_reviewed_list:
  - src/Valt.UI/Extensions.cs
  - src/Valt.UI/Views/ApplicationModalNames.cs
  - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml
  - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml.cs
  - src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs
  - src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml
  - src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs
  - src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs
  - src/Valt.UI/Lang/language.resx
  - src/Valt.UI/Lang/language.Designer.cs
  - tests/Valt.Tests/UI/Screens/GoalSummaryViewModelTests.cs
  - tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs
findings:
  blocker: 0
  warning: 2
  info: 5
  total: 7
status: fixed
fixed_at: 2026-10-06
fix_summary: >-
  WR-01 fixed: ViewSummaryCommand returns early on NotSupported query result
  (defense-in-depth) and CanViewSummary converted from deny-list to allow-list
  of the 9 supported goal-type DTOs; new NotSupported guard test added.
  WR-02 fixed: GoalSummaryStrategyUnit moved out of GoalSummaryViewModel to
  Valt.UI.Views.Main.Tabs.Transactions.Models as GoalStrategyUnit (own file);
  all references updated. IN-02 fixed: unused Request.GoalId removed (verified
  no readers). IN-03 fixed: DataGrid got BorderBrush=Background500Brush /
  BorderThickness=1 per UI-SPEC. IN-04 documented no-change: Padding=10 is the
  global form-fields-area style value and the LoanStateHistory precedent.
  IN-01 left as-is (flags used by tests); pt-BR/es deferral is intentional
  (Phase 51, GOL-09). Full suite green: 1818 passed.
---

# Phase 50: Code Review Report — Goal Summary Modal UI

**Reviewed:** 2026-10-06
**Depth:** phase-level (standard + cross-file API verification)
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Reviewed the full diff `88bc8a4..HEAD` for the Goal Summary modal against the
design contract in `50-UI-SPEC.md`. Build succeeds with 0 warnings/errors; all
28 tests in the two new/affected fixtures pass.

The implementation is largely faithful to the contract: chromeless modal
pattern copied from `LoanStateHistory` (SystemDecorations None, CustomTitleBar,
Escape → CloseCommand, CenterOwner), MinWidth/MinHeight 720×480, DynamicResource
brushes only, Geist/GeistMono fonts, `x:Static` localization, query awaited
before `IModalFactory.CreateAsync` (no async-load-in-modal), sign derivation
solely from `RunningTotal` deltas, sats-only/fiat-only rows render empty cells,
final total = last row's RunningTotal (never recomputed). DI registration and
modal-name enum (43) are correct; `ModalFactory.CreateAsync` plumbing
(`Parameter` → `OnBindParameterAsync`) verified end-to-end.

No security issues found (no injection surface, no secrets, no P/Invoke).
Findings below are robustness and contract-fidelity items.

## Narrative Findings (AI reviewer)

### WR-01: `ViewSummaryCommand` opens the modal for a `NotSupported` query result — misleading empty state

**File:** `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs:242-273`
**Issue:** The command checks only `result.IsFailure`. If the query succeeds with
`GoalContributingTransactionsResult.NotSupported` (returned by Phase 49 for
goal types without contributing-transaction semantics), the modal still opens.
`GoalSummaryViewModel.OnBindParameterAsync` then hits the
`request.Result is not Supported` early-return, leaving `HasRows = false`, so
the user sees "No transactions yet / No transactions contribute to this goal
yet." — factually wrong for an unsupported goal type.

Today this is unreachable because `CanViewSummary` gates out NetWorthBtc, but
the gate is a **deny-list** (`_goal.GoalType is not NetWorthBtcGoalTypeOutputDTO`,
`GoalEntryViewModel.cs:197`), so any *future* goal type automatically gets a
visible "View summary" menu item, defaults to `Fiat` strategy unit, and lands
in this misleading modal. There is no fail-safe anywhere in the chain.

**Fix:**
```csharp
// In ViewSummaryCommand, after the IsFailure check:
if (result.Value is not GoalContributingTransactionsResult.Supported)
    return; // or show an informative message; modal does not open

// And make the gate an allow-list in GoalEntryViewModel.cs:
public bool CanViewSummary => _goal.GoalType is
    StackBitcoinGoalTypeOutputDTO or IncomeBtcGoalTypeOutputDTO or
    BitcoinHodlGoalTypeOutputDTO or DcaGoalTypeOutputDTO or
    SavingsRateGoalTypeOutputDTO or SpendingLimitGoalTypeOutputDTO or
    SaveFiatGoalTypeOutputDTO or IncomeFiatGoalTypeOutputDTO or
    ReduceExpenseCategoryGoalTypeOutputDTO;
```

### WR-02: `GoalEntryViewModel` (Models layer) depends on the modal ViewModel

**File:** `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs:9,197-206`
**Issue:** The panel row model now imports
`Valt.UI.Views.Main.Modals.GoalSummary` and exposes
`GoalSummaryViewModel.GoalSummaryStrategyUnit` as its own `SummaryStrategyUnit`
property. A list-item model depending on a specific modal's nested enum inverts
the dependency direction — any rename/move of the modal or enum silently breaks
the panel, and the enum cannot be reused by a second consumer without dragging
the modal along. The strategy unit is a property of the *goal type*, not of this
modal.

**Fix:** Move `GoalSummaryStrategyUnit` to a neutral location
(e.g. `Valt.UI.Views.Main.Tabs.Transactions.Models.GoalStrategyUnit` or the
App-layer Goals DTOs) and have `GoalSummaryViewModel` reference it from there.

## Info

### IN-01: Sign-derivation flags are view-dead code

**File:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs:170-171` (RowItemViewModel)
**Issue:** `IsContributionPositive` / `IsContributionNegative` are bound nowhere in
`GoalSummaryView.axaml` — the view consumes only `ContributionForeground`.
Only the tests read them. Either bind them (e.g. `Classes.positive=` in the cell
templates) or drop them and assert on `ContributionForeground` in tests.

### IN-02: `Request.GoalId` is never consumed

**File:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs:175`
**Issue:** `GoalId` is required on the Request record but the modal never reads it
(the query is pre-detched by `ViewSummaryCommand`). Harmless, but it invites the
assumption the modal re-queries. Remove it or document it as trace-only.

### IN-03: DataGrid border missing — UI-SPEC color-table deviation

**File:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml:84`
**Issue:** 50-UI-SPEC.md Color table assigns `Background500Brush` to "Header
strip border, **DataGrid border**". The header strip border is set, but the
DataGrid has no `BorderBrush`/`BorderThickness`, so the grid edge renders with
Fluent default chrome against the window background. Add:
```xml
BorderBrush="{DynamicResource Background500Brush}" BorderThickness="1"
```

### IN-04: pt-BR / es resx not updated

**File:** `src/Valt.UI/Lang/language.resx` (only)
**Issue:** AGENTS.md mandates updating all three language files; only en was
touched. This is an explicit, documented deferral in 50-UI-SPEC.md ("pt-BR/es
translations are Phase 51, GOL-09"), and ResourceManager falls back to neutral
strings, so no runtime breakage — flagged only so the deferral isn't lost.

### IN-05: Close-button container uses non-4px-multiple padding

**File:** `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml:158-160`
**Issue:** `Padding="10"` on the close-button Border and `Margin="12"` on the
strip (12 is on-scale; 10 is not). The UI-SPEC spacing contract allows 4-multiples
only, with a narrow exception list that doesn't include this padding. Likely
copied from the `form-fields-area` class precedent; verify against
`LoanStateHistoryView` and either align to 8 or record the exception.

---

_Reviewed: 2026-10-06_
_Reviewer: gsd-code-reviewer_
_Depth: phase-level_
