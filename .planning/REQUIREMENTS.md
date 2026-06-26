# Requirements: Valt

**Defined:** 2026-06-19
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v1 Requirements

### Async & Fire-and-Forget Safety

- [x] **ASYNC-01**: No `async void` methods remain in the UI layer; all fire-and-forget work is observable and logs failures
- [x] **ASYNC-02**: No `.GetAwaiter().GetResult()` blocking calls remain in ViewModels
- [x] **ASYNC-03**: `SafeFireAndForget` either surfaces exceptions to a logger or is replaced by an explicit command/task pattern

### HTTP Resilience

- [x] **HTTP-01**: `IHttpClientFactory` is registered in DI and all `new HttpClient()` sites use named/typed clients
- [x] **HTTP-02**: Price providers and update checkers share a consistent HTTP client lifetime and configuration
- [x] **HTTP-03**: Existing provider tests continue to pass against the factory-based clients

### Background Job Efficiency

- [x] **JOB-01**: `AccountTotalsJob` polling interval is raised and only acts as a day-rollover safety net
- [x] **JOB-02**: `GoalProgressUpdaterJob` is triggered by stale-flag events instead of relying primarily on polling
- [x] **JOB-03**: Job fallback intervals remain high enough to avoid unnecessary wake-ups

### Database Performance

- [x] **DB-01**: LiteDB indexes are ensured once per database open, not on every collection access
- [x] **DB-02**: `LocalDatabase` and `PriceDatabase` expose a single `EnsureIndexes()` call invoked from all open paths
- [x] **DB-03**: No regression in query behavior or index coverage after centralization

### ViewModel Simplification — Services

- [x] **VM-SVC-01**: `TransactionEditorViewModel` delegates transaction-details DTO construction to a dedicated builder service
- [x] **VM-SVC-02**: `ManageAssetViewModel` delegates per-type asset command/DTO construction to a dedicated builder service
- [ ] **VM-SVC-03**: `MainViewModel` uses a reusable modal-launcher service instead of repeated `_modalFactory`/`ShowDialogSafeAsync` boilerplate
- [ ] **VM-SVC-04**: `ReportsViewModel` delegates leverage-position and BTC-loan dashboard data generation to dedicated services or child VMs

### ViewModel Simplification — Child VMs & XAML

- [ ] **VM-CHILD-01**: `TransactionEditorViewModel` is split into per-transfer-type child VMs bound through `ContentControl` + `DataTemplate`s
- [ ] **VM-CHILD-02**: `ManageAssetViewModel` is split into per-asset-type child VMs bound through `ContentControl` + `DataTemplate`s
- [ ] **VM-CHILD-03**: `ReportsViewModel` is split into per-report child VMs with a coordinator owning filters and the report-data cache
- [ ] **VM-CHILD-04**: `MainViewModel` is split into database-flow coordinator, job-status aggregator, and market-mood child VMs

### Test Reliability

- [ ] **TEST-01**: Live-API tests are isolated behind `[Category("LiveApi")]` and do not run in normal CI
- [ ] **TEST-02**: `DatabaseTest` lifecycle issues are fixed (duplicate `[OneTimeTearDown]`, `new SetUp()` hiding, state sharing)
- [ ] **TEST-03**: Timing-dependent `Task.Delay` calls in UI tests are replaced with deterministic synchronization

### Handler Test Coverage

- [ ] **HANDLER-01**: `EditTransactionCommand`, `EditFixedExpenseCommand`, and `EditAssetCommand` handlers have unit tests
- [ ] **HANDLER-02**: `UpdateAssetPriceCommand`, `UpdateAssetQuantityCommand`, `BulkChangeCategoryTransactionsCommand`, and `BulkRenameTransactionsCommand` handlers have unit tests
- [ ] **HANDLER-03**: `GetTransactionsQuery`, `GetAccountsQuery`, `GetFixedExpenseHistoryQuery`, and `GetSpendingEvolutionQuery` handlers have unit tests
- [ ] **HANDLER-04**: `GetAssetQuery` and `GetAssetSummaryQuery` handlers have unit tests

