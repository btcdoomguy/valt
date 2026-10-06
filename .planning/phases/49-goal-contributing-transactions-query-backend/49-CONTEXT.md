# Phase 49: Goal Contributing-Transactions Query Backend - Context

**Gathered:** 2026-10-06
**Status:** Ready for planning

<domain>
## Phase Boundary

An App-layer query exposes exactly which transactions feed any goal's total calculation, with a running total that reconciles with the goal's displayed progress. This phase delivers the backend only: the per-strategy transaction-exposure mechanism, the CQRS query + DTOs, and handler tests proving reconciliation. UI (Phase 50) and MCP/localization/docs (Phase 51) build on top.

</domain>

<decisions>
## Implementation Decisions

### Strategy Exposure Mechanism
- Extend `IGoalProgressCalculator` with a method returning the contributing transaction rows (e.g., `GetContributingTransactionsAsync`) — selection logic stays in one place per strategy and cannot drift from the progress math.
- New CQRS query `GetGoalContributingTransactionsQuery` in `Valt.App/Modules/Goals/Queries/` returning a row DTO, backed by a new/existing Infra service (per AGENTS.md CQRS conventions — ViewModels must use dispatchers, not direct repository access).

### Non-Transaction-Based Goal Types
- Goal types whose progress is NOT derived from transactions (e.g., NetWorthBtc from account balances, BitcoinHodl from price) do NOT get the View Summary feature at all. The query returns a typed result identifying these types so the UI (Phase 50) can hide/disable the context-menu item for them.

### Row Shape & Running Total
- Rows sorted chronologically ascending — the running total reads naturally top-to-bottom.
- Running total mirrors the same accumulation the goal's progress math uses (e.g., cumulative spending for SpendingLimit, cumulative sats for StackBitcoin) — the final row must equal the goal's displayed progress.
- Amounts shown with natural sign convention (income +, expense −); the running total still follows the goal's accumulation math.
- Sats values converted at the transaction-date BTC price (consistent with the progress calculators' conversion), not the current live price.

### Data Loading & Reconciliation
- Extend the existing `GoalTransactionReader` loading/conversion machinery (accounts, price lookups, multi-currency conversion) rather than building a parallel loader.
- Per-strategy handler tests prove the returned set and final running total respond correctly to transaction add/remove and reconcile with `CalculateProgressAsync` output for the same goal.
- Compute on demand per goal (no cross-goal cache in this phase).

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator` — one calculator per goal type (SpendingLimit, StackBitcoin, ReduceExpenseCategory, SaveFiat, IncomeFiat, IncomeBtc, SavingsRate, Dca, NetWorthBtc, BitcoinHodl), all consuming `GoalProgressInput` and returning `GoalProgressResult`.
- `Valt.Infra/Modules/Goals/Services/GoalTransactionReader` — shared multi-currency transaction aggregation (`CalculateTotalExpenses`, `CalculateTotalIncome`) with price-database lookups and closest-date rate resolution; internal to Infra.
- `Valt.App/Modules/Goals/Queries/` — existing CQRS query patterns (GetGoal, GetGoals, GetStaleGoals) with DTOs in `Valt.App/Modules/Goals/DTOs/`.
- `Valt.Infra/Modules/Goals/Services/GoalProgressCalculatorFactory` — factory resolving calculator per `GoalTypeNames`.

### Established Patterns
- Progress calculators are internal Infra classes registered via `Valt.Infra/Modules/Goals/Extensions.cs`; queries flow App → Infra interfaces (`IGoalQueries`).
- Conversions use the price database with a 7-day buffer and closest-date resolution; amounts rounded via `FiatValue`/`Math.Round(2)`.
- Tests: NUnit + builders (`GoalBuilder`, `TransactionBuilder`, `FiatAccountBuilder`, `BtcAccountBuilder`), `DatabaseTest` base for in-memory LiteDB.

### Integration Points
- `IGoalProgressCalculator` extension point — new method must be implemented by all 10 calculators (or a default "not supported" fallback for non-transaction-based types).
- `GoalTransactionReader` — extend to expose per-transaction contributions with conversion context.
- App layer — new query + handler + DTO; auto-registered via `AddApplication()` scanning.

</code_context>

<specifics>
## Specific Ideas

- The user explicitly scoped OUT the View Summary feature for non-transaction-based goal types — hide it, don't show an empty-state explanation modal.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>
