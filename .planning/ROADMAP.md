# Roadmap: Valt

**Milestone:** v0.4 Quality & Performance
**Created:** 2026-06-19
**Continues from:** Phase 10

## Milestone Goal

Eliminate stability risks, reduce wasteful background work, simplify oversized ViewModels, and stabilize the test suite without adding new user-facing features.

## Phases

| # | Phase | Goal | Requirements | Success Criteria |
|---|---|------|--------------|------------------|
| 11 | Safe Fire-and-Forget | 3/3 | Complete    | 2026-06-19 |
| 12 | Remove Blocking Calls | 1/1 | Complete   | 2026-06-19 |
| 13 | HTTP Client Factory | 2/2 | Complete    | 2026-06-19 |
| 14 | Event-Driven Goal Updates | 3/3 | Complete   | 2026-06-22 |
| 15 | Throttle Account Totals Job | 1/1 | Complete | 2026-06-22 |
| 16 | Centralize LiteDB Indexes | 2/2 | Complete   | 2026-06-22 |
| 17 | Transaction Editor Builder | 2/2 | Complete    | 2026-06-22 |
| 18 | Manage Asset Builder | 2/2 | Complete    | 2026-06-23 |
| 19 | Modal Launcher Service | Extract reusable modal-launcher service from `MainViewModel` | VM-SVC-03 | 3 |
| 20 | Reports Dashboard Builders | Extract leverage/loan dashboard builders | VM-SVC-04 | 4 |
| 21 | Transaction Editor Child VMs | Split editor into per-transfer-type child VMs | VM-CHILD-01 | 5 |
| 22 | Manage Asset Child VMs | Split asset modal into per-asset-type child VMs | VM-CHILD-02 | 5 |
| 23 | Reports Child VMs | Split reports into per-report child VMs | VM-CHILD-03 | 5 |
| 24 | MainViewModel Final Split | Split main shell into coordinator/aggregator/child VMs | VM-CHILD-04 | 4 |
| 25 | Isolate Live API Tests | Move network-dependent tests behind `[Category("LiveApi")]` | TEST-01 | 3 |
| 26 | Test Base Hygiene | Fix `DatabaseTest` lifecycle and timing-dependent waits | TEST-02, TEST-03 | 4 |
| 27 | Command Handler Tests | Add tests for high-value edit/update/bulk commands | HANDLER-01, HANDLER-02 | 4 |
| 28 | Query Handler Tests | Add tests for high-value read queries | HANDLER-03, HANDLER-04 | 4 |

**Total phases:** 18
**Total v1 requirements mapped:** 27
**Coverage:** 27/27 ✓

## Phase Details

### Phase 11: Safe Fire-and-Forget

**Goal:** Eliminate fire-and-forget crashes in the UI by replacing `SafeFireAndForget` and all `async void` methods with observable, logged patterns.

**Requirements:** ASYNC-01, ASYNC-03

**Success criteria:**

1. `SafeFireAndForget` is removed or rewritten to require an `ILogger` and catch/log exceptions.
2. All 5 `async void` methods in `.axaml.cs` and ViewModels are converted to `async Task` or command-based patterns.
3. All 26 `SafeFireAndForget` call sites are replaced with `FireAndForgetSafeAsync` (or equivalent) that accepts a logger.
4. Build passes and existing UI tests still pass.

**Plans:** 3/3 plans complete

Plans:
**Wave 1**

- [x] 11-01-PLAN.md — Introduce IFireAndForgetTaskRunner, replace SafeFireAndForget extension, register in DI, add runner tests

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 11-02-PLAN.md — Replace SafeFireAndForget call sites in ViewModels and move TransactionEditorViewModel init to OnBindParameterAsync
- [x] 11-03-PLAN.md — Convert async void event handlers, lifecycle hooks, application shutdown cleanup, and update tests

### Phase 12: Remove Blocking Calls

**Goal:** Remove synchronous blocking on async work in `ReportsViewModel`.

**Requirements:** ASYNC-02

**Success criteria:**

1. The two `.GetAwaiter().GetResult()` calls on `_indicatorsPanel.RefreshAsync()` in `ReportsViewModel` are removed.
2. `Initialize()` and `LoadCachedIndicatorsData()` become fully asynchronous or defer indicator loading without blocking the UI thread.
3. No `.GetAwaiter().GetResult()` remains in ViewModels (verified by grep/regex).
4. Reports tab still initializes and refreshes correctly.

