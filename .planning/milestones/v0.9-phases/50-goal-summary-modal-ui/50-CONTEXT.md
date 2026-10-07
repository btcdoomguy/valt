# Phase 50: Goal Summary Modal UI - Context

**Gathered:** 2026-10-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can right-click any goal entry in the Goals section (Transactions tab) and inspect the full transaction breakdown behind its number in a read-only modal. This phase delivers the UI only: context-menu item, "View summary" command wiring, the chromeless modal with its grid, and neutral (en) strings. MCP tool, pt-BR/es translations, and docs are Phase 51 (GOL-08/09/10).

Backend dependency (Phase 49, COMPLETE): `GetGoalContributingTransactionsQuery` in `Valt.App/Modules/Goals/Queries/` returns `GoalContributingTransactionsResult` — `Supported(IReadOnlyList<ContributingTransactionRow>)` / `NotSupported(goalType)` / failure (`GOAL_NOT_FOUND`). Rows carry Date, Description, AccountName, CategoryName (nullable), FiatAmount (non-negative magnitude), FiatCurrencyCode, SatsAmount (non-negative magnitude), Contribution (signed), RunningTotal (signed, per-strategy unit). 9 supported strategies; `NetWorthBtc` returns `NotSupported`.

</domain>

<decisions>
## Implementation Decisions

### Modal Content & Layout
- Single read-only DataGrid filling the modal, with a header strip showing goal name, period, and the final reconciled total (the last row's RunningTotal).
- Running-total column formatted per strategy unit using existing formatters: fiat with 2dp + currency, sats with thousands separator, count as integer, percentage with 1dp.
- Separate **Fiat** and **Sats** columns side by side (Sats cell empty for fiat-only rows per the Phase 49 Q3 contract — zero/empty, not a fabricated conversion).

### Gating & Empty/Error States
- "View summary" visibility bound to a new `CanViewSummary` property on `GoalEntryViewModel` (false for `NetWorthBtc`) — the item is HIDDEN for unsupported types (locked user decision: no feature at all for non-transaction-based types).
- Empty contributing set (Supported with zero rows): menu item stays enabled; the modal opens with a localized empty-state message ("No transactions contribute to this goal yet").
- Secure mode: menu item DISABLED in secure mode (consistent with Edit — the breakdown would leak masked financial data).

### Wiring, Sizing & Localization Split
- New `ViewSummaryCommand` on `GoalsPanelViewModel`: dispatches `GetGoalContributingTransactionsQuery` via `IQueryDispatcher`, then opens the new modal via `IModalFactory` (pattern: `LoanStateHistory` / `FixedExpenseHistory` modals). The modal has its own VM receiving the goal entry + query result.
- Chromeless modal: `SystemDecorations="None"` with `CustomTitleBar`, `MinWidth="720"`, `MinHeight="480"`, sized ~800×560 (per AGENTS.md modal conventions).
- Localization split: neutral (en) `language.resx` strings added now so the feature works; pt-BR + es translations land in Phase 51 per roadmap (GOL-09). Designer.cs property added for each new key.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GoalsPanelView.axaml` — per-entry `Border.ContextMenu` with Edit/Recalculate/Delete; `RecalculateGoalCommand` + `CanRecalculate` binding pattern to copy for the new item.
- `GoalsPanelViewModel.cs` — already injects `ICommandDispatcher`, `IQueryDispatcher`, `IModalFactory`; `RecalculateGoalAsync` shows the error-display pattern.
- Modal analogs: `Views/Main/Modals/LoanStateHistory/` and `FixedExpenseHistory/` (chromeless + CustomTitleBar + DataGrid + own VM), `IModalFactory` (`src/Valt.UI/Services/IModalFactory.cs`).
- Phase 49 DTOs: `ContributingTransactionRow`, `GoalContributingTransactionsResult` in `Valt.App/Modules/Goals/DTOs/`.

### Established Patterns
- Modals: `SystemDecorations="None"`, custom title bar, `d:DesignWidth/Height` = MinWidth/MinHeight per AGENTS.md; opened via `_modalFactory.ShowDialogAsync` or equivalent.
- Localization: `x:Static local:language.<Key>` in AXAML; strings in `src/Valt.UI/Lang/language.resx` (+ pt-BR, es) with `language.Designer.cs` properties.
- Grid: DataGrid with column definitions bound to VM row items; existing formatters for fiat/sats amounts (check `FiatValue`/`BtcValue` display converters used in Transactions grid).

### Integration Points
- `GoalsPanelViewModel` — new command + `CanViewSummary` exposure.
- `GoalEntryViewModel` — new `CanViewSummary` property (requires goal type knowledge; check how GoalEntryViewModel is constructed and whether GoalTypeName is available).
- `IModalFactory` + DI registration (`Valt.UI` composition root) for the new modal.
- `language.resx` / `language.Designer.cs` — new keys.

</code_context>

<specifics>
## Specific Ideas

- The user locked: no View Summary at all for non-transaction-based goal types — hidden, not disabled, and no empty-state explanation modal for them.

</specifics>

<deferred>
## Deferred Ideas

- MCP tool exposure (GOL-08), pt-BR/es translations (GOL-09), `.claude/docs/goals.md` update (GOL-10) — all Phase 51 per roadmap.

</deferred>
