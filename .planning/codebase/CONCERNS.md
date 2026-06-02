# Codebase Concerns

**Analysis Date:** 2026-05-27

## Tech Debt

### LiteDB Repository Bug Workaround
- **Issue:** `IFixedExpenseRepository.GetFixedExpenseByIdAsync()` throws exceptions when entity not found due to accessing properties on null entity in a LiteDB expression. Code uses `catch (Exception) when (true)` as a workaround.
- **Files:** `src/Valt.App/Modules/Budget/FixedExpenses/Commands/DeleteFixedExpense/DeleteFixedExpenseHandler.cs`, `src/Valt.App/Modules/Budget/FixedExpenses/Commands/EditFixedExpense/EditFixedExpenseHandler.cs`
- **Impact:** Fragile error handling that could mask other legitimate exceptions.
- **Fix approach:** Fix the root cause in the repository implementation to return null properly instead of throwing.

### async void Methods in UI Layer
- **Issue:** Several UI event handlers use `async void` instead of `async Task`, making exception handling and testing difficult.
- **Files:** 
  - `src/Valt.UI/Views/Main/Modals/ManageGoal/GoalTypeEditors/ReduceExpenseCategoryGoalTypeEditorViewModel.cs:46`
  - `src/Valt.UI/Views/Main/Modals/Settings/SettingsViewModel.cs:177`
  - `src/Valt.UI/Views/Main/Modals/ManageCategories/ManageCategoriesView.axaml.cs:63`
  - `src/Valt.UI/Views/Main/MainView.axaml.cs:89`
  - `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsView.axaml.cs:89`
- **Impact:** Unhandled exceptions in these methods will crash the application. Cannot be awaited or tested.
- **Fix approach:** Refactor to `async Task` with proper event handler wrappers, or use `SafeFireAndForget` with error handling.

### lock(object) Instead of Lock Class
- **Issue:** Multiple files use `lock (object)` instead of the .NET 9 `Lock` class as required by project conventions.
- **Files:**
  - `src/Valt.Infra/DataAccess/PriceDatabase.cs` (uses `Lock _writeLock` - actually correct)
  - `src/Valt.Infra/Kernel/BackgroundJobs/JobLogPool.cs`
  - `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/ThrottledBitcoinPriceProvider.cs`
  - `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/CoinGeckoRateLimiter.cs`
  - `src/Valt.Infra/Modules/Reports/ReportDataProvider.cs`
  - `src/Valt.UI/Services/IconMaps/IconMapLoader.cs`
  - `src/Valt.UI/State/TabRefreshState.cs`
- **Impact:** Suboptimal performance and not following project conventions.
- **Fix approach:** Replace `private readonly object _lock = new();` with `private readonly Lock _lock = new();` in all files.

### SafeFireAndForget Pattern
- **Issue:** `SafeFireAndForget` is an `async void` extension method used extensively throughout the UI layer to fire-and-forget tasks. While it has error logging, it still has the fundamental `async void` problem.
- **Files:** `src/Valt.UI/Base/TaskExtensions.cs:20`
- **Impact:** Exceptions after the initial await may be lost or cause unexpected behavior.
- **Fix approach:** Consider using `FireAndForget` with a returned `Task` that can be tracked, or ensure all inner exceptions are properly caught.

## Known Bugs

### Sync-over-Async in Event Handler
- **Bug description:** `MarkGoalsStaleOnPriceUpdateHandler` uses `.GetAwaiter().GetResult()` for async repository calls.
- **Files:** `src/Valt.Infra/Modules/Goals/Handlers/MarkGoalsStaleOnPriceUpdateHandler.cs:50`, `:59`
- **Trigger:** Any price update event
- **Impact:** Risk of deadlocks, especially in UI thread contexts. Blocks the calling thread.
- **Fix approach:** Make the handler method async and await the calls properly.

### Empty/Silent Catch Blocks
- **Bug description:** Multiple catch blocks silently swallow exceptions without logging.
- **Files:**
  - `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs:224` - `catch (Exception)` with empty body
  - `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs:243` - `catch (Exception)` with empty body
  - `src/Valt.UI/Views/Main/Modals/ConversionCalculator/ConversionCalculatorViewModel.cs:175` - `catch (Exception)` with empty body
  - `src/Valt.UI/Views/Main/Tabs/AvgPrice/AvgPriceViewModel.cs:503` - `catch (Exception)` with empty body
  - `src/Valt.UI/Services/Theming/ThemeService.cs:88` - `catch (Exception)` with empty body
  - `src/Valt.UI/UserControls/UpdateIndicatorViewModel.cs:251` - `catch` with comment "Silently fail"
- **Impact:** Errors go undetected, making debugging extremely difficult. Data corruption may occur silently.
- **Fix approach:** Add at minimum `_logger.LogWarning(ex, ...)` to all catch blocks.