**Plans:** 1/1 plans complete

Plans:

- [x] 12-01-PLAN.md
- [ ] 12-PLAN-01.md — Remove `.GetAwaiter().GetResult()` from `ReportsViewModel`, delete dead code, and add architecture guard tests

### Phase 13: HTTP Client Factory

**Goal:** Register `IHttpClientFactory` and migrate all 15 direct `new HttpClient()` sites to named/typed clients.

**Requirements:** HTTP-01, HTTP-02, HTTP-03

**Success criteria:**

1. `services.AddHttpClient()` is called in the DI composition root.
2. All crawlers, providers, and update checkers receive `HttpClient` via constructor or `IHttpClientFactory`.
3. No direct `new HttpClient()` remains in production code (verified by grep).
4. Existing provider tests still pass; new factory registrations do not break singleton provider lifetime.

**Plans:** 2/2 plans complete

Plans:

**Wave 1**

- [x] 13-01-PLAN.md — Register IHttpClientFactory, add named clients, create HttpClientNames constants, refactor crawlers/providers/update checker

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 13-02-PLAN.md — Refactor UI ViewModels, update all provider tests with HttpClientTestFactory, verify no `new HttpClient()` remains

### Phase 14: Event-Driven Goal Updates

**Goal:** Trigger `GoalProgressUpdaterJob` from stale events instead of relying primarily on polling.

**Requirements:** JOB-02, JOB-03

**Success criteria:**

1. `GoalEventHandler`, `MarkGoalsStaleEventHandler`, and `MarkGoalsStaleOnPriceUpdateHandler` inject `BackgroundJobManager` and trigger the job after `MarkAsStale()`.
2. App-layer command handlers (`RecalculateGoal`, `CopyGoalsFromLastMonth`) publish a notification that is handled in Infra to trigger the job.
3. `GoalProgressUpdaterJob` interval is raised to at least 60 seconds as a fallback.
4. Goal progress still recalculates promptly after relevant changes.

**Plans:** 3/3 plans complete

Plans:

**Wave 1**

- [x] 14-01-PLAN.md — Move notification abstractions to Valt.App and update all consumers

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 14-02-PLAN.md — Wire Infra goal domain-event handlers to trigger GoalProgressUpdaterJob and raise interval
- [x] 14-03-PLAN.md — Bridge App command handlers to Infra job triggering via GoalProgressUpdateRequested notification

### Phase 15: Throttle Account Totals Job

**Goal:** Reduce `AccountTotalsJob` polling to a day-rollover safety net.

**Requirements:** JOB-01, JOB-03

**Success criteria:**

1. `AccountTotalsJob.Interval` is raised to 60 seconds (or longer).
2. The job still detects local-day rollover and refreshes current totals.
3. Incremental account cache updates remain event-driven through existing domain-event handlers.
4. No user-visible delay in account totals after day change.

**Plans:** 1/1 complete

Plans:

**Wave 1**

- [x] 15-01-PLAN.md — Raise `AccountTotalsJob` interval to 120 seconds and add day-rollover unit + architecture tests

### Phase 16: Centralize LiteDB Indexes

**Goal:** Ensure LiteDB indexes once per database open instead of on every collection access.

**Requirements:** DB-01, DB-02, DB-03

**Success criteria:**

1. `LocalDatabase` has a private `EnsureIndexes()` method called from all `Open*` paths.
2. `PriceDatabase` has a private `EnsureIndexes()` method called from all `Open*` paths.
3. Per-access `EnsureIndex` calls are removed from collection accessor methods.
4. All existing indexes remain in place; no query regressions.

**Plans:** 2/2 plans complete

Plans:

**Wave 1**

- [x] 16-01-PLAN.md — Centralize LiteDB indexes in LocalDatabase and PriceDatabase

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 16-02-PLAN.md — Verify centralized LiteDB index coverage with in-memory tests

### Phase 17: Transaction Editor Builder

**Goal:** Extract a dedicated builder service for transaction-details DTOs from `TransactionEditorViewModel`.

**Requirements:** VM-SVC-01

**Success criteria:**

1. A new `ITransactionDetailsBuilder` / `TransactionDetailsBuilder` service exists in `Valt.UI` or `Valt.App`.
2. `BuildTransactionDetailsDtoFromForm` and `LoadTransactionDetailsFromDto` logic moves into the service.
3. `TransactionEditorViewModel` calls the service and remains functionally identical.
4. Unit tests cover the builder for all transfer types.

