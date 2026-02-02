# Code Review Improvements Plan

**Created**: 2026-02-01
**Status**: In Progress
**Branch**: v0.2.7.x

---

## Executive Summary

Comprehensive code review identified **25+ issues** across performance, database indexes, and code quality. This document tracks the analysis findings and implementation progress.

---

## TODO List

### Phase 1: Quick Wins (Database & Queries)
- [ ] **1.1** Add `Visible` index to Accounts collection
- [ ] **1.2** Add `GroupId` index to Accounts collection
- [ ] **1.3** Add `GroupId` index to Transactions collection
- [ ] **1.4** Fix client-side filtering in AccountQueries.cs
- [ ] **1.5** Fix client-side filtering in AccountDisplayOrderManager.cs
- [ ] **1.6** Replace Count() with Any() in AutoSatAmountJob.cs

### Phase 2: Performance Critical
- [ ] **2.1** Convert linear searches to dictionary lookups in TransactionQueries.cs
- [ ] **2.2** Single-pass aggregation in MonthlyTotalsReport.cs
- [ ] **2.3** Pre-group transactions in AllTimeHighReport.cs
- [ ] **2.4** Fix repeated collection scans in MainViewModel.cs
- [ ] **2.5** Remove unnecessary Task.Run in ReportDataProvider.cs
- [ ] **2.6** Add rate lookup caching in GoalTransactionReader.cs

### Phase 3: Code Quality
- [ ] **3.1** Extract ValidatedName base class for 7 name value objects
- [ ] **3.2** Create GoalProgressCalculator base class with template method
- [ ] **3.3** Consolidate goal type serialization into single class
- [ ] **3.4** Standardize ObjectId creation patterns

---

## Detailed Findings

### Category 1: Performance Issues

#### HIGH SEVERITY

##### 1.1 TransactionQueries - Linear Searches
**File**: `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs`
**Lines**: 89-143
**Severity**: HIGH

**Problem**: Uses `.SingleOrDefault()` O(n) lookups in mapping loop instead of O(1) dictionary lookups.
```csharp
// Current (BAD) - O(n) per lookup
var category = allCategories.Items.SingleOrDefault(x => x.Id == transactionEntity.CategoryId.ToString())!;
var fromAccount = allAccountsList.SingleOrDefault(a => a.Id == transactionEntity.FromAccountId);
var toAccount = transactionEntity.ToAccountId is not null
    ? allAccountsList.SingleOrDefault(a => a.Id == transactionEntity.ToAccountId)
    : null;
```

**Impact**: With 1000 transactions × 3 lookups = 3000+ O(n) searches.

**Solution**:
```csharp
// Convert to dictionaries before loop - O(1) per lookup
var categoryDict = allCategories.Items.ToDictionary(x => x.Id);
var accountDict = allAccountsList.ToDictionary(a => a.Id);

var dtos = result.Select(transactionEntity =>
{
    var category = categoryDict[transactionEntity.CategoryId.ToString()];
    var fromAccount = accountDict.GetValueOrDefault(transactionEntity.FromAccountId);
    var toAccount = transactionEntity.ToAccountId is not null
        ? accountDict.GetValueOrDefault(transactionEntity.ToAccountId)
        : null;
    // ...
});
```

---

##### 1.2 MonthlyTotalsReport - Multiple LINQ Enumerations
**File**: `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`
**Lines**: 180-193, 209-217
**Severity**: HIGH

**Problem**: 8 separate iterations (4 `.Where()` + 4 `.Sum()`) over transaction collections.
```csharp
var income = fromTransactions.Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount > 0)
    .Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
var expense = fromTransactions.Where(x => x.Type == TransactionEntityType.Bitcoin && x.FromSatAmount < 0)
    .Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
var purchase = toTransactions.Where(x => x.Type == TransactionEntityType.FiatToBitcoin && x.ToSatAmount > 0)
    .Sum(x => x.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
var sale = fromTransactions.Where(x => x.Type == TransactionEntityType.BitcoinToFiat && x.FromSatAmount < 0)
    .Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
```

**Solution**: Single-pass aggregation:
```csharp
decimal income = 0, expense = 0, purchase = 0, sale = 0;
foreach (var tx in fromTransactions)
{
    var amount = tx.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;
    if (tx.Type == TransactionEntityType.Bitcoin)
    {
        if (tx.FromSatAmount > 0) income += amount;
        else expense += amount;
    }
    else if (tx.Type == TransactionEntityType.BitcoinToFiat && tx.FromSatAmount < 0)
        sale += amount;
}
foreach (var tx in toTransactions)
{
    if (tx.Type == TransactionEntityType.FiatToBitcoin && tx.ToSatAmount > 0)
        purchase += tx.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin;
}
```