### Commented-Out Code
- **Bug description:** Dead code left in source files.
- **Files:** `src/Valt.UI/Views/Main/Modals/IconSelector/IconSelectorViewModel.cs:71` - `//FilterIconsAsync().Wait();`
- **Impact:** Code clutter, potential confusion.

## Security Considerations

### Database Password in Memory
- **Risk:** The database password is stored as a plain string field `_password` in `LocalDatabase` and retained for the lifetime of the database connection.
- **Files:** `src/Valt.Infra/DataAccess/LocalDatabase.cs:26`, `:45`
- **Current mitigation:** None - standard .NET string immutability means the password remains in memory until GC.
- **Recommendations:** Consider using `SecureString` or at minimum ensuring the field is cleared on `CloseDatabase()` and `Dispose()`.

### Temporary File During Password Change
- **Risk:** During `ChangeDatabasePassword`, a temporary folder is created in `Path.GetTempPath()` with a random GUID name. If the operation fails mid-way, the temp file may not be cleaned up.
- **Files:** `src/Valt.Infra/DataAccess/LocalDatabase.cs:93-146`
- **Current mitigation:** Uses `File.Replace()` for atomic swap, but temp folder cleanup is not in a `finally` block.
- **Recommendations:** Wrap temp directory cleanup in a `try/finally` block.

### MCP Server Without Authentication
- **Risk:** The embedded MCP server exposes database operations via HTTP on localhost without any authentication mechanism.
- **Files:** `src/Valt.Infra/Mcp/Server/McpServerService.cs`
- **Current mitigation:** Binds to localhost only (`ListenLocalhost`), not accessible from network.
- **Recommendations:** Add API key or token-based authentication if the server might run on shared machines.

### HttpClient Not Using IHttpClientFactory
- **Risk:** `GitHubUpdateChecker` creates a new `HttpClient` instance per check. `UpdateIndicatorViewModel` also creates a new `HttpClient` for downloads.
- **Files:** `src/Valt.Infra/Services/Updates/GitHubUpdateChecker.cs:19`, `src/Valt.UI/UserControls/UpdateIndicatorViewModel.cs:149`
- **Current mitigation:** None.
- **Recommendations:** Use `IHttpClientFactory` for proper connection pooling and socket exhaustion prevention.

## Performance Bottlenecks

