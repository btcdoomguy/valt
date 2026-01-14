# Codebase Concerns

**Analysis Date:** 2026-01-14

## Tech Debt

**Database Abstraction Leak:**
- Issue: `ILocalDatabase` interface exposes LiteDB collection types directly
- File: `src/Valt.Infra/DataAccess/ILocalDatabase.cs` (line 13)
- TODO: `//TODO: refactor to hide this from the UI`
- Impact: Creates tight coupling between UI and LiteDB implementation
- Fix approach: Create repository abstractions that don't expose LiteDB types

**Application Layer Missing:**
- Issue: Fixed expense binding logic is in ViewModel instead of service layer
- Files: `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs` (lines 383, 398)
- TODO: `//TODO: move to a specific app layer` (appears twice)
- Impact: Business logic mixed with presentation concerns
- Fix approach: Create `ITransactionService` to encapsulate business operations

**Thread Safety Gap:**
- Issue: Missing synchronization for concurrent icon map access
- File: `src/Valt.UI/Services/IconMaps/IconMapLoader.cs` (line 16)
- TODO: `//TODO: semaphore`
- Impact: Potential race conditions during icon loading
- Fix approach: Add `SemaphoreSlim` for thread-safe access to `_codeMap`

**Background Job Lifecycle:**
- Issue: Background jobs not properly stopped on application shutdown
- File: `src/Valt.UI/App.axaml.cs` (line 93)
- TODO: `//TODO: stop jobs before finalizing`
- Impact: Resource leaks, potential unhandled exceptions during shutdown
- Fix approach: Call `BackgroundJobManager.StopAll()` in application shutdown handler

**Critical Calculation Untested:**
- Issue: Wealth calculation logic explicitly marked as needing urgent testing
- File: `src/Valt.UI/State/AccountsTotalState.cs` (line 49)
- TODO: `//TODO: test this urgently`
- Impact: Critical financial calculation may have bugs
- Fix approach: Add comprehensive unit tests for wealth calculation

## Performance Bottlenecks

**N+1 Query Problem:**
- Problem: Separate database query for each transaction to fetch fixed expense records
- File: `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs` (line 60)
- TODO: `//TODO: optimize to avoid N-1`
- Measurement: Scales poorly with transaction count
- Cause: For-each loop executing individual queries
- Improvement path: Batch query or eager loading of fixed expense records

**Large Files (Code Organization):**
- `src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorViewModel.cs` - 851 lines
  - Issue: Single file handles validation, creation, editing, multiple account types
  - Impact: Difficult to maintain, high cyclomatic complexity
  - Fix: Extract into smaller focused classes

- `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs` - 570 lines
  - Issue: Manages accounts, transactions, fixed expenses in one ViewModel

- `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs` - 409 lines
  - Issue: Complex aggregation logic in single file

- `src/Valt.Infra/Services/CsvImport/CsvImportExecutor.cs` - 363 lines
  - Issue: Account creation, category creation, transaction creation all in one file

## Security Considerations

**Silent Exception Handling:**
- Risk: Exceptions silently swallowed without logging in some crawlers
- File: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs` (lines 157-164)
- Current mitigation: Falls back to safe default
- Recommendations: Add logging for silent catch blocks to aid debugging

**Unprotected Database Password Change:**
- Risk: Complex password change operation has minimal error handling
- File: `src/Valt.Infra/DataAccess/LocalDatabase.cs` (lines 68-139)
- Current mitigation: None
- Recommendations: Add cleanup in catch block for temporary files

## Fragile Areas

**Null Assertions Without Checks:**
- Files: `src/Valt.UI/Views/Main/Tabs/Transactions/TransactionListViewModel.cs` (lines 386, 401)
- Issue: `transaction!.SetFixedExpense()` used without null check after async query
- Risk: NullReferenceException if transaction not found
- Fix: Add null check before using transaction

**Fire-and-Forget Async Calls:**
- Pattern: `_ = AsyncMethod();` used throughout codebase (18+ occurrences)
- Files: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` (line 128), others
- Risk: Unobserved task exceptions could crash the application
- Fix: Either await properly or add explicit exception handling

## Dependencies at Risk

**None identified** - All dependencies are actively maintained:
- LiteDB - Active development
- Avalonia - Active development
- CommunityToolkit.Mvvm - Microsoft-maintained
- NUnit, NSubstitute - Active development

## Missing Features

**Error Handling in File Operations:**
- Problem: File operations lack try-catch in several places
- Files:
  - `src/Valt.Infra/Kernel/CrashReportService.cs` (line 34)
  - `src/Valt.UI/Services/LocalStorage/LocalStorageService.cs` (line 89)
- Risk: Unhandled exceptions if disk full or permissions denied
- Implementation complexity: Low

## Test Coverage Gaps

**CSV Import Workflow:**
- What's not tested: Full end-to-end CSV import integration
- Files: Individual components tested, but not workflow integration
- Risk: Import could break silently
- Priority: Medium
- Difficulty: Need mock file system and progress tracking

**Wealth Calculation:**
- What's not tested: Complex wealth calculation in `AccountsTotalState`
- File: `src/Valt.UI/State/AccountsTotalState.cs`
- Risk: Financial calculation errors unnoticed
- Priority: High (marked with urgent TODO)
- Difficulty: Complex multi-currency conversion logic

## Debug Code in Production

**Leftover Test Class:**
- Issue: Empty class `Foo` left in application code
- File: `src/Valt.UI/App.axaml.cs` (line 96)
- Code: `public class Foo;`
- Impact: Minor (unused), but indicates incomplete cleanup
- Fix: Remove the class

---

*Concerns audit: 2026-01-14*
*Update as issues are fixed or new ones discovered*
