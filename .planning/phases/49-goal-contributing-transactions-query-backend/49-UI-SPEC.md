---
phase: 49
slug: goal-contributing-transactions-query-backend
status: draft
shadcn_initialized: false
preset: none
created: 2026-10-06
---

# Phase 49 — UI Design Contract

> **Phase nature: BACKEND-ONLY.** This phase delivers an App-layer CQRS query
> (`GetGoalContributingTransactionsQuery`), row DTOs, and per-strategy
> reconciliation tests. There are **no user-facing surfaces** in this phase —
> the "View summary" modal UI is Phase 50, and localization/MCP/docs are Phase 51.
>
> Instead of visual contracts, this document defines the **DATA CONTRACT** that
> Phase 50's UI (and Phase 51's MCP tool) will consume. All visual dimensions
> (spacing, typography, color, copywriting, registry) are marked N/A.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (backend-only phase) |
| Preset | not applicable |
| Component library | not applicable — Avalonia 11.3 Fluent theme (no new UI components) |
| Icon library | not applicable — Phase 50 will use Material Design icon mapping (`MaterialSymbolsOutlined-map.json`) |
| Font | not applicable — existing app fonts (Geist, GeistMono) unchanged |

---

## Spacing Scale

**N/A — no user-facing surfaces in this phase.** Phase 50 will declare modal
spacing (project convention: margins/padding in multiples of 4; modals use
`MinWidth`/`MinHeight` from `d:DesignWidth`/`d:DesignHeight`).

---

## Typography

**N/A — no user-facing surfaces in this phase.** Phase 50 will declare grid
column header and row text styles.

---

## Color

**N/A — no user-facing surfaces in this phase.** One forward-looking data
contract note for Phase 50: the row DTO carries **natural signed amounts**
(income positive, expense negative), so the Phase 50 grid can apply the app's
existing green/red amount-foreground convention without re-deriving signs.

---

## Copywriting Contract

**N/A for this phase.** No new user-facing strings are introduced (nothing is
rendered). Per project convention, all localization strings — context menu
"View summary", modal title, column headers, empty state — are deferred to
Phase 51, which owns resx additions in `language.resx`, `language.pt-BR.resx`,
`language.es.resx`, and `language.Designer.cs`.

The only human-readable tokens this phase may define are **error codes** on
failure results (e.g. `GOAL_NOT_FOUND`, `GOAL_TYPE_NOT_SUPPORTED`) — codes,
not display copy. Display text for these belongs to Phase 51.

---

## Data Contract (replaces visual contract — consumed by Phase 50)

### Query

- **Name:** `GetGoalContributingTransactionsQuery` in
  `Valt.App/Modules/Goals/Queries/`, auto-registered via `AddApplication()` scanning.
- **Input:** goal identity sufficient to load the goal and its type
  (mirrors existing `GetGoal` query conventions).
- **Dispatch:** consumers use `IQueryDispatcher` only — no direct repository
  access from ViewModels (AGENTS.md CQRS convention).

### Result shape (typed union — Phase 50 branches on it)

| Result case | Payload | Phase 50 behavior |
|-------------|---------|-------------------|
| `Supported(IReadOnlyList<ContributingTransactionRow> Rows)` | Ordered row list (possibly empty) | Show modal with grid |
| `NotSupported(GoalTypeNames Type)` | The goal's type | **Hide/disable "View summary"** context-menu item — no empty-state explanation modal (locked decision, 49-CONTEXT.md `## Specifics`) |
| Failure (`GOAL_NOT_FOUND` etc.) | Error code + message | Existing app error handling |

### `ContributingTransactionRow` DTO

`Valt.App/Modules/Goals/DTOs/`, record with required init properties
(project DTO convention):

| Field | Type | Contract |
|-------|------|----------|
| `Date` | `DateOnly` | Transaction date |
| `Description` | `string` | Transaction description (may be empty — UI must tolerate, no invented placeholder) |
| `AccountName` | `string` | Display name of the account that held the transaction |
| `CategoryName` | `string?` | `null` when the transaction has no category (nullable, not empty string) |
| `FiatAmount` | `FiatValue` | Natural signed amount: income +, expense − (49-CONTEXT.md `### Row Shape & Running Total`) |
| `FiatCurrencyCode` | `string` | ISO code of the fiat amount (rows may span currencies) |
| `SatsAmount` | `BtcValue` | Converted at the **transaction-date BTC price** (closest-date resolution with the existing 7-day buffer), never the live price — consistent with the progress calculators' conversion |
| `RunningTotal` | decimal (fiat, rounded via `FiatValue` / `Math.Round(2)`) | Mirrors the goal's own accumulation semantics (e.g. cumulative spending for SpendingLimit, cumulative sats for StackBitcoin) |

### Ordering

- Rows are sorted **chronologically ascending** (oldest first). The running
  total reads naturally top-to-bottom. This is the single declared order —
  Phase 50 renders rows in returned order with no re-sorting.

### Reconciliation invariant (test-enforced)

- The **final row's `RunningTotal` exactly equals the goal's currently
  displayed progress** (`CalculateProgressAsync` output for the same goal),
  for every transaction-based goal type. Per-strategy handler tests prove:
  1. The returned set and final running total respond correctly to a
     contributing transaction being added and removed.
  2. The query reconciles with `CalculateProgressAsync` for the same goal.
- Goal types covered (all 8 transaction-based strategies):
  `SpendingLimit`, `StackBitcoin`, `ReduceExpenseCategory`, `SaveFiat`,
  `IncomeFiat`, `IncomeBtc`, `SavingsRate`, `Dca`.
- Non-transaction-based types return `NotSupported`:
  `NetWorthBtc` (account balances), `BitcoinHodl` (price).

### Empty-result semantics

- A supported goal with **no contributing transactions returns
  `Supported` with an empty row list — not an error, not `NotSupported`.**
  Phase 50 owns the graceful empty-state rendering; this contract only
  guarantees the empty-list case is representable.

### Data loading

- Implemented by extending the existing `GoalTransactionReader`
  loading/conversion machinery (accounts, price lookups, multi-currency
  conversion) and the per-strategy `IGoalProgressCalculator` selection logic —
  never a parallel loader. Selection logic lives in one place per strategy so
  the contribution set cannot drift from the progress math.
- Computed on demand per goal; **no cross-goal cache** in this phase.

---

## UI Considerations

None applicable — no UI surfaces exist in this phase. State coverage
(empty/loading/error/populated) is deferred to Phase 50's UI-SPEC, which will
consume this data contract. The one contract-level guarantee relevant to Phase
50 state handling: the empty contributing-set is a **successful empty list**,
and non-transaction-based goal types are a **typed `NotSupported` result**
(never an empty list, never an exception) so the context menu can hide the
feature deterministically.

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| — | — | — | No UI in this phase; data contract above is the deliverable |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| — | none | not required (no component registries; Avalonia/.NET project, shadcn N/A) |

---

## Checker Sign-Off

Backend-only phase. Visual dimensions 1–6 are N/A by construction; the
verifiable contract is the **Data Contract** section above, enforced by
per-strategy handler tests (Phase 49 success criterion 4).

- [ ] Dimension 1 Copywriting: N/A (no user-facing strings; error codes deferred to Phase 51)
- [ ] Dimension 2 Visuals: N/A (no surfaces)
- [ ] Dimension 3 Color: N/A (signed-amount convention noted for Phase 50)
- [ ] Dimension 4 Typography: N/A
- [ ] Dimension 5 Spacing: N/A
- [ ] Dimension 6 Registry Safety: N/A (no registries)

**Approval:** pending