### ReportsViewModel Monolith
- **Problem:** `ReportsViewModel` is 1,374 lines and handles 10+ dashboard panels, chart data, filters, and async loading states. This violates single responsibility principle.
- **Files:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`
- **Cause:** All report data fetching and UI state management consolidated in one class.
- **Improvement path:** Extract individual panel ViewModels (WealthPanel, BtcStackPanel, LeveragePanel, etc.) and delegate to them.

### Repeated Database Queries in Loops
- **Problem:** `FiatHistoryUpdaterJob` calls `_priceDatabase.GetFiatData()` inside loops and individual currency processing, creating repeated collection access overhead.
- **Files:** `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs`
- **Cause:** Collection access is not cached; `EnsureIndex` may be called repeatedly.
- **Improvement path:** Cache collection references and batch operations where possible.

### CurrentWealth Calculation on Every Access
- **Problem:** `AccountsTotalState.CurrentWealth` performs complex currency conversion calculations on every property access.
- **Files:** `src/Valt.UI/State/AccountsTotalState.cs:38`
- **Cause:** Property getter does real-time calculations with multiple rate lookups and division operations.
- **Improvement path:** Cache the calculated value and invalidate only when inputs change.

### Duplicate Entry Removal Loads All Records
- **Problem:** `RemoveDuplicateEntries()` loads entire Bitcoin and Fiat collections into memory to find duplicates.
- **Files:** `src/Valt.Infra/DataAccess/PriceDatabase.cs:184-225`
- **Cause:** Uses `FindAll()` then `GroupBy` in memory.
- **Improvement path:** Use database-side aggregation queries or process in batches.

### Aggressive Background Job Retry
- **Problem:** Background jobs retry 3 times with only 100ms delay between attempts.
- **Files:** `src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs:141`
- **Cause:** Very short retry delay doesn't give transient issues (network, DB lock) time to resolve.
- **Improvement path:** Use exponential backoff (e.g., 1s, 5s, 15s).

## Fragile Areas

### TransactionEditorViewModel Complexity
- **Files:** `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs` (1,016 lines)
- **Why fragile:** Complex validation logic with multiple custom validators, account mode switching, and installment handling. The `BuildTransactionDetailsDtoFromForm()` method uses string-based account mode matching.
- **Safe modification:** Add unit tests for `BuildTransactionDetailsDtoFromForm()` before changing. Validate all transfer type combinations.
- **Test coverage:** Only basic ViewModel tests exist; the complex validation matrix is not fully covered.

### Currency Rate Dictionary Access Without Null Checks
- **Files:** `src/Valt.UI/State/AccountsTotalState.cs:156`, `:184`
- **Why fragile:** Direct dictionary indexer access `_ratesState.FiatRates[_currencySettings.MainFiatCurrency]` without `TryGetValue` can throw `KeyNotFoundException`.
- **Safe modification:** Replace all direct indexer accesses with `TryGetValue` and graceful fallback.
- **Test coverage:** Partial - some rate scenarios tested but not all missing currency combinations.

### Background Job Manager Stop Timeout
- **Files:** `src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs:319`
- **Why fragile:** 5-second timeout for job stop may not be sufficient for long-running DB operations. If a job is mid-transaction, the timeout could leave the database in an inconsistent state.
- **Safe modification:** Ensure all background jobs support cooperative cancellation and checkpoint regularly.
- **Test coverage:** Basic manager tests exist but not timeout scenarios.

### CSV Import Error Handling
- **Files:** `src/Valt.Infra/Services/CsvImport/CsvImportExecutor.cs`
- **Why fragile:** Individual row failures are collected as strings but the import continues. Partial success state may leave accounts/categories created but not all transactions.
- **Safe modification:** Consider transaction wrapping or rollback capability for atomic imports.
- **Test coverage:** Good test coverage for happy path; limited tests for partial failure scenarios.

## Scaling Limits

### LiteDB Concurrent Access
- **Current capacity:** Single-threaded database access with `SemaphoreSlim` lock for MCP operations.
- **Limit:** The `LocalDatabase` uses a single `SemaphoreSlim(1,1)` for thread-safe access, but UI operations don't use it. Concurrent UI + MCP access could cause LiteDB corruption.
- **Scaling path:** Consider WAL mode or queuing all DB access through a single channel.

### Price Database Growth
- **Current capacity:** Bitcoin daily prices since 2020 + 32 fiat currencies daily.
- **Limit:** `RemoveDuplicateEntries` loads entire collections. At 10+ years of data, this will consume significant memory.
- **Scaling path:** Implement batch processing or database-side deduplication.

## Dependencies at Risk

### LiveChartsCore.SkiaSharpView (Preview Version)
- **Risk:** Using pre-release version `2.1.0-dev-365`.
- **Impact:** Potential breaking changes, bugs, or compatibility issues with Avalonia 12.
- **Migration plan:** Monitor for stable release and upgrade when available.

### SkiaSharp.NativeAssets.Linux (Preview Version)
- **Risk:** Using preview version `3.119.4-preview.1.1`.
- **Impact:** Potential rendering issues on Linux.
- **Migration plan:** Upgrade to stable when released.

### YahooFinanceApi
- **Risk:** Unofficial API wrapper that may break if Yahoo changes their API.
- **Impact:** Asset price updates for stocks/ETFs would fail.
- **Migration plan:** No official alternative; monitor for community forks or switch to another provider.

## Missing Critical Features

### Database Backup Automation
- **Problem:** No automated backup mechanism. Password change creates a single `.bak` file but no rotation.
- **Files:** `src/Valt.Infra/DataAccess/LocalDatabase.cs:91`
- **Blocks:** Users could lose data if the database file becomes corrupted.

### LiteDB Corruption Recovery
- **Problem:** No validation or recovery logic for corrupted LiteDB files on startup.
- **Blocks:** Application may crash on startup with unhelpful errors if the DB is corrupted.

### Graceful Price Provider Degradation
- **Problem:** When all price providers fail, the application shows error states but doesn't clearly communicate to users that prices are stale.
- **Blocks:** Users may make financial decisions based on outdated price data.

## Test Coverage Gaps

### UI Layer Tests
- **What's not tested:** 257 UI source files vs 15 test files. Most ViewModels have no tests.
- **Files:** `src/Valt.UI/` (257 files) vs `tests/Valt.Tests/UI/` (15 files)
- **Risk:** UI logic changes can break without detection. Complex validation in `TransactionEditorViewModel` is untested.
- **Priority:** High

### MCP Tools
- **What's not tested:** No tests exist for any MCP tool classes.
- **Files:** `src/Valt.Infra/Mcp/Tools/*.cs`
- **Risk:** MCP tools may break when commands/queries are modified. The tools directly call dispatchers without integration tests.
- **Priority:** Medium

### Background Jobs Under Failure
- **What's not tested:** Timeout scenarios, retry exhaustion, and cancellation mid-operation are not tested.
- **Files:** `src/Valt.Infra/Kernel/BackgroundJobs/`
- **Risk:** Background jobs may leave databases in inconsistent states during failures.
- **Priority:** Medium

### CSV Import Error Scenarios
- **What's not tested:** Partial failures, malformed rows, and duplicate detection are not well-tested.
- **Files:** `src/Valt.Infra/Services/CsvImport/`
- **Risk:** Data import may silently skip records or create inconsistent state.
- **Priority:** Medium

### Update Service
- **What's not tested:** No tests for `GitHubUpdateChecker` or `UpdateIndicatorViewModel` download logic.
- **Files:** `src/Valt.Infra/Services/Updates/`
- **Risk:** Update check failures or download issues are not caught by tests.
- **Priority:** Low

---

*Concerns audit: 2026-05-27*
