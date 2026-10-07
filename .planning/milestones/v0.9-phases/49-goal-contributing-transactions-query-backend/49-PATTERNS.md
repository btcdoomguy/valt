# Phase 49: Goal Contributing-Transactions Query Backend - Pattern Map

**Mapped:** 2026-10-06
**Files analyzed:** 13
**Analogs found:** 13 / 13

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsQuery.cs` (new) | query (CQRS) | request-response | `src/Valt.App/Modules/Goals/Queries/GetGoal/GetGoalQuery.cs` | exact |
| `src/Valt.App/Modules/Goals/Queries/GetGoalContributingTransactions/GetGoalContributingTransactionsHandler.cs` (new) | handler (CQRS) | request-response | `src/Valt.App/Modules/Goals/Queries/GetGoal/GetGoalHandler.cs` | exact |
| `src/Valt.App/Modules/Goals/DTOs/ContributingTransactionRow.cs` (new) | model/DTO | transform | `src/Valt.App/Modules/Goals/DTOs/GoalDTO.cs` | role-match |
| `src/Valt.App/Modules/Goals/DTOs/GoalContributingTransactionsResult.cs` (new) | model/DTO (union) | transform | `src/Valt.App/Modules/Goals/DTOs/GoalDTO.cs` (`GoalTypeOutputDTO` abstract record pattern) | role-match |
| `src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs` (modified) | contract | request-response | file itself — extend per existing style | exact |
| `src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs` (modified) | contract | request-response | file itself — add default interface method | exact |
| `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs` (modified) | service | transform (row-level) | file itself — add row methods beside aggregates | exact |
| 9 transaction-based calculators in `src/Valt.Infra/Modules/Goals/Services/` (modified) | service | transform | `SpendingLimitProgressCalculator.cs` (reader-backed), `StackBitcoinProgressCalculator.cs` (direct DB) | exact |
| `src/Valt.Infra/Modules/Goals/Services/GoalContributingTransactionsService.cs` (new) | service | orchestration / request-response | `GoalQueries.cs` + `GoalProgressCalculatorFactory.cs` composite | role-match |
| `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs` (modified) | query impl | request-response | file itself — extract `GetPeriodRange` into shared internal helper | exact |
| `src/Valt.Infra/Extensions.cs` (modified) | config (DI) | registration | `src/Valt.Infra/Extensions.cs:322-333` existing goals block | exact |
| `tests/Valt.Tests/Application/Goals/Queries/GetGoalContributingTransactionsHandlerTests.cs` (new) | test | verify | `GetGoalHandlerTests.cs` (handler over `DatabaseTest`) + `StackBitcoinProgressCalculatorTests.cs` (real calculators + builders) | exact |
| `tests/Valt.Tests/Services/Goals/*ProgressCalculatorTests.cs` (modified if mocks of `IGoalTransactionReader` need row-method stubs) | test | verify | `SpendingLimitProgressCalculatorTests.cs` (NSubstitute mock pattern) | exact |

## Pattern Assignments

### `GetGoalContributingTransactionsQuery.cs` (new, query)

**Analog:** `src/Valt.App/Modules/Goals/Queries/GetGoal/GetGoalQuery.cs` (full file, 15 lines)

**Core pattern:**
```csharp
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoal;

/// <summary>
/// Query to get a single goal by ID.
/// </summary>
public record GetGoalQuery : IQuery<GoalDTO?>
{
    /// <summary>
    /// The ID of the goal to retrieve.
    /// </summary>
    public required string GoalId { get; init; }
}
```
Conventions: public sealed record, `IQuery<T>` from `Valt.App.Kernel.Queries`, `required` init-only properties, XML doc comments. New query returns `Result<GoalContributingTransactionsResult>` (union DTO below), input is `GoalId` only (per Assumption A4 — period range re-derived in Infra).

---

### `GetGoalContributingTransactionsHandler.cs` (new, handler)

**Analogs:** `src/Valt.App/Modules/Goals/Queries/GetGoal/GetGoalHandler.cs` (lines 1-20) + `src/Valt.App/Kernel/Result.cs` (lines 20-31)

**Handler skeleton:**
```csharp
internal sealed class GetGoalHandler : IQueryHandler<GetGoalQuery, GoalDTO?>
{
    private readonly IGoalQueries _goalQueries;

    public GetGoalHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public Task<GoalDTO?> HandleAsync(GetGoalQuery query, CancellationToken ct = default)
    {
        return _goalQueries.GetGoalAsync(query.GoalId);
    }
}
```
Conventions: `internal sealed class`, single `IGoalQueries` dependency injected via ctor, `HandleAsync(query, CancellationToken ct = default)`, auto-registered via `AddApplication()` scanning — no manual registration.

**Failure plumbing for GOAL_NOT_FOUND** (`Result.cs:30-31` + `Error.cs:21-22`):
```csharp
return Result<GoalContributingTransactionsResult>.Failure(Error.NotFound("Goal", query.GoalId));
// Error.NotFound yields Code "GOAL_NOT_FOUND" verbatim
```
Handler shape (from RESEARCH handler skeleton): check `await _goalQueries.GetGoalAsync(query.GoalId) is null` → Failure; else `Result<T>.Success(await _goalQueries.GetContributingTransactionsAsync(query.GoalId))`. Handler stays a thin pass-through — no `TransactionEntityType` knowledge (anti-pattern in RESEARCH).

---

### `ContributingTransactionRow.cs` + `GoalContributingTransactionsResult.cs` (new, DTOs)

**Analog:** `src/Valt.App/Modules/Goals/DTOs/GoalDTO.cs` (full file, 171 lines)

**DTO conventions** (`GoalDTO.cs:6-17`):
```csharp
public record GoalDTO
{
    public required string Id { get; init; }
    public required DateOnly RefDate { get; init; }
    // ... all required init-only properties, XML doc comments on type
}
```

**Abstract-record union pattern** (`GoalDTO.cs:23-39` — reuse for `GoalContributingTransactionsResult`):
```csharp
public abstract record GoalTypeOutputDTO
{
    public abstract int TypeId { get; }
    ...
}
```
Apply the same closed-union shape from RESEARCH Pattern 1:
```csharp
public abstract record GoalContributingTransactionsResult
{
    public sealed record Supported(IReadOnlyList<ContributingTransactionRow> Rows)
        : GoalContributingTransactionsResult;
    public sealed record NotSupported(GoalTypeNames Type)
        : GoalContributingTransactionsResult;
}
```
`ContributingTransactionRow` fields per GOL-05: date, description, account name, category name (null when uncategorized — map null explicitly at DTO boundary, per RESEARCH), signed fiat amount + currency code, sats amount, running total (`decimal`; per-strategy unit documented — Q2).

---

### `IGoalQueries.cs` (modified, App contract)

**Analog:** file itself (`src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs`, full file, 10 lines)

```csharp
public interface IGoalQueries
{
    Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync();
    Task<IReadOnlyList<GoalDTO>> GetGoalsAsync(DateOnly? filterDate);
    Task<GoalDTO?> GetGoalAsync(string goalId);
}
```
Add: `Task<GoalContributingTransactionsResult> GetContributingTransactionsAsync(string goalId);` — returning the App union DTO directly (precedent: this interface already returns App DTOs from Infra; Infra references App per research). No `Result<>` wrapper here — the handler wraps.

---

### `IGoalProgressCalculator.cs` (modified, Infra contract — default method)

**Analog:** file itself (`src/Valt.Infra/Modules/Goals/Services/IGoalProgressCalculator.cs`, full file, 12 lines)

```csharp
public record GoalProgressResult(decimal Progress, IGoalType UpdatedGoalType);

public interface IGoalProgressCalculator
{
    GoalTypeNames SupportedType { get; }
    Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input);
}
```
Extension per RESEARCH Pattern 2 (default interface method — only `NetWorthBtc` keeps the default):
```csharp
Task<GoalContributingTransactionsResult?> GetContributingTransactionsAsync(GoalProgressInput input)
    => Task.FromResult<GoalContributingTransactionsResult?>(
        new GoalContributingTransactionsResult.NotSupported(input.TypeName));
```

---

### `GoalTransactionReader.cs` (modified — add row-level methods, aggregates as row sums)

**Analog:** file itself (full read, 281 lines). Key sections:

**Aggregate loop to convert to rows** (`GoalTransactionReader.cs:53-100`) — the expense predicate block that must become the row method:
```csharp
// Handle fiat expenses (negative FromFiatAmount on Fiat type only)
if (tx.Type == TransactionEntityType.Fiat && tx.FromFiatAmount < 0)
{
    var amount = ConvertFiatTransactionAmount(tx, context, txDate, useFromAccount: true, absoluteValue: true);
    totalExpenses += amount;
}
// Handle Bitcoin expenses (negative FromSatAmount on Bitcoin type only)
else if (tx.Type == TransactionEntityType.Bitcoin && tx.FromSatAmount < 0)
{
    var btcAmount = Math.Abs(tx.FromSatAmount ?? 0) / SatoshisPerBitcoin;
    totalExpenses += ConvertBtcToTarget(btcAmount, context, txDate);
}
// NOTE: FiatToBitcoin and BitcoinToFiat are transfers, not expenses
```

**Data context loading** (`GoalTransactionReader.cs:124-180`): `LoadDataContext` already loads accounts dictionary, BTC rates + sorted dates, fiat rates by currency + sorted dates, transactions (with optional category filter), 7-day buffer at lines 138-140. Row methods must reuse this same context — no parallel loader.

**Conversion machinery** (private, reusable as-is):
- `ConvertFiatTransactionAmount` (lines 102-122) — from-account vs to-account, absolute value flag
- `ConvertFiatToTarget` (lines 182-209) — USD-hop semantics, returns 0 when rate missing (Pitfall 6)
- `ConvertBtcToTarget` (lines 211-222) — sats → main fiat at tx-date price
- `GetFiatRateAt` / `GetUsdBitcoinPriceAt` (lines 224-255), `FindClosestDate` binary search (lines 257-270), `SatoshisPerBitcoin` const (line 36)

**Locked refactor (RESEARCH Pattern 3):** new internal row record + `GetExpenseRows(from, to, categoryId)` / `GetIncomeRows(from, to)` on `IGoalTransactionReader`; re-implement `CalculateTotalExpenses`/`CalculateTotalIncome` as `Sum` over rows so drift is impossible. Internal row record carries: tx entity (date, name, categoryId, fromAccountId), natural signed fiat amount + original currency code, native sats (`ToSatAmount`/`FromSatAmount`) or converted at closest-date price, main-fiat converted amount.

---

### 9 Calculators (modified — add `GetContributingTransactionsAsync`)

**Analog A (reader-backed):** `src/Valt.Infra/Modules/Goals/Services/SpendingLimitProgressCalculator.cs` (full file, 34 lines)
```csharp
internal class SpendingLimitProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;
    public GoalTypeNames SupportedType => GoalTypeNames.SpendingLimit;

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var config = GoalTypeSerializer.DeserializeSpendingLimit(input.GoalTypeJson);
        var totalSpending = _transactionReader.CalculateTotalExpenses(input.From, input.To);
        ...
        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
```
**Analog B (direct-DB, multi-bucket):** `src/Valt.Infra/Modules/Goals/Services/StackBitcoinProgressCalculator.cs` (full file, 65 lines) — the four-bucket selection pattern (lines 33-50):
```csharp
var btcPurchased = transactions
    .Where(x => x.Type == TransactionEntityType.FiatToBitcoin && x.ToSatAmount > 0)
    .Sum(x => x.ToSatAmount ?? 0);
```
The rows method must expose the same buckets as signed rows (purchase +, direct income +, sale −, direct expense −), cumulative net sats as running total.

**Analog C (category filter):** `ReduceExpenseCategoryProgressCalculator.cs:22-25` — `new ObjectId(config.CategoryId)` passed as reader's `categoryId` argument.

Per-strategy rows methods mirror each calculator's existing `CalculateProgressAsync` structure exactly (deserialize config → select → accumulate); only `NetWorthBtc` keeps the interface default. `BitcoinHodl` implements it (`BitcoinToFiat && FromSatAmount < 0`, per Q1 resolution).

---

### `GoalContributingTransactionsService.cs` (new, Infra orchestration)

**Analogs:** `GoalQueries.cs` (goal load + period range, lines 71-79 and 241-253) + `GoalProgressCalculatorFactory.cs` (full file, 19 lines) + `GoalQueries.cs:109-117` (dictionary-join name resolution)

**Goal load + ObjectId parse** (`GoalQueries.cs:71-79`):
```csharp
public Task<GoalDTO?> GetGoalAsync(string goalId)
{
    var entity = _localDatabase.GetGoals().FindById(new ObjectId(goalId));
    if (entity is null)
        return Task.FromResult<GoalDTO?>(null);
    return Task.FromResult<GoalDTO?>(MapToDto(entity));
}
```
(Research V5 note: malformed `ObjectId` — wrap `new ObjectId(goalId)` defensively or rely on LiteDB behavior; malformed id must yield null → `GOAL_NOT_FOUND`.)

**Period range** (`GoalQueries.cs:241-253`, duplicated at 28 and 56 — extract to shared internal helper per RESEARCH "Don't Hand-Roll"):
```csharp
private static (DateOnly From, DateOnly To) GetPeriodRange(DateOnly refDate, GoalPeriods period, DateOnly? startDate = null)
{
    return period switch
    {
        GoalPeriods.Monthly => (new DateOnly(refDate.Year, refDate.Month, 1),
            new DateOnly(refDate.Year, refDate.Month, DateTime.DaysInMonth(refDate.Year, refDate.Month))),
        GoalPeriods.Yearly => (startDate ?? new DateOnly(refDate.Year, 1, 1),
            new DateOnly(refDate.Year, 12, 31)),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };
}
```

**Calculator resolution** (`GoalProgressCalculatorFactory.cs:14-18`): `GetCalculator(GoalTypeNames)` throws `NotSupportedException` when unregistered — service resolves via factory after casting entity's `GoalTypeNameId`.

**Name dictionary-join** (`TransactionQueries.cs:113-117`) — resolve account/category names once, not per-row:
```csharp
categoryDict.TryGetValue(transactionEntity.CategoryId.ToString(), out var category);
accountDict.TryGetValue(transactionEntity.FromAccountId, out var fromAccount);
var toAccount = transactionEntity.ToAccountId is not null && accountDict.TryGetValue(transactionEntity.ToAccountId, out var ta)
    ? ta
    : null;
```
(UI-SPEC requires `CategoryName == null` for uncategorized — map null explicitly at DTO boundary instead of `?? string.Empty`.)

Service shape: load goal entity → `AsDomainObject()` mapping exists (`Valt.Infra/Modules/Goals/Extensions.cs` — RESEARCH "Don't Hand-Roll") → build `GoalProgressInput(typeName, goalTypeJson, from, to)` → `factory.GetCalculator(type).GetContributingTransactionsAsync(input)` → map rows → App DTO with name dictionaries + chronological sort (stable secondary key, Pitfall 4) + running total accumulation. Alternative approved by A1: fold into `GoalQueries` instead of a new class.

---

### `src/Valt.Infra/Extensions.cs` (modified — DI registration)

**Analog:** `src/Valt.Infra/Extensions.cs:286,322-333`
```csharp
services.AddSingleton<IGoalQueries, GoalQueries>();
...
services.AddSingleton<IGoalTransactionReader, GoalTransactionReader>();
services.AddSingleton<IGoalProgressCalculator, StackBitcoinProgressCalculator>();
// ... all 10 calculators ...
services.AddSingleton<IGoalProgressCalculatorFactory, GoalProgressCalculatorFactory>();
```
If a new `GoalContributingTransactionsService` class is created: `services.AddSingleton<IGoalContributingTransactions, GoalContributingTransactionsService>();` (name at planner's discretion). If folded into `GoalQueries`, no new registration needed.

---

### `GetGoalContributingTransactionsHandlerTests.cs` (new, test)

**Analog A (handler-over-DatabaseTest pattern):** `tests/Valt.Tests/Application/Goals/Queries/GetGoalHandlerTests.cs` (full file, 105 lines)
```csharp
[TestFixture]
public class GetGoalHandlerTests : DatabaseTest
{
    private GetGoalHandler _handler = null!;

    [SetUp]
    public async Task SetUpHandler()
    {
        var existingGoals = await _goalRepository.GetAllAsync();
        foreach (var goal in existingGoals)
            await _goalRepository.DeleteAsync(goal);

        _handler = new GetGoalHandler(_goalQueries);
    }
```
Conventions: `[TestFixture]`, extends `DatabaseTest`, cleanup of goals in `[SetUp]`, handler constructed directly with the real Infra `_goalQueries` (available as protected field per `DatabaseTest.cs:62,89`).

**Analog B (real calculator + builder seeding):** `tests/Valt.Tests/Services/Goals/StackBitcoinProgressCalculatorTests.cs`
- Direct construction: `new StackBitcoinProgressCalculator(_localDatabase)` (line 18)
- `GoalProgressInput` construction (lines 37-41)
- Builder seeding + insert (lines 386-444):
```csharp
var entity = TransactionBuilder.ATransaction()
    .WithDate(date)
    .WithName("BTC Purchase")
    .AsBitcoinPurchase(satAmount: satAmount, fiatAmount: 100m)
    .Build();
_localDatabase.GetTransactions().Insert(entity);
```
- Assertion style: `Assert.That(result.Progress, Is.EqualTo(50m));` with `Assert.Multiple` for multi-assert tests.

**Test support construction** (RESEARCH Wave 0):
```csharp
var reader = new GoalTransactionReader(_localDatabase, _priceDatabase,
    new CurrencySettings(_localDatabase, Substitute.For<INotificationPublisher>()));
```
`CurrencySettings` defaults to USD. `DatabaseTest` does NOT register `IGoalTransactionReader`/`CurrencySettings` (`DatabaseTest.cs:70-91`) — construct per fixture (established precedent).

**Price seeding** (`tests/Valt.Tests/Builders/PriceDataBuilder.cs:9-17`):
```csharp
PriceDataBuilder.SeedRange(_priceDatabase, fromDate, toDate, btcPriceUsd: 50_000m,
    ("BRL", 5.0m)); // seed full goal period ± 7-day buffer (Pitfall 6)
```

**Goal builders** (`tests/Valt.Tests/Builders/GoalBuilder.cs:89-125`): `AGoal()`, `AStackBitcoinGoal(targetSats)`, `AMonthlyGoal()`, `AYearlyGoal()`, `ASpendingLimitGoal(targetAmount)`, `ABitcoinHodlGoal(maxSellableSats)`, `AReduceExpenseCategoryGoal(targetAmount, categoryId, categoryName)`, `ASaveFiatGoal(targetAmount)`, `ASavingsRateGoal(targetPercentage)`, `ANetWorthBtcGoal(targetSats)` — note no `ADcaGoal`/`AIncomeFiatGoal`/`AIncomeBtcGoal` factories exist; use `AGoal()` + fluent setters or add factories if needed.

**IdGenerator setup:** `DatabaseTest` ctor already calls `IdGenerator.Configure(new LiteDbIdProvider())` (line 67) — no `[OneTimeSetUp]` needed.

---

### Mocked-reader calculator tests (possibly modified)

**Analog:** `tests/Valt.Tests/Services/Goals/SpendingLimitProgressCalculatorTests.cs:45`
```csharp
_transactionReader.CalculateTotalExpenses(from, to, null).Returns(500m);
```
NSubstitute mocks `IGoalTransactionReader`. If row methods are added to the interface, NSubstitute auto-returns default (`null`/empty) for unstubbed members — existing tests keep passing (per RESEARCH Pattern 3 caveat + `InternalsVisibleTo("DynamicProxyGenAssembly2")` at `Valt.Infra/Extensions.cs:85-86`). If new tests mock row methods, follow the same `.Returns(...)` pattern.

## Shared Patterns

### CQRS Query/Handler Shape (all App-layer files)
**Source:** `GetGoalQuery.cs` + `GetGoalHandler.cs`
**Apply to:** Query, Handler, handler tests
Public record query with `required` init props + `IQuery<T>`; `internal sealed` handler with single contract dependency; `HandleAsync(query, ct = default)`; auto-registration via `AddApplication()` — never manually registered.

### Result / Error Railway
**Source:** `src/Valt.App/Kernel/Result.cs:20-31`, `src/Valt.App/Kernel/Error.cs:21-22`
**Apply to:** Handler, service
```csharp
Result<T>.Success(value) / Result<T>.Failure(Error.NotFound("Goal", id))  // Code: "GOAL_NOT_FOUND"
```
`Result<T>.NotFound(entityType, id)` static helper also available (line 30-31).

### Infra-implements-App-contract (dependency direction)
**Source:** `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs:10` (`internal class GoalQueries : IGoalQueries`), `src/Valt.App/Modules/Goals/Contracts/IGoalQueries.cs`
**Apply to:** New service/contract method
Infra references App DTOs and returns them across the contract boundary; internals visible to tests via `InternalsVisibleTo("Valt.Tests")`.

### Selection predicates live beside progress math
**Source:** every calculator's `CalculateProgressAsync`
**Apply to:** rows methods in all 9 calculators
Verbatim `Where` predicates from the progress method reused in the rows method (never copied to a second location — RESEARCH Pitfall 3). Transfers (`FiatToBitcoin`, `BitcoinToFiat` in reader-backed strategies) never appear in expense/income rows.

### Testing base
**Source:** `tests/Valt.Tests/DatabaseTest.cs`
**Apply to:** all new tests
In-memory LiteDB (`_localDatabase`, `_priceDatabase`), repositories/queries pre-constructed as protected fields, per-test `RefreshLocalInstances()`, builders for all seed data, NUnit 4 + NSubstitute 5.

## No Analog Found

None — every file has a close analog in the codebase. The only novel shape is the `Supported`/`NotSupported` union DTO, which composes two existing patterns: abstract record union (`GoalTypeOutputDTO`) and `Result<T>` wrapping.

## Metadata

**Analog search scope:** `src/Valt.App/Modules/Goals/`, `src/Valt.Infra/Modules/Goals/`, `src/Valt.Infra/Modules/Budget/Transactions/Queries/`, `src/Valt.Infra/Extensions.cs`, `src/Valt.App/Kernel/`, `tests/Valt.Tests/` (Application/Goals/Queries, Services/Goals, Builders, DatabaseTest)
**Files scanned:** 18
**Pattern extraction date:** 2026-10-06