**Plans:** 2/2 plans complete

Plans:

**Wave 1**

- [x] 17-01-PLAN.md — Create ITransactionDetailsBuilder/TransactionDetailsBuilder service and unit tests

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 17-02-PLAN.md — Wire builder into TransactionEditorViewModel and verify full suite

### Phase 18: Manage Asset Builder

**Goal:** Extract a dedicated builder service for per-type asset command/DTO construction from `ManageAssetViewModel`.

**Requirements:** VM-SVC-02

**Success criteria:**

1. A new `IAssetFormBuilder` / `AssetFormBuilder` service exists.
2. Create/edit DTO construction for all 9 asset types moves into the service.
3. `ManageAssetViewModel` calls the service and remains functionally identical.
4. Unit tests cover the builder for at least the complex asset types (BTC loan, leveraged, real estate).

**Plans:** 2/2 plans complete

Plans:

**Wave 1**

- [x] 18-01-PLAN.md — Create IAssetFormBuilder/AssetFormBuilder service and exception type

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 18-02-PLAN.md — Wire builder into ManageAssetViewModel, register DI, update/add tests

### Phase 19: Modal Launcher Service

**Goal:** Remove repeated modal-launch boilerplate from `MainViewModel`.

**Requirements:** VM-SVC-03

**Success criteria:**

1. A new `IModalLauncher` / `ModalLauncher` service wraps `_modalFactory.CreateAsync` + `ShowDialogSafeAsync`.
2. `MainViewModel` uses the service for all modal launches.
3. ~150 lines of repetitive code are removed.
4. Modal behavior remains unchanged.

**Plans:** 2 plans

Plans:

**Wave 1**

- [ ] 19-01-PLAN.md — Create IModalLauncher/ModalLauncher service, DI registration, and unit tests

**Wave 2** *(blocked on Wave 1 completion)*

- [ ] 19-02-PLAN.md — Refactor MainViewModel to use IModalLauncher and add command tests

### Phase 20: Reports Dashboard Builders

**Goal:** Extract leverage-position and BTC-loan dashboard data generation from `ReportsViewModel`.

**Requirements:** VM-SVC-04

**Success criteria:**

1. New services or child VMs own `LeveragePositionsData` and `BtcLoansData` generation.
2. `ReportsViewModel` delegates to them and still reacts to price/settings changes.
3. Dashboard data remains identical after the move.
4. Services/VMs have unit tests.

**Plans:** TBD

### Phase 21: Transaction Editor Child VMs

**Goal:** Split `TransactionEditorViewModel` into per-transfer-type child VMs with `ContentControl` + `DataTemplate`s.

**Requirements:** VM-CHILD-01

**Success criteria:**

1. Child VMs exist for the major transfer-type groups (e.g., fiat transaction, bitcoin transaction, transfer).
2. `TransactionEditorView.axaml` uses `ContentControl` with `DataTemplate`s bound to the active child VM.
3. Validation and command building still work for all transaction types.
4. Existing tests pass; new child-VM tests added.

**Plans:** TBD

### Phase 22: Manage Asset Child VMs

**Goal:** Split `ManageAssetViewModel` into per-asset-type child VMs with `ContentControl` + `DataTemplate`s.

**Requirements:** VM-CHILD-02

**Success criteria:**

1. Child VMs exist for the major asset-type groups (basic, real estate, leveraged, BTC loan, BTC lending).
2. `ManageAssetView.axaml` uses `ContentControl` with `DataTemplate`s bound to the active child VM.
3. Create/edit flows and validation still work for all asset types.
4. Existing tests pass; new child-VM tests added.

**Plans:** TBD

### Phase 23: Reports Child VMs

**Goal:** Split `ReportsViewModel` into per-report child VMs with a coordinator owning filters and the report-data cache.

**Requirements:** VM-CHILD-03

**Success criteria:**

1. Per-report child VMs exist for monthly totals, category charts, wealth overview, indicators, leverage, BTC loans, etc.
2. A `ReportsCoordinator` (or the remaining `ReportsViewModel`) owns filter state, `IReportDataProvider` cache, and refresh orchestration.
3. `ReportsView.axaml` binds to child VMs instead of a flat list of top-level properties.
4. Reports still refresh correctly on filter/price/settings changes.

