---
phase: 50
slug: goal-summary-modal-ui
status: draft
shadcn_initialized: false
preset: none
created: 2026-10-06
---

# Phase 50 — UI Design Contract

> Visual and interaction contract for the Goal Summary modal: a read-only,
> chromeless DataGrid modal showing the transactions behind a goal's progress
> number, opened from a new "View summary" item in the per-goal context menu.
> Consumes the Phase 49 data contract (`GetGoalContributingTransactionsQuery` /
> `GoalContributingTransactionsResult` / `ContributingTransactionRow`).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Avalonia 11.3 / .NET 9 project — shadcn gate N/A) |
| Preset | not applicable |
| Component library | Avalonia 11.3 Fluent theme + Valt custom styles (`Styles/DefaultStyles.axaml`, `Styles/CustomResources.axaml`) |
| Icon library | Material Symbols Outlined (`MaterialSymbolsOutlined-map.json`) — no new icons required this phase beyond what `CustomTitleBar` already provides |
| Font | Geist (text columns), GeistMono (amount/total columns) — existing `DynamicResource Geist` / `GeistMono` |

Modal chrome pattern is copied from the `LoanStateHistory` modal analog
(`Views/Main/Modals/LoanStateHistory/`): `SystemDecorations="None"`
(`WindowDecorations="None"` + `ExtendClientAreaToDecorationsHint="True"`),
`userControls:CustomTitleBar` with title/pressed/close bindings, Escape →
`CloseCommand`, `WindowStartupLocation="CenterOwner"`.

---

## Spacing Scale

Declared values (multiples of 4 only):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Inline gaps inside header strip (label-to-value), grid cell padding |
| sm | 8px | Header-strip internal padding; gap between header strip and DataGrid |
| md | 12px | Modal content outer margin (root content grid `Margin="12"` — replaces the `LoanStateHistory` 10px precedent to honor the multiple-of-4 contract) |
| lg | 16px | Empty-state panel padding; close-button area top margin |
| xl | 24px | Header-strip section separation (goal identity vs. period vs. total) |

Exceptions:

- Modal size: design `d:DesignWidth="800" d:DesignHeight="560"`,
  `MinWidth="720" MinHeight="480"`, no fixed MaxWidth/MaxHeight (user may
  enlarge to read long descriptions) — per AGENTS.md modal conventions and
  50-CONTEXT.md locked decision.
- Close button: `Width="100"` (matches `LoanStateHistory_Close` precedent).
- DataGrid row/column chrome uses Fluent defaults (`GridLinesVisibility="All"`,
  `CanUserResizeColumns="True"`), same as the `LoanStateHistory` grid.

---

## Typography

Exactly 3 sizes in use, all referencing existing `DynamicResource` tokens
(`Styles/CustomResources.axaml`: Small=12, Normal=13, Large=16).
Exactly 2 weights: Regular 400 and Semibold 600.

| Role | Size | Weight | Line Height | Font |
|------|------|--------|-------------|------|
| Grid rows / column headers | 12 (`FontSizeSmall`) | 400 (rows) / 600 (headers) | default single-line (~1.2) | Geist (text), GeistMono (amounts) |
| Header strip: goal name, labels | 13 (`FontSizeNormal`) | 600 (goal name) / 400 (labels) | default single-line | Geist |
| Empty state body | 12 (`FontSizeSmall`) | 400 | default | Geist |
| Header strip: final reconciled total | 16 (`FontSizeLarge`) | 600 | default single-line | GeistMono |

Rules:

- All grid cells are single-line; `TextTrimming="CharacterEllipsis"` on
  Description, Account, and Category cells (precedent: `GoalsPanelView.axaml`
  goal-name/description blocks).
- Amount columns (Fiat, Sats, Running total) use GeistMono for digit
  alignment (precedent: `GoalsPanelView.axaml` SUCCESS/FAILED labels and
  `GoalEntryViewModel` sats formatting use GeistMono).
- Date column: `Date.ToShortDateString()` culture formatting (precedent:
  `LoanStateHistoryViewModel.LoadTimelineAsync`).

---

## Visuals

**Focal point:** the final reconciled total in the header strip (16px Semibold
GeistMono) is the primary visual anchor — it draws the eye first. The goal name
(13px Semibold) is secondary; the grid body is tertiary.

## Color

60/30/10 split using existing `DynamicResource` brushes only — no new hex
values, no hardcoded colors.

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | Window default background (no explicit Background on root, same as `LoanStateHistoryView`) | Modal body, DataGrid surface |
| Secondary (30%) | `Background800Brush` | Header strip background |
| Secondary (30%) | `Background500Brush` | Header strip border, DataGrid border |
| Text primary | `Text100Brush` (implicit) / `Text400Brush` (secondary labels, period, empty state, category/account muted text) | Header strip labels, empty state |
| Semantic positive | `SemanticPositive200Brush` | Positive amount/running-total cells (income, stack additions) |
| Semantic negative | `SemanticNegative200Brush` | Negative amount/running-total cells (expenses, spending-limit consumption) |

Accent reserved for: **nothing in this phase.** This is a read-only,
informational modal — there is no accent-colored interactive element. The only
button (Close) uses default Fluent button chrome. Chromatic color is limited to
the signed-amount semantic convention inherited from the Phase 49 data
contract (natural signed amounts: income +, expense −, rendered green/red per
the app-wide amount-foreground convention).

Accent reserved-for list: none.

Destructive color: **none** — this phase introduces no destructive action
(Delete remains on the panel context menu, outside this modal).

