---
gsd_state_version: 1.0
milestone: v0.4
milestone_name: Quality & Performance
current_phase: 19
status: completed
stopped_at: Completed 18-02-PLAN.md
last_updated: "2026-06-28T21:19:48.918Z"
last_activity: 2026-06-28
last_activity_desc: Completed quick task 260628-pgi: Fix NullReferenceException in BtcInput when copying a transaction
progress:
  total_phases: 18
  completed_phases: 9
  total_plans: 18
  completed_plans: 18
  percent: 50
current_phase_name: Modal Launcher Service
---

# STATE.md

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-19)

**Core value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.
**Current focus:** Phase 19 — Modal Launcher Service

## Current Position

Phase: 19 — COMPLETE
Plan: 2 of 2
Status: Phase 19 complete
Last activity: 2026-06-23 — Phase 19 marked complete

Progress: [██        ] 28%

## Accumulated Context

### Decisions

- Latest recorded loan state snapshot wins for current-value calculations
- Initial loan setup values are immutable
- Existing loans are auto-seeded with an initial state entry
- Loan state history is stored inside `BtcLoanDetails` JSON
- Seeded `CurrentTotalDebt` must match the `LoanStartDate` effective date (`LoanAmount + Fees` or `FixedTotalDebt`)
- Missing `EffectiveDate` or `LoanStartDate` in persisted snapshots is invalid and must throw
- Persisted dates are parsed with `CultureInfo.InvariantCulture`
- Duplicate effective dates in the constructor-supplied snapshot list are invalid
- `UpdateLoanStateViewModel` falls back to `AssetDTO` setup values when no loan-state snapshot exists
- `FiatValue` properties on the update modal use `[Required]` only; `[Range]` is omitted because `FiatValue` does not implement `IComparable`
- English-only localization for new strings in Phase 08; pt-BR/es translations for UpdateLoanState_* and Assets_UpdateLoanState keys are added in Phase 08 gap closure (revised D-07) instead of deferred to Phase 10
- `language.Designer.cs` is maintained manually in this environment because `PublicResXFileCodeGenerator` does not run on Linux builds
- [Phase ?]: Created a dedicated SatsLabel key instead of reusing Reports.Statistics.MedianExpensesSatsLabel because the existing key is a full chart label, not a unit suffix.
- [Phase ?]: Used 'sats' as the universal pt-BR/es translation, matching the project's existing Bitcoin terminology.
- [Phase 10-polish-verification]: Wrapped DateOnly.ParseExact in try/catch per threat model T-10-02-01 — Returns clean error messages instead of propagating FormatException to MCP clients for invalid yyyy-MM-dd input
- [Phase 10-polish-verification]: Force-added gitignored 09-VERIFICATION.md and 09-UAT.md — Preserves Phase 9 runtime verification evidence in git history alongside tracker update, consistent with tracked 08-VERIFICATION.md/08-UAT.md
- [Phase 11-safe-fire-and-forget]: Migrated all existing SafeFireAndForget call sites during Task 2 to keep the build green, not only TaskExtensions.cs
- [Phase 11-safe-fire-and-forget]: Used a custom TestLogger instead of ILogger.Received() because LogError and LogDebug are extension methods that NSubstitute cannot intercept
- [Phase ?]: Tasks 1 and 2 already satisfied by 11-01; only TransactionEditor lifecycle moved in 11-02. — Verification of existing call sites showed all SafeFireAndForget replacements and constructor injections were already in place from Plan 11-01.
- [Phase ?]: Preserved BackgroundJobManager.StopAll() in OnShutdownRequested before awaiting MainViewModel.OnClosingAsync to avoid shutdown regression
- [Phase ?]: Used explicit HandleRemovedCurrenciesAsync helper because IFireAndForgetTaskRunner.RunAsync accepts Task, not Func<Task>
- [Phase ?]: Retained intentional async void OnShutdownRequested because Avalonia ShutdownRequestedEventArgs has no deferral and the plan requires this signature
- [Phase ?]: Grouped HTTP consumers into five named client profiles (GitHubApi, CoinGecko, Indicator, PriceProvider, UpdateDownload) to keep the registry small while preserving timeouts and headers. — Avoids an unbounded named-client registry while keeping timeouts and headers centralized.
- [Phase ?]: Added HttpClientTestFactory in 13-01 rather than deferring to 13-02 because provider constructor changes broke test compilation and blocked the solution build. — Necessary Rule 3 auto-fix to keep dotnet build Valt.sln green after constructor signature changes.
- [Phase 13]: Reused PriceProvider named client for AboutViewModel donation-address fetch to avoid adding a sixth named client — AboutViewModel loads donation addresses from a short raw URL; the 30 s PriceProvider timeout is acceptable and keeps the named-client registry small
- [Phase 13]: Kept parameterless design-time constructors in UpdateIndicatorViewModel and AboutViewModel with null-forgiving IHttpClientFactory defaults — Avalonia XAML designer requires parameterless constructors; null-forgiving assignment keeps nullable analysis happy
- [Phase 14-01]: Moved notification abstractions (INotification, INotificationPublisher, INotificationHandler<>) to Valt.App.Kernel.Notifications so App-layer command handlers can publish events without referencing Valt.Infra
- [Phase 14-01]: Retained Valt.Infra.Kernel.Notifications namespace for NotificationPublisher concrete class and handlers to minimize churn
- [Phase 14-01]: Updated LayerDependencyTests.Notifications_Should_Not_Be_Abstract to scan both AppAssembly and InfraAssembly using NetArchTest.Types.InAssemblies
- [Phase 14-02]: Used real BackgroundJobManager with a fake IBackgroundJob in handler tests because BackgroundJobManager is sealed and cannot be substituted with NSubstitute
- [Phase 14-02]: Updated AGENTS.md Background Jobs table to reflect the new 120-second GoalProgressUpdaterJob interval so project guidance stays in sync
- [Phase 14-03]: App command handlers must not reference BackgroundJobManager or IGoalProgressState; they publish GoalProgressUpdateRequested via INotificationPublisher
- [Phase 14-03]: Infra handler GoalProgressUpdateRequestedHandler owns MarkAsStale() + TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater)
- [Phase 14-03]: CopyGoalsFromLastMonthHandler publishes notification only when copiedCount > 0
- [Phase 14-03]: Reused real BackgroundJobManager + fake IBackgroundJob test harness for GoalProgressUpdateRequestedHandlerTests because BackgroundJobManager is sealed
- [Phase ?]: Explicit EnsureIndexes() call in ChangeDatabasePassword — The acceptance criterion requires ChangeDatabasePassword to contain a call to EnsureIndexes(). Although OpenDatabase(filePath, newPassword) already invokes EnsureIndexes(), adding an explicit call keeps the method self-describing and satisfies the criterion.
- [Phase ?]: Preserve existing index inventory exactly — Moving rather than modifying indexes guarantees no regression in query behavior or index coverage. New indexes are out of scope for this hardening phase.
- [Phase ?]: Queried LiteDB $indexes system collection — LiteDB 5.0.21 ILiteCollection<T> does not expose GetIndexes(); used $indexes system collection instead.
- [Phase ?]: Asserted BSON-mapped index expressions — LiteDB persists BSON-mapped field names in $indexes, so assertions use mapped names rather than C# property names.
- [Phase 17-transaction-editor-builder]: Kept ITransactionDetailsBuilder in Valt.UI layer to preserve App-layer dependency boundary and keep form-state records with presentation concerns.
- [Phase ?]: Preserved TransactionEditorViewModel parameterless design-time constructor unchanged so Avalonia designer continues to work. — Avoids breaking Avalonia XAML designer instantiation.
- [Phase ?]: Kept the asset form builder in Valt.UI to preserve the App-layer dependency boundary
- [Phase 18-manage-asset-builder]: Accepted 18-01 implementation as complete — All acceptance criteria in 18-02-PLAN.md are satisfied by the existing commits from Plan 18-01; no additional source changes were required for Plan 18-02.
- [Phase 18-manage-asset-builder]: Deferred live-API test failures to Phase 25 — Two full-suite failures (BitcoinDominanceProviderTests and CoinGeckoProviderTests) are pre-existing network/TEST-01 debt scheduled for Phase 25 and are unrelated to the asset form builder refactor.

