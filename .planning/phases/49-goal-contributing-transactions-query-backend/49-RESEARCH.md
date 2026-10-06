# Phase 49: Goal Contributing-Transactions Query Backend - Research

**Researched:** 2026-10-06
**Domain:** .NET 9 CQRS / Avalonia desktop app — App-layer query over per-strategy goal progress calculators
**Confidence:** HIGH

## Summary

This phase adds one App-layer CQRS query, `GetGoalContributingTransactionsQuery`, that returns the ordered set of transactions contributing to any goal's progress, with a per-row running total whose final value reconciles exactly with `CalculateProgressAsync` output. All ten goal progress calculators were read in-session and classified by selection semantics. Eight are unambiguously transaction-derived; `NetWorthBtc` is balance-derived (not transaction-based); and **`BitcoinHodl` is in fact transaction-derived** (it sums `BitcoinToFiat` sales in the period) — contradicting the CONTEXT.md assumption that it is price-based. This discrepancy is flagged in Open Questions Q1.

The recommended mechanism (locked by CONTEXT.md) is to extend `IGoalProgressCalculator` with a rows method, with a default `NotSupported` fallback for non-transaction-based types. Feasibility was validated against every calculator: all selection predicates are explicit `Where` clauses over `TransactionEntityType` + amount signs, fully reproducible per-row. The one structural gap is `GoalTransactionReader`: its two aggregate methods (`CalculateTotalExpenses`/`CalculateTotalIncome`) hide per-transaction selection internally, so it needs new row-level counterparts — ideally with the aggregates re-implemented as sums of the row methods so drift is impossible by construction.

The dependency direction supports the plan cleanly: Infra already references App contracts/DTOs (`GoalQueries` implements `Valt.App.Modules.Goals.Contracts.IGoalQueries` and returns App DTOs), so an App-layer row DTO union (`Supported`/`NotSupported`) can flow from calculators through a new Infra service into the App handler. Infra internals are visible to tests (`InternalsVisibleTo("Valt.Tests")`), and `DatabaseTest` + `PriceDataBuilder` cover all seeding needs.