---

##### 1.3 AllTimeHighReport - O(n×m×k) Complexity
**File**: `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs`
**Lines**: 83-99
**Severity**: HIGH

**Problem**: For each account on each date, transaction list is filtered and iterated multiple times.
```csharp
var fromAccount = transactionsOfDate.Where(x => x.FromAccountId == accountId);
var toAccount = transactionsOfDate.Where(x => x.ToAccountId == accountId);
var change = fromAccount.Sum(x => x.FromSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
change += toAccount.Sum(x => x.ToSatAmount.GetValueOrDefault() / SatoshisPerBitcoin);
```

**Solution**: Pre-group transactions by date and account before processing.

---

##### 1.4 MainViewModel - Repeated Collection Scans
**File**: `src/Valt.UI/Views/Main/MainViewModel.cs`
**Lines**: 632-656
**Severity**: HIGH

**Problem**: Multiple `.All()` and `.Any()` calls on Jobs collection on every property change.
```csharp
if (Jobs.All(x => x.State == BackgroundJobState.Ok)) { ... }
if (Jobs.All(x => x.State == BackgroundJobState.Error)) { ... }
if (Jobs.Any(x => x.State == BackgroundJobState.Running)) { ... }
if (Jobs.Any(x => x.State == BackgroundJobState.Ok)) { ... }
```

**Solution**: Single-pass state aggregation:
```csharp
bool allOk = true, allError = true, anyRunning = false, anyOk = false;
foreach (var job in Jobs)
{
    if (job.State != BackgroundJobState.Ok) allOk = false;
    if (job.State != BackgroundJobState.Error) allError = false;
    if (job.State == BackgroundJobState.Running) anyRunning = true;
    if (job.State == BackgroundJobState.Ok) anyOk = true;
}
```

---

#### MEDIUM SEVERITY

##### 1.5 GoalTransactionReader - Repeated Rate Lookups
**File**: `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs`
**Lines**: 231-268

**Problem**: Currency conversion lookups performed inline for each transaction.

**Solution**: Cache rate lookups by (date, currency) tuple.

---

##### 1.6 AccountCacheService - Missing Async Benefits
**File**: `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountCacheService.cs`
**Lines**: 19-65, 84-132

**Problem**: Methods marked `async Task` but do synchronous work without ConfigureAwait.

**Solution**: Add `ConfigureAwait(false)` or make truly synchronous.

---

##### 1.7 AutoSatAmountJob - Unnecessary Count() Query
**File**: `src/Valt.Infra/Modules/Budget/Transactions/Services/AutoSatAmountJob.cs`
**Lines**: 54-59

**Problem**:
```csharp
var btcRecordCount = _priceDatabase.GetBitcoinData().Query().Count();
if (btcRecordCount == 0) { ... }
```

**Solution**: Use `.Any()` for existence check:
```csharp
var hasBtcData = _priceDatabase.GetBitcoinData().Query().Exists();
```

---

##### 1.8 ReportDataProviderFactory - Unnecessary Task.Run
**File**: `src/Valt.Infra/Modules/Reports/ReportDataProvider.cs`
**Lines**: 47-53

**Problem**: Uses `Task.Run()` for I/O bound operations on synchronous LiteDB.

**Solution**: Remove Task.Run, execute synchronously or use proper async patterns.

---

##### 1.9 ExpensesByCategory Filter Checks
**File**: `src/Valt.Infra/Modules/Reports/ExpensesByCategory/ExpensesByCategoryReport.cs`
**Lines**: 91, 101

**Problem**: `.Any()` followed by `.Contains()` check in nested loops.

**Solution**: Convert filter lists to HashSets for O(1) lookup.

---

#### LOW SEVERITY

##### 1.10 MonthlyTotalsReport - Keys.ToList() in Loops
**File**: `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`
**Lines**: 311-329

**Problem**: `.Keys.ToList()` allocates new list for iteration.

**Solution**: Use `.Clear()` or iterate Keys directly.

---

##### 1.11 CategoryQueries - Inefficient ParentId Lookups
**File**: `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs`
**Lines**: 288-292

**Problem**: `.SingleOrDefault()` on category list for parent lookups.