### Blockers

(None)

### Concerns / Carried Debt

(None — Phase 09 runtime UI checks were completed and passed during Phase 10 end-to-end verification; see 09-UAT.md and 09-VERIFICATION.md.)

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|---|------|--------|-----------|
| 260616-rcu | Fix Stock asset edit modal not loading stored acquisition date | 2026-06-16 | be0b8b3 | [260616-rcu-fix-stock-asset-edit-modal-not-loading-s](./quick/260616-rcu-fix-stock-asset-edit-modal-not-loading-s/) |
| 260619-fix-loan-snapshot-edit | Fix editing original BTC loan data after snapshots exist | 2026-06-19 | f8bdf78 | [260619-fix-loan-snapshot-edit](./quick/260619-fix-loan-snapshot-edit/) |
| 260628-pgi | Fix NullReferenceException in BtcInput when copying a transaction | 2026-06-28 | d2f8685 | [260628-pgi-fix-nullreferenceexception-in-btcinput-w](./quick/260628-pgi-fix-nullreferenceexception-in-btcinput-w/) |

### Todos

(None)

### Completed Plans

- 06-01 — LoanStateSnapshot value object & immutable storage
- 06-02 — Snapshot-driven calculations & query consumer verification
- 06-03 — Snapshot persistence & legacy auto-seeding
- 06-04 — Domain & serializer test coverage
- 06-05 — Close serializer & domain validation gaps
- 06-06 — Correct & extend test coverage
- 08-01 — Update Loan State screen modal, wiring, and ViewModel tests
- 08-02 — Localize Update Loan State context menu, modal labels, and validation message keys
- 08-03 — Fix Current Loan Context refresh, localized validation messages, and modal layout
- 09-01 — Build Loan State History modal, ViewModel, DI registration, and localization
- 09-02 — Wire Assets tab context menu and Update Loan State "View History" link
- 09-03 — Add LoanStateHistoryViewModel unit tests
- 11-01 — Introduce IFireAndForgetTaskRunner, replace SafeFireAndForget extension, register in DI, add runner tests
- 13-01 — Register IHttpClientFactory and named clients; migrate infrastructure HTTP consumers
- 13-02 — Refactor UI ViewModels and update provider/crawler tests for factory-based clients
- 14-01 — Move notification abstractions from Valt.Infra to Valt.App; update all consumers and architecture tests
- 14-02 — Wire Infra goal domain-event handlers to trigger GoalProgressUpdaterJob and raise interval to 120s
- 14-03 — Bridge App command handlers to Infra job triggering via GoalProgressUpdateRequested notification

