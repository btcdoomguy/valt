---
phase: 49-goal-contributing-transactions-query-backend
reviewed: 2026-10-06T20:24:47Z
depth: deep
files_reviewed: 22
files_reviewed_list:
  - src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs
  - src/Valt.App/Modules/Goals/DTOs/GoalContributingTransactionsResult.cs
  - src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsQuery.cs
  - src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsHandler.cs
  - src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs
  - src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/GoalContributionRow.cs
  - src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs
  - src/Valt.Infra/Modules/Goals/Services/SpendingLimitProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/IncomeFiatProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/SaveFiatProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/ReduceExpenseCategoryProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/SavingsRateProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/IncomeBtcProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/DcaProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/BitcoinHodlProgressCalculator.cs
  - src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs
  - src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsCurrency.cs
  - src/Valt.Infra/Modules/Goals/GoalPeriodRangeHelper.cs
  - src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs
  - src/Valt.Infra/Extensions.cs
  - tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs
  - tests/Valt.Tests/DatabaseTest.cs
findings:
  critical: 1
  warning: 3
  info: 3
  total: 7
status: fixed
fixed_at: 2026-10-06T20:34:54Z
fix_summary: "CR-01 fixed (sale fiat leg read from ToFiatAmount/ToAccountId + test assertions);
  WR-01 fixed (DTO doc aligned to magnitude contract); WR-02 fixed (null guard + test);
  WR-03 deferred to Phase 51 per GOL-08 (note under finding); IN-01 left as-is (magic settings
  key, low risk); IN-02 fixed (redundant re-sort removed, ordering contract documented);
  IN-03 left as-is (pre-existing intended yearly range semantics)."
remaining: none-blocking
---

# Phase 49: Code Review Report

**Reviewed:** 2026-10-06T20:24:47Z
**Depth:** deep
**Files Reviewed:** 22
**Status:** issues_found

## Summary

Reviewed the contributing-transactions query backend: new CQRS query/handler, App DTOs,
`IGoalQueries` extension, per-calculator row overrides for all nine transaction-based goal
types, the reader-backed `GetExpenseRows`/`GetIncomeRows` row paths with aggregates
re-derived as row sums, the new `GoalContributingTransactionsService`/`GoalContributionRow`/
`GoalContributingTransactionsCurrency` infra types, the `GoalPeriodRangeHelper` extraction,
DI wiring, and 944 lines of new handler tests plus the `DatabaseTest` fixture rebuild.

The architecture is sound: selection logic stays beside each calculator's progress math,
aggregates are literally `Sum(r => r.Contribution)` over the row path (no drift by
construction), ordering is deterministic `(Date, Id)`, and malformed/missing goal IDs map
to `GOAL_NOT_FOUND`. The test suite is unusually thorough (boundary dates, same-day
ordering, add/remove set-behavior, per-date sats conversion, multi-currency).

One **blocker**: the `BitcoinToFiat` row shape is wrong in two calculators. The entity
mapping (`ConvertToBtcToFiatTransferEntity`, `Extensions.cs:328-353`) stores the sale's
fiat leg in `ToFiatAmount` with the fiat account in `ToAccountId`, and sets
`FromFiatAmount = null` — but both `BitcoinHodlProgressCalculator` and
`StackBitcoinProgressCalculator` read `tx.FromFiatAmount` / `FromAccountId` for those rows.
The tests never assert `FiatAmount`/`FiatCurrencyCode` on sale rows, so the bug ships green.

## Critical Issues

### CR-01: BitcoinToFiat rows report zero fiat amount and wrong currency (BitcoinHodl, StackBitcoin)

**File:** `src/Valt.Infra/Modules/Goals/Services/BitcoinHodlProgressCalculator.cs:90-96`,
`src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs:107-111`
**Issue:** For `BitcoinToFiat` (sale) transactions, the only entity mapping
(`ConvertToBtcToFiatTransferEntity`, `src/Valt.Infra/Modules/Budget/Transactions/Extensions.cs:346-348`)
persists `FromFiatAmount = null`, the fiat proceeds in `ToFiatAmount`, and the fiat account
in `ToAccountId`. Both calculators read the sale row's fiat leg from the wrong side:

- `BitcoinHodlProgressCalculator.GetContributingTransactionsAsync` passes
  `tx.FromFiatAmount ?? 0m` (always 0) and resolves currency via
  `ResolveFromAccountCurrency(tx, ...)` which looks up `tx.FromAccountId` — the *BTC*
  account, whose `Currency` is null — so every sale row silently gets `FiatAmount = 0` and
  the main currency code.
- `StackBitcoinProgressCalculator.GetContributingTransactionsAsync` has the identical flaw
  for its `BitcoinToFiat` bucket (`signedFiatAmount = tx.FromFiatAmount ?? 0m`, currency
  from `FromAccountId`).

This violates the `ContributingTransactionRow` contract ("Zero for sats-only rows (no fiat
leg)") — a sale is not sats-only; it has a fiat leg. A user examining a HODL-goal sale row
or a StackBitcoin sale row sees a fabricated zero-fiat row in the wrong currency. Purchase
(`FiatToBitcoin`) rows are unaffected: that mapping stores the debit in `FromFiatAmount`
with the fiat account in `FromAccountId`.

The existing tests do not catch this: `BitcoinHodl_SoldSats_Reconciles_AndIsSupported` seeds
a sale with `fiatAmount: 200m` but asserts only `RunningTotal`/`SatsAmount`, never
`FiatAmount`/`FiatCurrencyCode`. The seeded sale's `AsBitcoinSale` → `AsEntity` mapping
produces exactly the production `FromFiatAmount = null` shape, so the zero-fiat assertion
would fail if anyone added it.

**Fix:** Read the fiat leg from the correct side for sale rows:
```csharp
// BitcoinHodlProgressCalculator.GetContributingTransactionsAsync (sale rows)
var isSale = tx.Type == TransactionEntityType.BitcoinToFiat;
var signedFiat = isSale
    ? (tx.ToFiatAmount ?? 0m)
    : (tx.FromFiatAmount ?? 0m);
var fiatAccountId = isSale ? tx.ToAccountId : tx.FromAccountId;
// resolve currency against fiatAccountId (fall back to main currency)
```
and in `StackBitcoinProgressCalculator`, set `hasFiatLeg` currency resolution to use
`tx.ToAccountId` for the `BitcoinToFiat` bucket. Add assertions to the existing sale-row
tests: `FiatAmount.Value == 200m`, `FiatCurrencyCode == "USD"` for the seeded sale.

## Warnings

### WR-01: `ContributingTransactionRow.FiatAmount` contract says "natural display sign" but the mapper always applies `Math.Abs`

**File:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs:112`
(DTO doc: `src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs:26-30`)
**Issue:** The DTO documents `FiatAmount` as "Fiat amount with the natural display sign,
income +, expense -". `MapRow` unconditionally does `FiatValue.New(Math.Abs(row.SignedFiatAmount))`,
so every expense row (and every StackBitcoin sale row, per CR-01) is delivered as a
positive magnitude. Either the doc or the code is wrong; the UI phase will bind to one of
them. Since the sign is recoverable only via strategy-specific knowledge of
`RunningTotal` deltas, a silent wrong choice here means wrong signs in the UI.

**Fix:** Decide the contract and align both sides. If the natural sign is wanted (matches
the doc), drop the `Math.Abs` and keep the sign from `SignedFiatAmount`; if magnitude is
wanted, fix the DTO doc to say "absolute value; sign is expressed only in RunningTotal".

### WR-02: Null `GoalId` throws `NullReferenceException` instead of `GOAL_NOT_FOUND`

**File:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs:66-79`
**Issue:** `TryParseObjectId` dereferences `value.Length` with no null guard. The query's
`required string GoalId` only guards the compiler; any caller that passes null (MCP tool
boundary, deserialization, a UI binding glitch) gets an unhandled NRE out of the handler
instead of the documented NotFound. The handler has no validator, so nothing upstream
catches it.

