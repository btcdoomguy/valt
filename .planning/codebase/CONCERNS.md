# Codebase Concerns

**Analysis Date:** 2026-01-13

## Tech Debt

**N+1 Query in TransactionQueries:**
- Issue: `//TODO: optimize to avoid N-1` - Fetches fixed expense records inside loop per transaction
- File: `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs:60`
- Impact: Performance bottleneck when loading transactions with fixed expenses
- Fix approach: Use LiteDB Include() to eager-load related entities in single query

**Database Interface Exposure:**
- Issue: `ILocalDatabase` exposes all LiteDB collections directly to UI layer
- File: `src/Valt.Infra/DataAccess/ILocalDatabase.cs:13`
- Comment: `//TODO: refactor to hide this from the UI. should use a proxy to access what is needed`
- Impact: Architecture violation, UI can bypass repository pattern
- Fix approach: Create proxy interface exposing only domain-specific operations

**Fixed Expense Logic in ViewModel:**
- Issue: Transaction binding logic duplicated directly in ViewModel instead of service layer
- Files: `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs:383,398`
- Comment: `//TODO: move to a specific app layer`
- Impact: Business logic in presentation layer, hard to test
- Fix approach: Extract to use case or service class in Infrastructure layer

**Missing Guard Clauses:**
- Issue: Transaction constructor has no parameter validation
- File: `src/Valt.Core/Modules/Budget/Transactions/Transaction.cs:25`
- Comment: `//TODO: guard clauses`
- Impact: Invalid domain objects can be created
- Fix approach: Add validation for required fields, throw ArgumentException for invalid input

## Known Bugs

**Background Jobs Not Stopped on Shutdown:**
- Symptoms: Potential resource leaks, orphaned tasks on application close
- File: `src/Valt.UI/App.axaml.cs:93`
- Comment: `//TODO: stop jobs before finalizing`
- Workaround: Application closes anyway, jobs terminate with process
- Fix: Add `BackgroundJobManager.StopAll()` call in `OnExit()` or `IApplicationLifetime` callback

**Stray Debug Class:**
- Symptoms: Unused `public class Foo;` at end of file
- File: `src/Valt.UI/App.axaml.cs:96`
- Fix: Remove the class

## Security Considerations

**No Critical Security Issues Identified**
- Password-protected LiteDB database
- No user authentication (desktop app, local data only)
- All external API calls are read-only to public endpoints

## Performance Bottlenecks

**N+1 Query Pattern:**
- Problem: Transaction query fetches fixed expense records in loop
- File: `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs:60`
- Measurement: Not profiled, but O(n) database queries per transaction list
- Cause: Sequential queries for related entities
- Improvement path: Use LiteDB Include() for eager loading

**Blocking Async Operation:**
- Problem: `Task.Delay(100).Wait()` blocks thread
- File: `src/Valt.Infra/Kernel/BackgroundJobs/BackgroundJobManager.cs:28`
- Cause: Synchronous wait on async operation
- Improvement path: Refactor to fully async or use synchronous sleep

## Fragile Areas

**Icon Loading Thread Safety:**
- File: `src/Valt.UI/Services/IconMaps/IconMapLoader.cs:16`
- Comment: `//TODO: semaphore`
- Why fragile: Icon caching without synchronization
- Common failures: Race condition if multiple concurrent icon loads
- Fix: Add `SemaphoreSlim` for thread-safe cache access

**Wealth Calculation:**
- File: `src/Valt.UI/State/AccountsTotalState.cs:49`
- Comment: `//TODO: test this urgently`
- Why fragile: Complex calculation involving multiple currency conversions
- Test coverage: Marked as needing urgent testing
- Fix: Add comprehensive unit tests for `CalculateCurrentWealth()` method

## Error Handling Issues

**Silent Exception Swallowing in Crawlers:**
- Problem: Price crawlers catch all exceptions and return empty data
- Files:
  - `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/Providers/KrakenBitcoinHistoricalDataProvider.cs:67-72`
  - `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/FrankfurterFiatHistoricalDataProvider.cs:89-94`
  - `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/BitcoinInitialSeedPriceProvider.cs:49-54`
- Impact: Users won't know if price data is stale or failed to load
- Fix approach: Add health check status, surface errors in UI

## Async/Await Anti-Patterns

**Async Void Event Handlers:**
- Files:
  - `src/Valt.UI/Views/Main/Modals/Settings/SettingsViewModel.cs:144` - `OnSelectedFiatCurrenciesChanged`
  - `src/Valt.UI/Views/Main/MainView.axaml.cs:47` - `Window_OnClosing`
  - `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsView.axaml.cs:55` - `AccountsList_KeyDown`
- Impact: Exceptions cannot be caught, race conditions possible
- Fix: Return Task where possible, or wrap in try-catch with logging

**Blocking on Task.Result:**
- File: `src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs:95-96`
- Problem: Uses `.Result` after `Task.WhenAll()` which can deadlock
- Fix: Fully await the tasks instead of accessing Result

## Large Files

**Files Exceeding 500 Lines (Consider Refactoring):**
- `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs` (851 lines)
- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionsViewModel.cs` (578 lines)
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (548 lines)
- `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs` (521 lines)
- `src/Valt.UI/Views/Main/MainViewModel.cs` (506 lines)

## Test Coverage Gaps

**Wealth Calculation Untested:**
- File: `src/Valt.UI/State/AccountsTotalState.cs:49`
- What's not tested: `CalculateCurrentWealth()` method
- Risk: Financial calculations could be incorrect
- Priority: High (explicitly marked for urgent testing)

**Price Crawler Error Paths:**
- What's not tested: Exception handling in price providers
- Risk: Silent failures not detected
- Priority: Medium

## Dependencies at Risk

**No Critical Dependency Risks Identified**
- All major packages actively maintained
- .NET 10 is current LTS release
- Avalonia 11.x is stable

## Summary

**High Priority:**
1. N+1 query in TransactionQueries (performance)
2. Wealth calculation needs tests (correctness)
3. Async void event handlers (stability)

**Medium Priority:**
1. Extract fixed expense logic to service layer
2. Add guard clauses to Transaction constructor
3. Fix thread safety in IconMapLoader
4. Stop background jobs on shutdown

**Low Priority:**
1. Remove stray Foo class
2. Refactor large ViewModels (500+ lines)
3. Add health status for price providers

---

*Concerns audit: 2026-01-13*
*Update as issues are fixed or new ones discovered*