**Solution**: Use dictionary for parent lookups.

---

### Category 2: Missing Database Indexes

#### 2.1 Accounts - Missing Visible Index
**File**: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
**Priority**: HIGH

**Query Locations**:
- `AccountDisplayOrderManager.cs:17, 79`
- `AccountQueries.cs:45`

**Solution**: Add to `GetAccounts()`:
```csharp
collection.EnsureIndex(x => x.Visible);
```

---

#### 2.2 Accounts - Missing GroupId Index
**File**: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
**Priority**: HIGH

**Query Location**: `AccountGroupRepository.cs:42`
```csharp
var accounts = _localDatabase.GetAccounts().Find(a => a.GroupId == groupIdBson).ToList();
```

**Solution**: Add to `GetAccounts()`:
```csharp
collection.EnsureIndex(x => x.GroupId);
```

---

#### 2.3 Transactions - Missing GroupId Index
**File**: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
**Priority**: HIGH

**Query Location**: `TransactionRepository.cs:110`

**Solution**: Add to `GetTransactions()`:
```csharp
collection.EnsureIndex(x => x.GroupId);
```

---

#### 2.4 Client-Side Filtering - AccountQueries
**File**: `src/Valt.Infra/Modules/Budget/Accounts/Queries/AccountQueries.cs`
**Line**: 45
**Priority**: HIGH

**Problem**:
```csharp
var accounts = _localDatabase.GetAccounts().FindAll().Where(account => account.Visible || showHiddenAccounts)
```

**Solution**:
```csharp
var accounts = showHiddenAccounts
    ? _localDatabase.GetAccounts().FindAll()
    : _localDatabase.GetAccounts().Find(x => x.Visible);
```

---

#### 2.5 Client-Side Filtering - AccountDisplayOrderManager
**File**: `src/Valt.Infra/Modules/Budget/Accounts/AccountDisplayOrderManager.cs`
**Lines**: 17, 29, 79
**Priority**: MEDIUM

**Problem**: Filters all accounts in memory instead of querying database.

**Solution**: Query database with combined filter:
```csharp
var groupAccounts = _localDatabase.GetAccounts()
    .Find(a => a.GroupId == groupId && a.Visible)
    .OrderBy(a => a.DisplayOrder)
    .ToList();
```

---

#### 2.6 Composite Indexes (Optional)
**File**: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
**Priority**: MEDIUM

**Recommendation**: For high-frequency account range queries in AccountCacheService:
```csharp
collection.EnsureIndex(x => new { x.FromAccountId, x.Date });
collection.EnsureIndex(x => new { x.ToAccountId, x.Date });
```

---

### Category 3: Code Duplication

#### 3.1 Name Value Objects (7 classes)
**Files**:
- `src/Valt.Core/Modules/Budget/Accounts/AccountName.cs`
- `src/Valt.Core/Modules/Budget/Transactions/TransactionName.cs`
- `src/Valt.Core/Modules/Budget/Categories/CategoryName.cs`
- `src/Valt.Core/Modules/Budget/FixedExpenses/FixedExpenseName.cs`
- `src/Valt.Core/Modules/AvgPrice/AvgPriceProfileName.cs`
- `src/Valt.Core/Modules/Budget/Accounts/AccountGroupName.cs`
- `src/Valt.Core/Modules/Budget/Accounts/AccountCurrencyNickname.cs`

**Problem**: Identical validation logic repeated 7 times:
```csharp
if (string.IsNullOrWhiteSpace(value))
    throw new Empty*Exception();
if (value.Length > MAX_LENGTH)
    throw new MaximumFieldLengthException(...);
```

**Solution**: Extract base record with validation:
```csharp
public abstract record ValidatedName
{
    protected abstract int MaxLength { get; }
    protected abstract Exception CreateEmptyException();

    protected void ValidateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw CreateEmptyException();
        if (value.Length > MaxLength)
            throw new MaximumFieldLengthException(MaxLength, value.Length);
    }
}
```

---

#### 3.2 Goal Progress Calculators (7 classes)
**Files**: `src/Valt.Infra/Modules/Goals/Services/*ProgressCalculator.cs`
- DcaProgressCalculator.cs
- StackBitcoinProgressCalculator.cs
- IncomeBtcProgressCalculator.cs
- BitcoinHodlProgressCalculator.cs
- IncomeFiatProgressCalculator.cs
- SpendingLimitProgressCalculator.cs
- ReduceExpenseCategoryProgressCalculator.cs