## v2 Requirements

### Architecture Hardening

- **ARCH-01**: Remove the direct `Valt.UI` → `Valt.Infra` project reference and enforce the boundary with a NetArchTest rule
- **ARCH-02**: Move report-engine abstractions from `Valt.Infra` to `Valt.App` and expose them through queries
- **ARCH-03**: Move wealth calculation out of `AccountsTotalState` into an App-layer query
- **ARCH-04**: Split `IConfigurationManager` into focused interfaces
- **ARCH-05**: Introduce application-level ports for settings, transaction-term search, and price lookup

### Security Hardening

- **SEC-01**: Replace secure-mode SHA-256 password hashing with PBKDF2 or equivalent
- **SEC-02**: Avoid writing the unencrypted database to a temporary path during password changes
- **SEC-03**: Review and harden MCP server authentication if it remains enabled

### Additional Performance

- **PERF-01**: Replace `ItemsControl` with virtualized `ListBox` in asset lists
- **PERF-02**: Diff/update large bound collections instead of clear/rebuild
- **PERF-03**: Cache chart series and points instead of recreating them on every refresh
- **PERF-04**: Parallelize or batch asset-price updates instead of sequential 1-second-delay loop

### Additional Test Coverage

- **TEST-V2-01**: Add MCP tool tests for `TransactionTools`, `AccountTools`, `GoalTools`, `CurrencyTools`, and `ReportTools`
- **TEST-V2-02**: Add background-job tests for `AutoSatAmountJob`, `AccountTotalsJob`, `GoalProgressUpdaterJob`, `AssetPriceUpdaterJob`, and `BitcoinHistoryUpdaterJob`
- **TEST-V2-03**: Add migration and settings serialization tests

## Out of Scope

| Feature | Reason |
|---------|--------|
| Major UI redesign or new user-facing features | This milestone is hardening/quality, not new capability |
| Replacing LiteDB or Avalonia | Out of scope for a quality milestone; would require its own roadmap |
| Full MCP server redesign | Security review only; redesign deferred to v0.5 |
| Real-time price streaming architecture | Not needed; current polling + event triggers are sufficient after tuning |
| Mobile or web port | Not a quality concern |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ASYNC-01 | Phase 11 | Complete |
| ASYNC-02 | Phase 12 | Complete |
| ASYNC-03 | Phase 11 | Complete |
| HTTP-01 | Phase 13 | Complete |
| HTTP-02 | Phase 13 | Complete |
| HTTP-03 | Phase 13 | Complete |
| JOB-01 | Phase 15 | Complete |
| JOB-02 | Phase 14 | Complete |
| JOB-03 | Phase 14/15 | Complete |
| DB-01 | Phase 16 | Complete |
| DB-02 | Phase 16 | Complete |
| DB-03 | Phase 16 | Complete |
| VM-SVC-01 | Phase 17 | Complete |
| VM-SVC-02 | Phase 18 | Complete |
| VM-SVC-03 | Phase 19 | Pending |
| VM-SVC-04 | Phase 20 | Pending |
| VM-CHILD-01 | Phase 21 | Pending |
| VM-CHILD-02 | Phase 22 | Pending |
| VM-CHILD-03 | Phase 23 | Pending |
| VM-CHILD-04 | Phase 24 | Pending |
| TEST-01 | Phase 25 | Pending |
| TEST-02 | Phase 26 | Pending |
| TEST-03 | Phase 26 | Pending |
| HANDLER-01 | Phase 27 | Pending |
| HANDLER-02 | Phase 27 | Pending |
| HANDLER-03 | Phase 28 | Pending |
| HANDLER-04 | Phase 28 | Pending |

**Coverage:**

- v1 requirements: 27 total
- Mapped to phases: 27
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-19*
*Last updated: 2026-06-19 after milestone v0.4 planning start*