**Fix:** Guard at the top of `GetContributingTransactionsAsync`:
```csharp
if (string.IsNullOrWhiteSpace(goalId))
    return null;
```
(or add an `IValidator<GetGoalContributingTransactionsQuery>` returning a validation
failure, per the CQRS conventions in AGENTS.md).

### WR-03: New query not exposed via MCP tools (AGENTS.md MCP Impact Checklist item 1)

**File:** `src/Valt.Infra/Mcp/Tools/` (no `GetContributingTransactions` reference anywhere
in `Mcp/` or `Valt.UI/`)
**Issue:** AGENTS.md mandates considering MCP impact for every new query/command handler.
`GetGoalContributingTransactionsQuery` is a read-only goal introspection query — exactly
the kind of operation the AI-assistant surface benefits from — yet no `GoalTools` method
was added, and `McpServerService.ForwardServicesFromMainApp` was not touched. If the
decision is to defer MCP exposure to the UI phase, it should be recorded in the phase
artifacts; otherwise the tool is missing from the checklist's perspective.

**Fix:** Either add a `GetGoalContributingTransactions` MCP tool to `GoalTools.cs`
(returning a rows/not-supported DTO), or document the deferral in the phase summary so the
checklist item is consciously closed.

> **Deferred (2026-10-06, review-fix iteration 1):** MCP exposure of
> `GetGoalContributingTransactionsQuery` is **deferred to Phase 51** — it is requirement
> **GOL-08** in the v0.9 roadmap and will be delivered with the UI phase that consumes this
> query. No `GoalTools` method or `ForwardServicesFromMainApp` change in Phase 49; this
> checklist item is consciously closed as out-of-phase scope.

## Info

### IN-01: Magic settings key duplicates `BaseSettings` persistence format

**File:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsCurrency.cs:17`
**Issue:** `MainFiatCurrencyKey = "CurrencySettings.MainFiatCurrency"` re-encodes the
`$"{className}.{prop.Name}"` key format that `BaseSettings.Load`/`Save` generate by
reflection (`BaseSettings.cs:42-45`). Verified the strings match today, but renaming the
`CurrencySettings` class or the property silently degrades every direct-DB calculator
(StackBitcoin/BitcoinHodl/Dca/IncomeBtc) to a USD default with no compile-time signal —
and it would disagree with the reader-backed calculators, which use the cached
`CurrencySettings.MainFiatCurrency`.

**Fix:** Expose the persisted key from `BaseSettings` (e.g. a static
`BaseSettings.KeyFor<TSettings>(nameof(TSettings.MainFiatCurrency))`) or inject
`CurrencySettings` into the calculators instead of re-reading the collection.

### IN-02: Redundant re-sort in the service

**File:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs:62-66`
**Issue:** Every calculator override already emits rows ordered by `(Date, Id)` (reader
methods order, and each direct-DB override orders). The service re-applies the identical
ordering over the mapped rows. Harmless but duplicated logic; if a future calculator
intentionally orders differently (e.g. largest-first), the service will silently override it.

**Fix:** Either drop the re-sort and document "calculators must return rows in (Date, Id)
order" on `IGoalProgressCalculator.GetContributingTransactionsAsync`, or keep the re-sort
and delete the per-calculator ordering. One owner, not two.

### IN-03: `GoalPeriodRangeHelper` yearly range semantics are surprising for multi-year spans

**File:** `src/Valt.Infra/Modules/Goals/GoalPeriodRangeHelper.cs:19-20`
**Issue:** For `Yearly` with a `startDate`, the range is `[startDate, Dec 31 of
refDate.Year]` — e.g. startDate 2024-03-01 with refDate 2025-06-15 spans ~19 months. This
is verbatim pre-existing behavior (moved from `GoalQueries.GetPeriodRange`), so no
regression, but the helper is now advertised as the "single source of truth" and the
contributing-transactions query inherits the span. If a yearly goal's intent is
`startDate … startDate + 1 year`, both stale-goal detection and row selection currently
over-count.

**Fix:** No code change required if this matches the goal domain's intent — but confirm
against the Goals module docs (`.claude/docs/goals.md`) and add a boundary test for a
yearly goal whose startDate year differs from refDate year.

---

_Reviewed: 2026-10-06T20:24:47Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: deep_
