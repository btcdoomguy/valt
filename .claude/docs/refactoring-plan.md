# Valt Refactoring & Performance Improvement Plan

**Created:** 2026-02-07
**Branch:** v0.2.7.4
**Last Updated:** 2026-02-07

## Priority 1: Immediate (Critical)

### 1.1 Fix UI Thread Blocking in LiveRateState
- **File:** `src/Valt.UI/State/LiveRateState.cs:146-147`
- **Issue:** `.GetAwaiter().GetResult()` blocks UI thread inside `WeakReferenceMessenger.Receive`
- **Fix:** Converted `RefreshLastPrice()` to `async Task RefreshLastPriceAsync()`, replaced `.GetAwaiter().GetResult()` with `await`, caller uses fire-and-forget `_ = RefreshLastPriceAsync()`
- **Status:** DONE

### 1.2 Fix O(n²) Lookups in Query Classes
- **Files:**
  - `src/Valt.Infra/Modules/Budget/FixedExpenses/Queries/FixedExpenseQueries.cs:36-38, 84-85`
  - Other query classes with `SingleOrDefault` inside LINQ loops
- **Issue:** `SingleOrDefault` called per iteration = O(n²)
- **Fix:** Replaced with `ToDictionary()` + `TryGetValue()` for O(1) lookups in both `GetFixedExpensesAsync` and `GetFixedExpenseHistoryAsync`
- **Status:** DONE

### 1.3 Fix AvgPriceProfileName Length Mismatch Bug
- **File:** `src/Valt.Core/Modules/AvgPrice/AvgPriceProfileName.cs:20-21`
- **Issue:** Validates against 30 but error message says 20
- **Fix:** Changed error message constant from 20 to 30 to match actual validation
- **Status:** DONE

### 1.4 Fix Fire-and-Forget Without Error Handling
- **File:** `src/Valt.UI/State/AccountsTotalState.cs:56, 61`
- **Issue:** `ContinueWith` without `TaskScheduler` or error handling
- **Fix:** Replaced `ContinueWith` with proper `async Task RefreshAndNotifyAsync()` method with try-catch logging
- **Status:** DONE

### 1.5 Fix SolidColorBrush Allocation Churn
- **File:** `src/Valt.UI/Views/Main/Controls/LiveRatesViewModel.cs:74-142`
- **Issue:** New `SolidColorBrush` created on every property access
- **Fix:** Added `static readonly` brush instances (NeutralBrush, PositiveBrush, NegativeBrush) and reused in all 3 brush properties
- **Status:** DONE

## Priority 2: Short-term (High Priority)

### 2.1 Replace Count() with Exists() for Empty Checks
- **File:** `src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs:72, 143, 180`
- **Issue:** `.Query().Count() == 0` forces full collection scan
- **Fix:** Replaced 3 occurrences of `.Query().Count()` with `.Exists(x => true)` for short-circuit evaluation
- **Status:** DONE

### 2.2 Fix GoalProgressUpdaterJob Interval
- **File:** `src/Valt.Infra/Modules/Goals/Services/GoalProgressUpdaterJob.cs:30`
- **Issue:** Runs every 1 second, mostly does nothing
- **Fix:** Changed interval from `TimeSpan.FromSeconds(1)` to `TimeSpan.FromSeconds(5)`
- **Status:** DONE

### 2.3 Fix Full Collection Loads for Aggregates
- **File:** `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs:195-205`
- **Issue:** `FindAll().ToList()` to compute `Min(x => x.Date)`
- **Fix:** Replaced `FindAll().ToList()` with `Exists()` + `.Min()`/`.Max()` directly on collections in both `GetRequiredStartDate` and `GetFiatDateRange`
- **Status:** DONE

### 2.4 Fix Missing Child ViewModel Disposal
- **File:** `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs:676-691`
- **Issue:** `_transactionListViewModel` never disposed
- **Fix:** Added `_transactionListViewModel?.Dispose()` call in parent `Dispose()` method
- **Status:** DONE

### 2.5 Fix Missing Null Check in AccountCacheService
- **File:** `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountCacheService.cs:22-31`
- **Issue:** Account loaded from DB without null check
- **Fix:** Added `if (account is null) return;` guard after `FindById`
- **Status:** DONE

## Priority 3: Medium-term (Future Work)

### 3.1 Remove Service Locator Anti-Pattern (ContextScope)
- **Status:** PENDING

### 3.2 UI-Infra Decoupling (UI should only reference App)
- **Status:** PENDING

### 3.3 Standardize Validation Patterns Across Handlers
- **Status:** PENDING

### 3.4 Cache Reflection in CommandDispatcher
- **Status:** PENDING

### 3.5 Extract Duplicated Logic in Command Handlers (currency validation, account validation, transaction details building)
- **Status:** PENDING

### 3.6 Add Composite Indexes for Common Multi-Field Queries
- **Status:** PENDING

### 3.7 Move BaseSettings from Infra to UI Layer
- **Status:** PENDING

## Priority 4: Long-term (Future Work)

### 4.1 Upgrade Preview/RC Packages to Stable
- **Status:** PENDING

### 4.2 Add Structured Logging to Critical Paths
- **Status:** PENDING

### 4.3 Fix Bare Exception Swallowing in Multiple Files
- **Status:** PENDING

### 4.4 Batch Goal Progress Calculations
- **Status:** PENDING

### 4.5 Fix Channel/Timer Recreation Without Cleanup in BackgroundJobManager
- **Status:** PENDING
