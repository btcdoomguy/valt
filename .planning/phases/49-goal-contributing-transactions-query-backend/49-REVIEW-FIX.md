---
phase: 49-goal-contributing-transactions-query-backend
fixed_at: 2026-10-06T20:34:54Z
review_path: .planning/phases/49-goal-contributing-transactions-query-backend/49-REVIEW.md
iteration: 1
findings_in_scope: 7
fixed: 5
skipped: 0
deferred: 1
left_as_is: 2
status: all_fixed
---

# Phase 49: Code Review Fix Report

**Fixed at:** 2026-10-06T20:34:54Z
**Source review:** `.planning/phases/49-goal-contributing-transactions-query-backend/49-REVIEW.md`
**Iteration:** 1
**Verification:** `dotnet build Valt.sln` + full `dotnet test` (1804 passed, 0 failed) run in
the main checkout (`workflow.use_worktrees=false`).

**Summary:**
- Findings in scope: 7 (1 critical, 3 warnings, 3 info)
- Fixed: 5 (CR-01, WR-01, WR-02, INFO-2, plus WR-03 resolved by documented deferral)
- Skipped: 0
- Left as-is by explicit scope decision: 2 (INFO-1, INFO-3)

## Fixed Issues

### CR-01: BitcoinToFiat rows report zero fiat amount and wrong currency

**Files modified:** `src/Valt.Infra/Modules/Goals/Services/BitcoinHodlProgressCalculator.cs`,
`src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs`,
`src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsCurrency.cs`,
`tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs`
**Commit:** `ecb7d52`
**Applied fix:** The production entity mapping persists a sale's fiat leg in `ToFiatAmount`
with the fiat account in `ToAccountId` (`FromFiatAmount` is always null). Both calculators
now read the fiat leg from the correct side per transaction type (`isSale ? To* : From*`),
and currency resolution goes through a new `ResolveAccountCurrency(ObjectId?, ...)` helper
(`ResolveFromAccountCurrency` delegates to it). Tests strengthened:
`BitcoinHodl_SoldSats_Reconciles_AndIsSupported` now asserts `FiatAmount.Value == 200m` /
`FiatCurrencyCode == "USD"` on the sale row, and `StackBitcoin_FourBuckets_NetSats_Reconcile`
asserts purchase (500 USD) and sale (150 USD) fiat fields.

### WR-01: `ContributingTransactionRow.FiatAmount` doc contradicts `Math.Abs` in MapRow

**Files modified:** `src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs`
**Commit:** `7f6d8a9`
**Applied fix:** Chose the magnitude contract (matches `MapRow`'s `Math.Abs` and every
existing test assertion): `FiatAmount`/`SatsAmount` docs now state they are always
non-negative magnitudes and that the contribution sign lives only in `RunningTotal` deltas;
type summary updated to match. No code change needed; no test contradicts.

### WR-02: Null `GoalId` throws NRE instead of `GOAL_NOT_FOUND`

**Files modified:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs`,
`tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs`
**Commit:** `3001913`
**Applied fix:** `TryParseObjectId` now guards `string.IsNullOrEmpty(value)` before the
length check, so a null goal id maps to `GOAL_NOT_FOUND` instead of an unhandled NRE. New
regression test `NullGoalId_ReturnsGoalNotFound` passes `{ GoalId = null! }` through the handler.

### WR-03: New query not exposed via MCP tools — DEFERRED, not implemented

**Files modified:** `.planning/phases/49-goal-contributing-transactions-query-backend/49-REVIEW.md` (note appended under the finding)
**Commit:** none — `.planning/` is gitignored in this repo; the note exists on disk only.
**Applied fix:** Per explicit scope decision, no MCP tool was added. A "Deferred" note under
WR-03 records that MCP exposure is requirement **GOL-08**, planned for **Phase 51** per the
v0.9 roadmap, consciously closing the AGENTS.md MCP Impact Checklist item as out-of-phase scope.

### IN-02: Redundant re-sort in the service

**Files modified:** `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs`,
`src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs`
**Commit:** `1a81bf1`
**Applied fix:** Gave row ordering a single owner: dropped the service's re-sort (verified
every calculator override and the reader-backed paths already order by `(Date, Id)` — 26
handler tests still pass) and documented the ordering contract on
`IGoalProgressCalculator.GetContributingTransactionsAsync`.

## Left As-Is (explicit scope decision)

### IN-01: Magic settings key duplicates `BaseSettings` persistence format

**Reason:** Fix is a non-trivial refactor (expose `BaseSettings.KeyFor<T>()` or inject
`CurrencySettings` into four calculators); strings verified to match today, risk is low.
Left for a dedicated cleanup.

### IN-03: `GoalPeriodRangeHelper` yearly range semantics

**Reason:** Pre-existing, verbatim-moved intended behavior — no regression; user confirmed
leave as-is.

## Verification

- `dotnet build Valt.sln` — 0 errors.
- `dotnet test` — 1804 passed, 0 failed, 0 skipped (30s).
- Gates ran in the **main checkout** (`workflow.use_worktrees=false` in `.planning/config.json`);
  numbers are reproducible from the committed tree.

---

_Fixed: 2026-10-06T20:34:54Z_
_Fixer: the agent (gsd-code-fixer)_
_Iteration: 1_