## Session Continuity

Last session: 2026-06-23T15:05:33.570Z
Stopped at: Completed 18-02-PLAN.md
Resume file: None

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 08 P03 | 2 min | 2 tasks | 2 files |

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 10 | 4 plans | - | planned localization, MCP audit, docs, verification |
| Phase 10-polish-verification P01 | 12min | 3 tasks | 5 files |
| Phase 10-polish-verification P02 | 18 min | 2 tasks | 2 files |
| Phase 10-polish-verification P03 | 12min | 3 tasks | 1 files |
| Phase 10-polish-verification P04 | 9min | 3 tasks | 5 files |
| Phase 11 P01 | 20 | 3 tasks | 13 files |
| Phase 11-safe-fire-and-forget P02 | 11min | 3 tasks | 2 files |
| Phase 11 P03 | 8min | 3 tasks | 8 files |
| Phase 13 P01 | 7min | 3 tasks | 24 files |
| Phase 13 P02 | 2min | 3 tasks | 2 files |
| Phase 14 P01 | 9min | 3 tasks | 63 files |
| Phase 14 P02 | 13min | 3 tasks | 9 files |
| Phase 14 P03 | 5min | 3 tasks | 7 files |
| Phase 16 P01 | 4min | 2 tasks | 2 files |
| Phase 16 P02 | 8min | 2 tasks | 2 files |
| Phase 17-transaction-editor-builder P01 | 3min | 3 tasks | 4 files |
| Phase 17-transaction-editor-builder P02 | 4min | 3 tasks | 2 files |
| Phase 18-manage-asset-builder P01 | 15min | 3 tasks | 8 files |
| Phase 18-manage-asset-builder P02 | 5min | 3 tasks | 0 files |