**Problem**: Template pattern repeated 7 times with only calculation logic differing.

**Solution**: Extract base class with template method:
```csharp
public abstract class BaseGoalProgressCalculator<TDto, TConfig> : IGoalProgressCalculator
    where TConfig : IGoalType
{
    public abstract GoalTypeNames SupportedType { get; }

    public async Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var dto = JsonSerializer.Deserialize<TDto>(input.GoalTypeJson)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TDto).Name}");
        var config = CreateConfig(dto);
        var progress = await CalculateProgressCore(input, config);
        var updated = UpdateConfig(config, progress);
        return new GoalProgressResult(progress, updated);
    }

    protected abstract TConfig CreateConfig(TDto dto);
    protected abstract Task<decimal> CalculateProgressCore(GoalProgressInput input, TConfig config);
    protected abstract IGoalType UpdateConfig(TConfig config, decimal progress);
}
```

---

#### 3.3 Goal Type Serialization Duplication
**Files**:
- `src/Valt.Infra/Modules/Goals/Extensions.cs`
- `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs`

**Problem**: Identical deserialization code for 7 goal types appears in two files.

**Solution**: Create `GoalTypeSerializer` class:
```csharp
public static class GoalTypeSerializer
{
    public static IGoalType Deserialize(GoalTypeNames type, string json) => type switch
    {
        GoalTypeNames.StackBitcoin => DeserializeAs<StackBitcoinGoalTypeDto, StackBitcoinGoalType>(json,
            dto => new StackBitcoinGoalType(dto.TargetSats, dto.CalculatedSats)),
        // ... other types
    };

    private static T DeserializeAs<TDto, T>(string json, Func<TDto, T> factory) =>
        factory(JsonSerializer.Deserialize<TDto>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TDto).Name}"));
}
```

---

#### 3.4 ObjectId Creation Inconsistency
**Locations**: 82 occurrences across infrastructure

**Problem**: Same operation done differently:
```csharp
new ObjectId(accountId.Value)        // Using .Value
new ObjectId(goal.Id.ToString())     // Using ToString()
accountId.AsObjectId()               // Extension method (rarely used)
```

**Solution**: Standardize on `.AsObjectId()` extension method across codebase.

---

#### 3.5 Entity-to-Domain Mapping Duplication
**Files**:
- `src/Valt.Infra/Modules/Budget/Accounts/Extensions.cs`
- `src/Valt.Infra/Modules/Budget/Categories/Extensions.cs`
- `src/Valt.Infra/Modules/Budget/Transactions/Extensions.cs`
- `src/Valt.Infra/Modules/Budget/FixedExpenses/Extensions.cs`

**Problem**: Identical try-catch wrapper in every AsDomainObject method.

**Solution**: Create helper method:
```csharp
public static class ConversionHelper
{
    public static T SafeConvert<TEntity, T>(TEntity entity, Func<TEntity, T> convert, string entityName)
    {
        try { return convert(entity); }
        catch (Exception ex) { throw new BrokenConversionFromDbException(entityName, entity.ToString(), ex); }
    }
}
```

---

## Work Log

| Date | Task | Status | Notes |
|------|------|--------|-------|
| 2026-02-01 | Initial analysis | ✅ Complete | Identified 25+ issues |
| | | | |

---

## References

### Files to Modify

**Phase 1 (Indexes & Queries)**:
- `src/Valt.Infra/DataAccess/LocalDatabase.cs`
- `src/Valt.Infra/Modules/Budget/Accounts/Queries/AccountQueries.cs`
- `src/Valt.Infra/Modules/Budget/Accounts/AccountDisplayOrderManager.cs`
- `src/Valt.Infra/Modules/Budget/Transactions/Services/AutoSatAmountJob.cs`

**Phase 2 (Performance)**:
- `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs`
- `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs`
- `src/Valt.UI/Views/Main/MainViewModel.cs`
- `src/Valt.Infra/Modules/Reports/ReportDataProvider.cs`
- `src/Valt.Infra/Modules/Goals/Services/GoalTransactionReader.cs`

**Phase 3 (Code Quality)**:
- `src/Valt.Core/Modules/Budget/Accounts/AccountName.cs` (and 6 other name classes)
- `src/Valt.Infra/Modules/Goals/Services/*ProgressCalculator.cs` (7 files)
- `src/Valt.Infra/Modules/Goals/Extensions.cs`
- `src/Valt.Infra/Modules/Goals/Queries/GoalQueries.cs`