**Primary recommendation:** Extend `IGoalProgressCalculator` with `GetContributingTransactionsAsync(GoalProgressInput)` returning an internal row-bearing result; add row-level methods to `IGoalTransactionReader` and re-implement its aggregates as row sums; expose via a new `IGoalQueries` contract method implemented by a thin Infra service that resolves account/category names once (dictionary-join pattern from `TransactionQueries`); wrap in `Result<>` with `GOAL_NOT_FOUND` via `Error.NotFound("Goal", id)`; prove reconciliation + add/remove responsiveness per strategy in `DatabaseTest`-based handler tests.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Extend `IGoalProgressCalculator` with a method returning the contributing transaction rows (e.g., `GetContributingTransactionsAsync`) — selection logic stays in one place per strategy and cannot drift from the progress math.
- New CQRS query `GetGoalContributingTransactionsQuery` in `Valt.App/Modules/Goals/Queries/` returning a row DTO, backed by a new/existing Infra service (per AGENTS.md CQRS conventions — ViewModels must use dispatchers, not direct repository access).
- Goal types whose progress is NOT derived from transactions (e.g., NetWorthBtc from account balances, BitcoinHodl from price) do NOT get the View Summary feature at all. The query returns a typed result identifying these types so the UI (Phase 50) can hide/disable the context-menu item for them.
- Rows sorted chronologically ascending — the running total reads naturally top-to-bottom.
- Running total mirrors the same accumulation the goal's progress math uses (e.g., cumulative spending for SpendingLimit, cumulative sats for StackBitcoin) — the final row must equal the goal's displayed progress.
- Amounts shown with natural sign convention (income +, expense −); the running total still follows the goal's accumulation math.
- Sats values converted at the transaction-date BTC price (consistent with the progress calculators' conversion), not the current live price.
- Extend the existing `GoalTransactionReader` loading/conversion machinery (accounts, price lookups, multi-currency conversion) rather than building a parallel loader.
- Per-strategy handler tests prove the returned set and final running total respond correctly to transaction add/remove and reconcile with `CalculateProgressAsync` output for the same goal.
- Compute on demand per goal (no cross-goal cache in this phase).

### the agent's Discretion
(None stated explicitly — "None — discussion stayed within phase scope." for deferred ideas.)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GOL-05 | Grid shows per transaction: date, description, account, category, and amount in fiat and sats | `ContributingTransactionRow` DTO contract (49-UI-SPEC); name resolution via dictionary-join on accounts/categories (pattern: `TransactionQueries.cs:113-115`); sats at tx-date price via `GoalTransactionReader` conversion machinery (`GoalTransactionReader.cs:211-222`) |
| GOL-06 | Grid shows a running total column that accumulates according to the goal's progress calculation | Per-strategy accumulation semantics table (see Calculator Classification); universal definition: `RunningTotal_i = strategy-calc(rows[0..i])`, so final row reconciles by construction |
| GOL-07 | Contributing-transaction set is derived from each goal type's progress strategy (App-layer query), so the summary always matches the calculated totals | Locked mechanism: extend `IGoalProgressCalculator` with rows method; drift-proofing recommendation: aggregates re-implemented as row sums; reconciliation tests per strategy |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Per-strategy transaction selection + conversion | Infra (`Valt.Infra/Modules/Goals/Services/`) | — | Calculators already live here and own the selection predicates; must not drift from progress math |
| Row name resolution (account/category) | Infra (`GoalQueries`/new service) | — | Entity dictionaries already loaded for conversion; join once, centrally |
| Query orchestration (load goal, build period range, dispatch to calculator) | Infra service behind App contract | App handler | Mirrors `GetStaleGoalsAsync` → `GoalProgressUpdaterJob` flow; App handler stays a thin pass-through like `GetGoalHandler` |
| CQRS query surface + typed result union | App (`Valt.App/Modules/Goals/`) | — | AGENTS.md CQRS convention; UI consumes via `IQueryDispatcher` only |
| Running-total accumulation | Infra (per calculator rows method) | — | Same code path as progress math |

## Standard Stack

### Core
No new external packages. The phase composes existing in-repo infrastructure:

| Component | Location | Purpose | Why Standard |
|-----------|----------|---------|--------------|
| `IGoalProgressCalculator` | `Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs` | Extension point for per-strategy row exposure | Locked decision; selection logic stays beside progress math |
| `IGoalTransactionReader` / `GoalTransactionReader` | `Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs` | Multi-currency tx loading, closest-date price resolution (7-day buffer, `GoalTransactionReader.cs:138-140`), fiat/BTC conversion | Locked decision: extend, don't parallel-build |
| `IGoalQueries` (App contract) | `Valt.App/Modules/Goals/Contracts/IGoalQueries.cs` | Infra-facing query surface, implemented by Infra `GoalQueries` | Established pattern (`GetGoalAsync` returns App DTOs from Infra) |
| `Result<T>` / `Error` | `Valt.App/Kernel/Result.cs`, `Error.cs` | Railway-oriented failure (`Error.NotFound("Goal", id)` → code `GOAL_NOT_FOUND` verbatim `Error.cs:21-22`) | UI-SPEC failure contract |
| NUnit + NSubstitute + builders | `tests/Valt.Tests` | Per-strategy reconciliation tests | AGENTS.md testing rules |
| `DatabaseTest` / `PriceDataBuilder` | `tests/Valt.Tests/DatabaseTest.cs`, `tests/Valt.Tests/Builders/PriceDataBuilder.cs` | In-memory LiteDB + seeded BTC/fiat price ranges | Success criterion 4: "no UI or database beyond the `DatabaseTest` base" |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extending `IGoalProgressCalculator` (locked) | Separate `IGoalContributionProvider` per strategy | Would duplicate selection predicates; drift risk the locked decision explicitly rejects |
| Extending `IGoalQueries` | Brand-new App contract interface | Unnecessary churn; `IGoalQueries` is the established Infra-facing goals query surface |

**Installation:** none — no new NuGet packages.

**Version verification:** N/A (no registry packages). SDK on machine: .NET SDK `10.0.101` [VERIFIED: `dotnet --version`]; project targets .NET 9 per AGENTS.md.

## Calculator Classification (core research deliverable)

All 10 calculators read in-session. Verbatim selection predicates quoted with citations.

### Transaction-based (selection fully explicit, per-row reproducible)

| GoalType | Calculator | Selects (verbatim predicates) | Running-total semantics | Feasibility |
|----------|-----------|------------------------------|------------------------|-------------|
| `SpendingLimit` | `SpendingLimitProgressCalculator.cs` | "Only includes real expenses: Fiat debits and Bitcoin debits. Does NOT include transfers" — `tx.Type == TransactionEntityType.Fiat && tx.FromFiatAmount < 0` and `tx.Type == TransactionEntityType.Bitcoin && tx.FromSatAmount < 0` [VERIFIED: `GoalTransactionReader.cs:62-74`] | Cumulative spending in main fiat (positive accumulation of absolute converted amounts) | HIGH — same loop, expose rows instead of sum |
| `ReduceExpenseCategory` | `ReduceExpenseCategoryProgressCalculator.cs:22-25` | Same as SpendingLimit + `CategoryId == targetCategoryId` filter [VERIFIED: `GoalTransactionReader.cs:167-169`] | Same, category-filtered | HIGH |
| `SaveFiat` | `SaveFiatProgressCalculator.cs:21-23` | Union of income rows (`Fiat && FromFiatAmount > 0` [VERIFIED: `GoalTransactionReader.cs:90`]) and expense rows | Cumulative `income − expenses` in main fiat | HIGH — merge + sort both row sets |
| `IncomeFiat` | `IncomeFiatProgressCalculator.cs:22` | `Fiat && FromFiatAmount > 0` only; "BitcoinToFiat is NOT counted as fiat income" [VERIFIED: `GoalTransactionReader.cs:95-96`] | Cumulative income in main fiat | HIGH |
| `SavingsRate` | `SavingsRateProgressCalculator.cs:23-35` | Same union as SaveFiat | Cumulative `(inc−exp)/inc × 100`, clamped ±100, 0 when cumulative inc ≤ 0 — incremental: track two accumulators | MEDIUM-HIGH — ratio is incremental-friendly, but rows mix signs and the running value is a percentage, not an amount (see Q2) |
| `StackBitcoin` | `StackBitcoinProgressCalculator.cs:33-53` | Four buckets: `FiatToBitcoin && ToSatAmount > 0` (+), `Bitcoin && FromSatAmount > 0` (+), `BitcoinToFiat && FromSatAmount < 0` (−), `Bitcoin && FromSatAmount < 0` (−) | Cumulative net sats (decimal can carry long) | HIGH — direct `_localDatabase` query, no reader dependency |
| `IncomeBtc` | `IncomeBtcProgressCalculator.cs:33-35` | `Bitcoin && FromSatAmount > 0` | Cumulative sats | HIGH |
| `Dca` | `DcaProgressCalculator.cs:28-30` | `Type == FiatToBitcoin` in range; progress is a **purchase count**, not an amount [VERIFIED: `DcaProgressCalculator.cs:28-38`] | Cumulative count (as decimal) | HIGH for selection; running total is a count, not fiat (see Q2) |
| `BitcoinHodl` ⚠️ | `BitcoinHodlProgressCalculator.cs:31-34` | `BitcoinToFiat && FromSatAmount < 0` — sold sats in period | Cumulative sold sats | HIGH — **but CONTEXT says non-transaction-based; code contradicts (Q1)** |

### Not transaction-based

| GoalType | Calculator | Derives from | Disposition |
|----------|-----------|--------------|-------------|
| `NetWorthBtc` | `NetWorthBtcProgressCalculator.cs:34-79` | Visible account caches (`GetAccountCaches`) + latest BTC/fiat prices | `NotSupported` — matches locked decision |

## Architecture Patterns

### System Architecture Diagram

```
UI (Phase 50) ──IQueryDispatcher──► GetGoalContributingTransactionsHandler (App)
                                          │ Result<GoalContributingTransactionsResult>
                                          ▼
                              IGoalQueries.GetContributingTransactionsAsync(goalId)
                                          │ (App contract, implemented in Infra)
                                          ▼
                              Infra: GoalContributingTransactionsService (new)
                                 │                    │
                    Load goal entity        Resolve calculator via
                    + period range          IGoalProgressCalculatorFactory
                    (GetPeriodRange logic)          │
                                 │                  ▼
                                 │      calculator.GetContributingTransactionsAsync
                                 │      (GoalProgressInput) ──► IGoalTransactionReader
                                 │      row-level methods (multi-currency conversion,
                                 │      closest-date price, 7-day buffer)
                                 ▼
                    Map rows → App DTO (account/category name
                    dictionary-join, sats at tx-date price)
                                 │
              ┌──────────────────┼─────────────────────┐
              ▼                  ▼                     ▼
     Supported(rows)     NotSupported(type)    Failure(GOAL_NOT_FOUND)
     (ascending, running  (NetWorthBtc,        (goal id absent)
      total per strategy)  BitcoinHodl per lock)
```

### Recommended Project Structure
```
src/
├── Valt.App/Modules/Goals/
│   ├── Queries/GetGoalContributingTransactions/   # new: Query + Handler
│   ├── DTOs/ContributingTransactionRow.cs         # new: row DTO
│   └── DTOs/GoalContributingTransactionsResult.cs # new: Supported/NotSupported union
├── Valt.App/Modules/Goals/Contracts/IGoalQueries.cs
│                                                  # + GetContributingTransactionsAsync
└── Valt.Infra/Modules/Goals/
    ├── Services/IGoalProgressCalculator.cs        # + rows method (default NotSupported)
    ├── Services/GoalTransactionReader.cs          # + row-level methods; aggregates as row sums
    ├── Services/GoalContributingTransactionsService.cs  # new (or fold into GoalQueries)
    └── Queries/GoalQueries.cs                     # GetPeriodRange → shared/internal helper

tests/Valt.Tests/Application/Goals/Queries/
└── GetGoalContributingTransactionsHandlerTests.cs # per-strategy fixtures
```

### Pattern 1: Typed union result (Success / NotSupported / Failure)
**What:** Query returns `Result<GoalContributingTransactionsResult>` where the value is a closed record union.
**When to use:** Always for this query — Phase 50 branches on the case (show modal / hide menu item / error).
**Example:**
```csharp
// Shape contract from 49-UI-SPEC (names at planner's discretion)
public abstract record GoalContributingTransactionsResult
{
    public sealed record Supported(IReadOnlyList<ContributingTransactionRow> Rows)
        : GoalContributingTransactionsResult;
    public sealed record NotSupported(GoalTypeNames Type)
        : GoalContributingTransactionsResult;
}

// Handler (App) — GOAL_NOT_FOUND via existing Error factory:
// Error.NotFound("Goal", id) yields Code "GOAL_NOT_FOUND" verbatim [VERIFIED: src/Valt.App/Kernel/Error.cs:21-22]
```

### Pattern 2: Default interface method for NotSupported
**What:** Non-transaction-based calculators do not implement row logic; the interface supplies a default.
**When to use:** `NetWorthBtc` (and `BitcoinHodl` if the lock stands). Avoids 2 stub classes and makes "not supported" a compile-time default.
**Example:**
```csharp
// Extends existing interface [VERIFIED: src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs:8-12]
public interface IGoalProgressCalculator
{
    GoalTypeNames SupportedType { get; }
    Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input);

    Task<GoalContributingTransactionsResult?> GetContributingTransactionsAsync(GoalProgressInput input)
        => Task.FromResult<GoalContributingTransactionsResult?>(
            new GoalContributingTransactionsResult.NotSupported(input.TypeName));
}
```

### Pattern 3: Row-level reader with aggregates as row sums
**What:** Add row-returning methods to `IGoalTransactionReader`; re-implement `CalculateTotalExpenses`/`CalculateTotalIncome` as `Sum` over those rows.
**When to use:** SpendingLimit, ReduceExpenseCategory, SaveFiat, IncomeFiat, SavingsRate — the five reader-backed strategies.
**Why:** Drift impossible by construction; existing mocked-reader calculator tests keep passing (NSubstitute mocks the interface; `InternalsVisibleTo("DynamicProxyGenAssembly2")` [VERIFIED: `Valt.Infra/Extensions.cs:85-86`]).
**Internal row record must carry** (per UI-SPEC data contract): tx entity (date, name, categoryId, fromAccountId), natural signed fiat amount + original currency code, sats amount (native `ToSatAmount`/`FromSatAmount` when present — purchases/income/expenses have actual sats; otherwise converted at closest-date price), main-fiat converted amount (for fiat-denominated running totals).

### Anti-Patterns to Avoid
- **Parallel loader:** building a second transaction-selection pipeline guarantees drift (locked decision forbids it).
- **Recomputing selection in the App handler:** the handler must not know about `TransactionEntityType` predicates; it only loads the goal and delegates.
- **Live-price sats conversion:** sats must come from tx-date price lookups (closest-date, 7-day buffer), never `RatesState`/live prices — otherwise rows won't reconcile with stored progress.
- **Per-row N+1 name lookups:** join accounts/categories via dictionaries once (pattern at `TransactionQueries.cs:113-115`), not per-row repository calls.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Closest-date price lookup | Own date-search over price series | `GoalTransactionReader` machinery (`FindClosestDate` binary search [VERIFIED: `GoalTransactionReader.cs:257-270`], 7-day buffer load) | Correctness + consistency with progress math |
| Multi-currency conversion | Own conversion chain | `ConvertFiatToTarget` / `ConvertBtcToTarget` (`GoalTransactionReader.cs:182-222`) | Same USD-hop semantics the calculators use |
| Period range from goal | Duplicate month/year boundary logic a third time | Extract `GetPeriodRange` (`GoalQueries.cs:241-253`, currently private static, already duplicated at `GoalQueries.cs:51-57`) into a shared internal helper | Third copy invites divergence |
| Failure/result plumbing | Custom exception-based flow | `Result<T>` + `Error.NotFound` (`Result.cs`, `Error.cs:21-22`) | UI-SPEC contract; codebase convention |
| Goal entity → domain mapping | New mapper | `AsDomainObject()` (`Valt.Infra/Modules/Goals/Extensions.cs`) | Existing, tested |

**Key insight:** every piece of hard logic this phase needs (selection predicates, conversion, period math, failure plumbing) already exists in-repo; the phase is wiring + row exposure, not new algorithms.

## Common Pitfalls

### Pitfall 1: BitcoinHodl misclassified as non-transaction-based
**What goes wrong:** Query returns `NotSupported` for BitcoinHodl while its progress is plainly derived from `BitcoinToFiat` rows; the Phase 50 context menu hides a feature that could have worked.
**Why it happens:** CONTEXT.md recorded "BitcoinHodl from price" as an example of a non-transaction type without reading the calculator.
**How to avoid:** Resolve Q1 before planning; if the lock stands, document the exclusion rationale in the plan.
**Warning signs:** `BitcoinHodlProgressCalculator.cs:27-34` queries `_localDatabase.GetTransactions()` directly.

### Pitfall 2: Running-total unit ambiguity (sats / count / percent vs fiat)
**What goes wrong:** UI-SPEC annotates `RunningTotal` as "fiat, rounded via FiatValue / Math.Round(2)", but StackBitcoin/IncomeBtc accumulate sats, Dca accumulates a count, SavingsRate a percentage. Blindly applying `Math.Round(2)` to sats or counts is harmless; treating them as fiat in Phase 50 display is not.
**Why it happens:** The data contract was written before calculator internals were re-verified.
**How to avoid:** Keep `RunningTotal` as `decimal` but document per-strategy unit (see Q2); reconciliation tests assert against the goal type's calculated field (`CalculatedSats`, `CalculatedPurchaseCount`, `CalculatedPercentage`), not a fiat assumption.
**Warning signs:** A test asserting `RunningTotal == CalculatedSpending` for a Dca goal.

### Pitfall 3: Aggregate/row drift via copied predicates
**What goes wrong:** New row methods copy the `Where` clauses; a future fix updates one site.
**How to avoid:** Aggregates re-implemented as `Sum(row => row.Contribution)` over the new row methods (Pattern 3).
**Warning signs:** Two code sites mentioning `TransactionEntityType.FiatToBitcoin`.

### Pitfall 4: Same-day ordering instability
**What goes wrong:** Rows sorted by date only; LiteDB insert order or dictionary enumeration makes same-day rows non-deterministic between calculator and test runs.
**How to avoid:** Stable secondary sort key (e.g., `Id` or insertion index); assert ordering in tests with multi-transaction days.
**Warning signs:** Flaky ordering assertions.

### Pitfall 5: FiatAmount for sats-only transactions
**What goes wrong:** `Bitcoin`-type income/expense rows have no fiat leg (`FromFiatAmount` null); a required non-null `FiatAmount` forces invented values.
**How to avoid:** `FiatValue.Zero` (or nullable — but UI-SPEC says required) plus `FiatCurrencyCode` = main currency; flag in plan (Q3).

### Pitfall 6: Missing rate data → conversion returns 0
**What goes wrong:** `GetFiatRateAt`/`GetUsdBitcoinPriceAt` return `0m` when no rate exists (`GoalTransactionReader.cs:196,229,246`); unseeded price DB silently produces zeroed rows that still "reconcile" (both paths zero) but are wrong.
**How to avoid:** Tests seed `PriceDataBuilder.SeedRange` for the full goal period ± buffer [VERIFIED: `tests/Valt.Tests/Builders/PriceDataBuilder.cs:9-17`].

## Code Examples

### Calculator rows method (SpendingLimit — reader-backed)
```csharp
// Mirrors existing calculator [VERIFIED pattern: SpendingLimitProgressCalculator.cs:17-33]
public async Task<GoalContributingTransactionsResult?> GetContributingTransactionsAsync(GoalProgressInput input)
{
    var config = GoalTypeSerializer.DeserializeSpendingLimit(input.GoalTypeJson);
    var rows = _transactionReader.GetExpenseRows(input.From, input.To, categoryId: null); // new row-level method
    // service-side: sort asc, map names, accumulate RunningTotal = cumulative converted main-fiat spending
    ...
}
```

### Dictionary-join name resolution (existing pattern to reuse)
```csharp
// Source: src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs:113-115
categoryDict.TryGetValue(transactionEntity.CategoryId.ToString(), out var category);
accountDict.TryGetValue(transactionEntity.FromAccountId, out var fromAccount);
// CategoryName = category?.Name ?? string.Empty — UI-SPEC requires null (not empty) for missing category;
// map null explicitly at the DTO boundary.
```

### Handler skeleton (App)
```csharp
// Pattern: GetGoalHandler.cs:7-19 + Result<T> plumbing (Result.cs:30-31)
internal sealed class GetGoalContributingTransactionsHandler
    : IQueryHandler<GetGoalContributingTransactionsQuery, Result<GoalContributingTransactionsResult>>
{
    private readonly IGoalQueries _goalQueries;
    public async Task<Result<GoalContributingTransactionsResult>> HandleAsync(
        GetGoalContributingTransactionsQuery query, CancellationToken ct = default)
    {
        var goal = await _goalQueries.GetGoalAsync(query.GoalId);
        if (goal is null)
            return Result<GoalContributingTransactionsResult>.Failure(Error.NotFound("Goal", query.GoalId));
        return Result<GoalContributingTransactionsResult>.Success(
            await _goalQueries.GetContributingTransactionsAsync(query.GoalId));
    }
}
```

## Validation Architecture

`workflow.nyquist_validation` is `true` [VERIFIED: `.planning/config.json`] — this section applies.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit 4.x + NSubstitute 5.x (existing) |
| Config file | none — test project conventions (`tests/Valt.Tests`) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GOL-05 | Row carries date, description, account name, category name (null when uncategorized), signed fiat amount + currency, tx-date sats | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests.RowShape"` | ❌ Wave 0 |
| GOL-06 | Running total accumulates per strategy semantics; **final row == `CalculateProgressAsync` calculated value** | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~Reconciles"` | ❌ Wave 0 |
| GOL-07 | Set derived from strategy: contributing add/remove changes rows + final total; excluded types (transfers) never appear | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~AddRemove"` | ❌ Wave 0 |
| GOL-07 | `NotSupported` for NetWorthBtc; `GOAL_NOT_FOUND` for bad id; `Supported([])` for empty set | unit (DatabaseTest) | `dotnet test --filter "FullyQualifiedName~NotSupported"` | ❌ Wave 0 |

### Test Matrix per Goal Type (handler-level, `DatabaseTest` base)
Each strategy gets: **(R)** reconciliation, **(A)** add-responsiveness, **(X)** remove/exclusion, **(O)** ascending order.

| Strategy | Seed transactions (TransactionBuilder) | R: final RunningTotal equals | Notes |
|----------|----------------------------------------|------------------------------|-------|
| SpendingLimit | `.AsFiatExpense(acct, 300m)` + `.AsBitcoinPurchase(...)`; transfers must NOT appear | `SpendingLimitGoalType.CalculatedSpending` | Foreign-currency account variant: seed `PriceDataBuilder.SeedRange` + fiat rate |
| ReduceExpenseCategory | `.AsFiatExpense(acct, 200m).WithCategoryId(cat)` + expense in other category | `CalculatedSpending` | Category filter parity test |
| IncomeFiat | `.AsFiatIncome(acct, 500m)`; `BitcoinToFiat` must NOT appear | `CalculatedIncome` | — |
| SaveFiat | mixed income + expense rows | `CalculatedSavings` | Mixed-sign ordering test |
| SavingsRate | mixed income + expense | `CalculatedPercentage` | Running total is a percentage (Q2) |
| StackBitcoin | purchase + direct income + sale + direct expense | `CalculatedSats` | Net accumulation; sats native per row |
| IncomeBtc | `.AsBitcoinIncome(sats)` | `CalculatedSats` | — |
| Dca | N × `.AsBitcoinPurchase(...)` | `CalculatedPurchaseCount` | Running total is a count (Q2) |
| BitcoinHodl ⚠️ | `.AsBitcoinSale(sats)` | `CalculatedSoldSats` | Only if Q1 resolves to include; else assert `NotSupported` |
| NetWorthBtc | — | — | Assert `NotSupported` |

**Cross-cutting tests:** empty transaction set → `Supported` with empty list; missing goal id → `IsFailure` + `Error.Code == "GOAL_NOT_FOUND"`; sats conversion uses tx-date price (seed price range, change price mid-period, assert per-row sats track their own date).

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~GetGoalContributingTransactionsHandlerTests"`
- **Per wave merge:** `dotnet test`
- **Phase gate:** full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` — new; covers the matrix above
- [ ] Test support: construct real `GoalTransactionReader` via `new GoalTransactionReader(_localDatabase, _priceDatabase, new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>()))` (defaults to USD [VERIFIED: `CurrencySettings.cs:19-22`; construction pattern `BtcDenominatedMetricsQueriesTests.cs:308`])
- [ ] Test support: real `GoalProgressCalculatorFactory` over manually instantiated calculators (pattern precedent: `StackBitcoinProgressCalculatorTests.cs:18` constructs calculator directly against `_localDatabase`)
- [ ] Framework install: none — NUnit/NSubstitute already referenced

**Test seam caveat:** handler tests exercise App handler + real Infra service + real calculators over `DatabaseTest`'s in-memory LiteDB. `DatabaseTest` does not register `IGoalTransactionReader`/`CurrencySettings` [VERIFIED: `DatabaseTest.cs:70-91`]; tests construct them per fixture (established precedent in report-query tests).

## Security Domain

Read-only query over already-open local database; no new attack surface. ASVS spot-check:

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | No | Data is local single-user; no authorization layer exists app-wide |
| V5 Input Validation | Yes (minimal) | `GoalId` string parsed to `ObjectId` in Infra (existing `GetGoalAsync` pattern, `GoalQueries.cs:73`); malformed id returns null → `GOAL_NOT_FOUND` |
| V6 Cryptography | No | No crypto operations; password-protected LiteDB handled by existing infra |

Known threat patterns: none new. The query computes on demand with no cache (locked), so no stale-data/confusion risk across goals.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test | ✓ | 10.0.101 (targets net9.0) | — |
| LiteDB in-memory | `DatabaseTest` | ✓ | via existing test infra | — |
| External APIs (price feeds) | not needed for tests | n/a | — | `PriceDataBuilder` seeds fixed rates |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

## Runtime State Inventory

Step 2.5 SKIPPED — this is a greenfield backend capability (new query + extension methods), not a rename/refactor/migration. No stored strings, service registrations, OS state, secrets, or build artifacts are renamed or relocated. Existing stored goal JSON (`GoalTypeJson`) is read, never rewritten, by this phase.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Aggregate-only reader (`CalculateTotalExpenses`/`CalculateTotalIncome`) | Row-level methods with aggregates as row sums | this phase | Drift-proof by construction |
| Private duplicated `GetPeriodRange` | Shared internal helper | this phase (recommended) | Third duplication avoided |

**Deprecated/outdated:** none identified.

## Open Questions

1. **BitcoinHodl classification (BLOCKS test-matrix row)** ⚠️
   - What we know: `BitcoinHodlProgressCalculator.cs:27-34` loads transactions in range and sums `BitcoinToFiat && FromSatAmount < 0`. It is transaction-derived, like Dca/IncomeBtc. CONTEXT.md's example "BitcoinHodl from price" is factually wrong.
   - What's unclear: does the user want BitcoinHodl included as a 9th supported type (consistent with code and with the goal-transparency spirit of v0.9), or excluded per the locked decision (which was premised on the wrong belief)?
   - Recommendation: include BitcoinHodl (supported); only `NetWorthBtc` returns `NotSupported`. Requires a CONTEXT amendment at planning time — planner must surface to user, not silently decide.

2. **RunningTotal unit for sats/count/percentage strategies**
   - What we know: UI-SPEC fixes `RunningTotal` type as `decimal` but annotates it "fiat, rounded via FiatValue / Math.Round(2)". StackBitcoin/IncomeBtc/BitcoinHodl accumulate sats (integer-magnitude), Dca a purchase count, SavingsRate a percentage.
   - What's unclear: does Phase 50 need a unit discriminator, or is per-strategy unit documentation enough?
   - Recommendation: keep `decimal RunningTotal` + per-strategy unit documented on the DTO; reconciliation tests assert against the goal type's calculated field. Low risk; decide at planning.

3. **FiatAmount for sats-only rows**
   - What we know: `Bitcoin`-type income/expense rows have no fiat leg. UI-SPEC requires non-null `FiatAmount`.
   - Recommendation: `FiatValue.Zero` with `FiatCurrencyCode` = main currency for sats-only rows; Phase 50 renders fiat+sats columns and a zero fiat amount is truthful. Decide at planning.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | New Infra service can live inside `GoalQueries` or as its own class — planner's choice; both satisfy CQRS conventions | Architecture Patterns | Low — structural only |
| A2 | `GoalContributingTransactionsResult` union lives in `Valt.App/Modules/Goals/DTOs/` and Infra may reference it (precedent: Infra `GoalQueries` returns App DTOs) | Architecture Patterns | Low — verified precedent exists |
| A3 | Existing `TransactionDTO`/name-join machinery is reused as a pattern, not as a dependency (goals rows carry different fields) | Code Examples | Low |
| A4 | The query input is `GoalId` only; period range + type JSON are re-derived from the stored goal inside Infra (as `GetStaleGoalsAsync` does) | System Architecture | Medium — if UI wants an arbitrary date range instead, input contract changes |

**If Q1–Q3 resolve at planning:** no user confirmation remains outstanding.

## Sources

### Primary (HIGH confidence)
- All 10 calculators in `src/Valt.Infra/Modules/Goals/Services/` — read in-session (selection predicates quoted verbatim with line numbers)
- `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs` — full read; conversion machinery, buffer, closest-date algorithm
- `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs` — DTO mapping, `GetPeriodRange`, name-join precedent
- `src/Valt.App/Modules/Goals/` — `GetGoal` query/handler, `IGoalQueries`, `Result`/`Error` kernel types
- `src/Valt.Infra/Extensions.cs:285,322-333` — DI registration of queries, reader, all 10 calculators, factory
- `src/Valt.Core/Modules/Goals/GoalTypeNames.cs` — verbatim enum values (10 members, `StackBitcoin = 0` … `NetWorthBtc = 9`)
- `tests/Valt.Tests/DatabaseTest.cs`, `Builders/PriceDataBuilder.cs`, `Builders/GoalBuilder.cs`, `Builders/TransactionBuilder.cs`, `Services/Goals/StackBitcoinProgressCalculatorTests.cs` — test infrastructure and seeding patterns
- `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs:308` — `CurrencySettings` construction in tests

### Secondary (MEDIUM confidence)
- `.planning/config.json` — `nyquist_validation: true` (read in-session, HIGH for this repo)

### Tertiary (LOW confidence)
- None — all claims trace to in-repo source reads this session.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; every component read in-session
- Architecture: HIGH — extension mechanism validated against all 10 calculators verbatim; one locked-decision/code discrepancy flagged (Q1)
- Pitfalls: HIGH — each derived from a specific cited code behavior

**Research date:** 2026-10-06
**Valid until:** 2026-11-05 (stable in-repo domain; revisit only if goal calculators change)