**Plans:** TBD

### Phase 24: MainViewModel Final Split

**Goal:** Split remaining `MainViewModel` responsibilities into database-flow coordinator, job-status aggregator, and market-mood child VMs.

**Requirements:** VM-CHILD-04

**Success criteria:**

1. `DatabaseFlowCoordinator` or equivalent owns open/close/migrate sequence.
2. `JobStatusAggregator` or equivalent owns background-job status presentation.
3. `MarketMoodViewModel` or equivalent owns Pepe/offline state.
4. `MainViewModel` focuses on shell/tab/navigation state; XAML still binds correctly.

**Plans:** TBD

### Phase 25: Isolate Live API Tests

**Goal:** Move network-dependent tests behind `[Category("LiveApi")]` so CI does not depend on external services.

**Requirements:** TEST-01

**Success criteria:**

1. All 8 live-API fixtures are marked `[Category("LiveApi")]`.
2. Deterministic mocked tests exist for the same providers (recorded responses or NSubstitute).
3. A filter/runsettings file lets CI skip `[Category("LiveApi")]` tests.
4. `dotnet test --filter "TestCategory!=LiveApi"` passes locally without network.

**Plans:** TBD

### Phase 26: Test Base Hygiene

**Goal:** Fix `DatabaseTest` lifecycle issues and replace timing-dependent waits.

**Requirements:** TEST-02, TEST-03

**Success criteria:**

1. The two duplicate `[OneTimeTearDown]` methods in `DatabaseTest` are merged.
2. Subclasses that hide `SetUp` with `new` are fixed to call `base.SetUp()` or use `override`.
3. The 60 `Task.Delay` calls in UI tests are replaced with deterministic synchronization.
4. Test-project compiler warnings are reduced.

**Plans:** TBD

### Phase 27: Command Handler Tests

**Goal:** Add unit tests for high-value edit/update/bulk command handlers.

**Requirements:** HANDLER-01, HANDLER-02

**Success criteria:**

1. `EditTransactionCommand` handler tests cover happy path and validation errors.
2. `EditFixedExpenseCommand` handler tests cover happy path and validation errors.
3. `EditAssetCommand`, `UpdateAssetPriceCommand`, `UpdateAssetQuantityCommand` handler tests exist.
4. `BulkChangeCategoryTransactionsCommand` and `BulkRenameTransactionsCommand` handler tests exist.

**Plans:** TBD

### Phase 28: Query Handler Tests

**Goal:** Add unit tests for high-value read query handlers.

**Requirements:** HANDLER-03, HANDLER-04

**Success criteria:**

1. `GetTransactionsQuery` handler tests exist.
2. `GetAccountsQuery` and `GetFixedExpenseHistoryQuery` handler tests exist.
3. `GetSpendingEvolutionQuery` handler tests exist.
4. `GetAssetQuery` and `GetAssetSummaryQuery` handler tests exist.

**Plans:** TBD

## Dependencies

- Phase 11 must complete before Phase 12.
- Phases 11-12 should complete before Phase 13 (factory migration touches crawlers used by jobs).
- Phase 14 must complete before Phase 15 (both touch job scheduling).
- Phase 16 is independent and can run in parallel with Phases 11-15.
- Phase 17 must complete before Phase 21.
- Phase 18 must complete before Phase 22.
- Phase 19 can run in parallel with Phases 17-18.
- Phase 20 must complete before Phase 23.
- Phases 21-24 are UI-restructuring phases and should be sequenced by risk: Transaction Editor, Manage Asset, Reports, Main.
- Phase 25 should complete before Phases 26-28 to avoid flaky CI during the coverage push.
- Phases 26-28 can overlap but Phase 26 hygiene fixes make Phases 27-28 more reliable.

## Notes

- Phase numbering continues from the previous milestone (Phase 10), so this milestone spans Phases 11-28.
- No `.planning/phases/` directories are created by this roadmap; they are generated when each phase is planned via `/gsd-plan-phase [N]`.
- The v0.4 milestone intentionally defers large architectural changes (removing the `Valt.UI` → `Valt.Infra` reference, secure-mode password hashing) to v0.5 so that v0.4 delivers incremental, verifiable improvements.

---
*Last updated: 2026-06-19 after milestone v0.4 roadmap creation*