Per-row sign derivation (prescriptive): the row DTO carries non-negative
magnitudes (`FiatAmount`, `SatsAmount`) and a signed `RunningTotal`. The UI
derives each row's contribution sign from the `RunningTotal` delta
(current row − previous row; first row = its own `RunningTotal` sign) and
applies SemanticPositive/SemanticNegative foreground to the Fiat, Sats, and
Running-total cells accordingly. Zero/neutral cells stay default foreground.
Sats-only rows (empty Fiat cell) and fiat-only rows (empty Sats cell) render
the empty cell with default foreground — never a fabricated `0`.

---

## Copywriting Contract

Neutral (en) strings only this phase; keys added to `language.resx` +
`language.Designer.cs`. pt-BR/es translations are Phase 51 (GOL-09, deferred
per 50-CONTEXT.md).

| Element | Copy | Resx key (proposed) |
|---------|------|---------------------|
| Context menu item | View summary | `Goals_ViewSummary` |
| Modal title bar / window title | Goal summary | `GoalSummary_Title` |
| Header strip — period label | Period | `GoalSummary_PeriodLabel` |
| Header strip — final total label | Total | `GoalSummary_TotalLabel` |
| Column header: date | Date | reuse existing transaction-grid date key if present, else `GoalSummary_ColumnDate` |
| Column header: description | Description | `GoalSummary_ColumnDescription` |
| Column header: account | Account | `GoalSummary_ColumnAccount` |
| Column header: category | Category | `GoalSummary_ColumnCategory` |
| Column header: fiat | Fiat | `GoalSummary_ColumnFiat` |
| Column header: sats | Sats | reuse `SatsLabel` pattern / `GoalSummary_ColumnSats` |
| Column header: running total | Running total | `GoalSummary_ColumnRunningTotal` |
| Empty state heading | No transactions yet | `GoalSummary_EmptyTitle` |
| Empty state body | No transactions contribute to this goal yet. | `GoalSummary_EmptyMessage` (locked copy from 50-CONTEXT.md `### Gating & Empty/Error States`) |
| Close button | Close | reuse `LoanStateHistory_Close` or new `GoalSummary_Close` |
| Error state | Query failure: show via existing `MessageBoxHelper.ShowErrorAsync(language.Error, message)` — modal does not open (precedent: `RecalculateGoalAsync` error-display pattern in `GoalsPanelViewModel`) | reuse `language.Error` |

Notes:

- The header strip goal name and period value are data-bound (goal
  `FriendlyName`; period from the goal entry, e.g. monthly period label), not
  literal copy.
- The final reconciled total is the **last row's `RunningTotal`**, formatted
  per strategy unit (see Formatting Contract below) — never recomputed in the
  UI.
- Destructive confirmation: **none** this phase.

### Formatting Contract (per-strategy running-total unit)

| Goal strategy unit | Running-total format | Amount columns |
|--------------------|----------------------|----------------|
| Fiat (SpendingLimit, SaveFiat, IncomeFiat, ReduceExpenseCategory) | `CurrencyDisplay.FormatFiat(value, rowCurrency)` — 2dp + currency code | Fiat cell formatted; Sats cell = `{SatsAmount:N0} sats` when present |
| Sats (StackBitcoin, IncomeBtc, BitcoinHodl) | `CurrencyDisplay.FormatSatsAsNumber(sats)` — thousands-separated | Sats cell formatted; Fiat cell empty when `FiatAmount` is zero (sats-only row) |
| Count (Dca) | Integer, thousands-separated (`{0:N0}`) | Fiat/Sats cells per row availability |
| Percentage (SavingsRate) | `{0:N1}%` — 1 decimal place | Fiat/Sats cells per row availability |

---

## UI Considerations

Applicable state considerations resolved: 9 covered, 1 backstop, 0 unresolved.

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| empty | Contributing-transaction grid (Supported, zero rows) | ✅ covered | Empty state copy (`No transactions yet` / `No transactions contribute to this goal yet.`) replaces the grid; menu item stays enabled per 50-CONTEXT.md |
| populated | Grid rows | ✅ covered | Rows render in query-returned chronological order with no re-sorting (49-UI-SPEC Ordering contract); final row's RunningTotal equals the goal's displayed progress |
| zero-one-many | 8 transaction-based strategies × 4 running-total units | ✅ covered | Per-unit Formatting Contract table above; strategy unit known from goal type at VM level |
| partial | Sats-only / fiat-only rows | ✅ covered | Empty counterpart cell (no fabricated conversion), Phase 49 Q3 contract |
| error | Query dispatch failure (`GOAL_NOT_FOUND`, infra errors) | ✅ covered | Modal does not open; `MessageBoxHelper.ShowErrorAsync(language.Error, message)` — RecalculateGoalAsync precedent |
| loading | Query dispatch before modal opens | ✅ covered | No loading chrome: `ViewSummaryCommand` awaits `IQueryDispatcher.DispatchAsync` before calling `IModalFactory`, so the modal opens fully populated (no async-load-in-modal, unlike LoanStateHistory) |
| gating | `CanViewSummary` / secure mode | ✅ covered | Menu item `IsVisible="{Binding CanViewSummary}"` (false → hidden for NetWorthBtc; locked decision: hidden, not disabled, no explanation modal); `IsEnabled="{Binding !IsSecureModeEnabled}"` (disabled in secure mode, consistent with Edit) |
| overflow | Long descriptions/accounts/categories; many rows | ✅ covered | `TextTrimming="CharacterEllipsis"` on text cells; DataGrid vertical scroll; user-resizable columns; window min 720×480, resizable |
| long-text | Header strip goal name | 🧪 backstop | Goal name truncated with ellipsis at ~40 chars visual check at verify time; statement: "Goal name in header strip truncates without pushing the period/total sections out of view at 720px min width" |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| — | none | not required (Avalonia/.NET project; no component registries; shadcn N/A) |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
