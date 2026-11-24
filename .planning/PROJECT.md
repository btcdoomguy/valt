# Valt

## What This Is

Valt is a personal budget management desktop application for bitcoiners, built with .NET and Avalonia UI. It tracks fiat and bitcoin accounts, transactions, external investments (assets), and displays values in bitcoin terms. As of v0.5, users can also mark assets as sold, keep a record in a dedicated Asset Sold History screen, and undo a sale to restore the asset to the active view. As of v0.6, the public Valt documentation site has been refreshed to reflect all shipped features, correct factual errors, and add missing pages including Settings & Configuration.

## Core Value

Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## Requirements

### Validated

- ✓ Track fiat and bitcoin accounts — Phase 1-3
- ✓ Record transactions between accounts and categories — Phase 1-3
- ✓ Manage asset groups and external investments (BTC, stocks, ETFs, real estate, etc.) — Phase 1-3
- ✓ BTC-backed loans with collateral, APR, LTV, and liquidation tracking — Phase 1-3
- ✓ Reports dashboard with wealth, leverage, and BTC loan summaries — Phase 1-3
- ✓ Custom BTC price simulation for reports — Phase 4-5
- ✓ Allow users to record BTC loan state changes over time (fee %, current debt, collateral, amount taken, effective date) — Phase 7-9
- ✓ Use the latest recorded loan state as the basis for current-value calculations, falling back to previous entries when the latest is deleted — Phase 6-7
- ✓ Provide an "Update Loan State" screen prefilled with current calculated totals, accessible from the Assets tab context menu — Phase 8
- ✓ Provide a loan state history screen listing all recorded changes with delete and add-new-state actions — Phase 9
- ✓ Auto-seed existing loans with an initial state entry derived from their setup values — Phase 6
- ✓ Localize all new user-facing strings across `language.resx`, `language.pt-BR.resx`, and `language.es.resx` — Phase 10
- ✓ Expose new loan-state commands/queries through MCP `AssetTools` — Phase 10
- ✓ Update `.claude/docs/assets.md` with loan state timeline behavior — Phase 10
- ✓ Complete end-to-end verification of add, update, delete, and fallback flows — Phase 10
- ✓ **HTTP-01**: `IHttpClientFactory` is registered in DI and all `new HttpClient()` sites use named/typed clients — Phase 13
- ✓ **HTTP-02**: Price providers and update checkers share a consistent HTTP client lifetime and configuration — Phase 13
- ✓ **HTTP-03**: Existing provider tests continue to pass against the factory-based clients — Phase 13
- ✓ Add a `Sold` flag and `Date Sold` to assets — v0.5 (Phase 29)
- ✓ Hide sold assets from the main Assets view and exclude them from totals/calculations — v0.5 (Phase 29)
- ✓ Provide Undo Sell action to restore the asset to the active view — v0.5 (Phase 29)
- ✓ Prompt for Date Sold when not provided during the sell action — v0.5 (Phase 29)
- ✓ Add a History button on the Assets toolbar to access sold assets — v0.5 (Phase 30)
- ✓ Build a History screen listing sold assets with their sale date — v0.5 (Phase 30)
- ✓ Show per-type asset details summary in the History screen when an asset is selected — v0.5 (Phase 30)
- ✓ Undo Sell action restores the asset to the active view and refreshes the Assets tab — v0.5 (Phase 30)
- ✓ **MCP-01**: AI assistant can mark an asset as sold via MCP tool — v0.5 (Phase 31)
- ✓ **MCP-02**: AI assistant can undo a sale via MCP tool — v0.5 (Phase 31)
- ✓ **MCP-03**: AI assistant can list sold assets via MCP tool — v0.5 (Phase 31)
- ✓ **DOCS-01**: All new Asset Sold History user-facing strings are localized in `language.resx`, `language.pt-BR.resx`, and `language.es.resx` — v0.5 (Phase 31)
- ✓ **DOCS-02**: `.claude/docs/assets.md` updated with sold-history behavior and MCP impact — v0.5 (Phase 31)
- ✓ Complete end-to-end verification of mark sold, history browse, details panel, undo, and totals refresh — v0.5 (Phase 31)
- ✓ **FXE-01**: Fixed Expenses page documents record states (Paid, ManuallyPaid, Ignored, Empty) — v0.6 (Phase 36)
- ✓ **FXE-02**: Fixed Expenses page documents the yearly overview and out-of-range detection — v0.6 (Phase 36)
- ✓ **FXE-03**: Fixed Expenses page clarifies account-vs-currency binding exclusivity — v0.6 (Phase 36)
- ✓ **MCP-01**: MCP Server page documents the AssetTools category (28 tools) — v0.6 (Phase 37)
- ✓ **MCP-02**: MCP Server page documents loan-state tools with parameters — v0.6 (Phase 37)
- ✓ **MCP-03**: MCP Server page documents sold-asset tools with parameters — v0.6 (Phase 37)
- ✓ **NAV-01**: `mkdocs.yml` navigation updated to include new Settings page — v0.6 (Phase 38)
- ✓ **NAV-02**: New page titles consistent in Portuguese and English navigation translations — v0.6 (Phase 38)
- ✓ **NAV-03**: Settings & Configuration page created and linked — v0.6 (Phase 38)
- ✓ **QA-01**: Every Portuguese content change mirrored in English `.en.md` files — v0.6 (Phases 32-38)
- ✓ **QA-02**: Documentation site builds strictly with no errors or broken internal links — v0.6 (Phases 32-38)
- ✓ **QA-03**: Content review checklist applied to all updated pages — v0.6 (Phase 38)
- ✓ **SIM-01**: BTC loan simulator UI exposes input loan parameters (collateral BTC, amount taken, liquidation LTV, start/end dates, interest rate, fees) — v0.8 (Phase 45)
- ✓ **SIM-02**: BTC loan simulator UI supports simple or compound interest mode selection — v0.8 (Phase 45)
- ✓ **SIM-03**: BTC loan simulator supports simple or compound interest mode selection — v0.8 (Phase 44)
- ✓ **SIM-04**: BTC loan simulator simple-interest uses act/365 parity with existing loan math; compound mode uses daily accrual — v0.8 (Phase 44)
- ✓ **SIM-05**: BTC loan simulator results panel shows total to repay, interest/fees breakdown — v0.8 (Phase 45)
- ✓ **SIM-06**: BTC loan simulator results panel shows fiat and sats values with conversion basis — v0.8 (Phase 45)
- ✓ **SIM-07**: BTC loan simulator results panel shows liquidation BTC price — v0.8 (Phase 45)
- ✓ **SIM-08**: BTC loan simulator results panel shows effective fee-inclusive APR — v0.8 (Phase 45)
- ✓ **SIM-09**: BTC loan simulator results panel shows distance to liquidation as signed percentage with color coding — v0.8 (Phase 45)
- ✓ **GOL-05**: Contributing-transaction rows carry date, description, account, category, and amount in fiat and sats — v0.9 (Phase 49)
- ✓ **GOL-06**: Rows carry a running total accumulated per the goal's progress calculation, reconciling exactly with displayed progress — v0.9 (Phase 49)
- ✓ **GOL-07**: Contributing-transaction set is derived from each goal type's progress strategy (App-layer query) — v0.9 (Phase 49)
- ✓ **GOL-03**: "View summary" right-click context menu on goal entries opens the Goal Summary modal (hidden for NetWorthBtc, disabled in secure mode) — v0.9 (Phase 50)
- ✓ **GOL-04**: Modal shows a read-only grid of all contributing transactions with fiat + sats amounts and reconciling running total — v0.9 (Phase 50)
- ✓ **GOL-08**: AI assistant can query a goal's contributing transactions via the `GetGoalContributingTransactions` MCP tool (typed Supported/Error contract) — v0.9 (Phase 51)
- ✓ **GOL-09**: All Goal Summary strings localized in en-US, pt-BR, es with Designer parity — v0.9 (Phase 51)
- ✓ **GOL-10**: `.claude/docs/goals.md` documents the goal summary feature and MCP tool — v0.9 (Phase 51)

### Active

None — all milestones through v0.11 shipped. The next milestone will be scoped from `.planning/research/NEW-FEATURES.md` (5 candidates: realized gains & tax-lot reporting, on-chain watch-only sync, UTXO/coin control, DCA analytics, budget planner & forecasting).

## Out of Scope

- v0.4 quality/hardening items (async void cleanup, HttpClient factory, background job throttling, LiteDB index centralization, god-VM refactor, live-API test isolation, handler unit tests) — still deferred to a future quality milestone; v0.6 did not address app code
- Recording sale price, capital gains, or tax lot information — out of scope for v0.5; sold history is for record-keeping only
- Real-time price streaming or automated sell detection — manual sale action only
- Mobile or web port — not in scope
- Changes to the Valt application code itself — v0.6 was documentation-only; no new app features

## Current State

- **v0.5 Asset Sold History shipped** on 2026-07-14.
- **v0.6 Documentation Site Refresh shipped** on 2026-07-17 — all public Valt docs updated through v0.5 features, factual errors corrected, missing pages added (Settings & Configuration), navigation updated, and a full content review checklist applied to 18 documentation files.
- **Deferred items:** 7 (see STATE.md Deferred Items) — v0.4 quality/hardening work and three debug sessions/quick tasks carried forward.
- **v0.6 milestone complete** — 7 of 7 phases, 17 of 17 plans, 42 tasks.
- **Phase 41 (WLT-04) complete** — asset-aware All Time High report with active net-worth assets and a "days under water" row in the existing ATH dashboard panel.
- **Phase 44 complete** — core BTC loan simulation calculator with simple/compound interest engine, liquidation price, effective APR, and monthly schedule.

- **Phase 45 complete** — BTC Loan Simulator modal UI with inputs/results panels, live recalculation, fiat/sats formatting, liquidation price, effective APR, and distance-to-liquidation color coding.
- **Phase 49 complete** — `GetGoalContributingTransactionsQuery` backend: 9 transaction-based goal strategies expose their contributing transactions with reconciling running totals (only `NetWorthBtc` unsupported); 1804 tests green; code review findings CR-01/WR-01/WR-02 fixed.
- **Phase 50 complete** — Goal Summary modal UI: "View summary" context menu (allow-list gating, secure-mode disable), chromeless resizable modal with 7-column read-only grid, per-unit running-total formatting, header strip with focal reconciled total; human visual sign-off approved; 1818 tests green.
- **Phase 51 complete** — `GetGoalContributingTransactions` MCP tool with typed Supported/Error contract, pt-BR/es localization (1048-key tri-locale parity), `.claude/docs/goals.md` update; end-to-end human sign-off approved; 1822 tests green. **v0.9 milestone complete** (3 phases, 11 plans, 8/8 requirements GOL-03..GOL-10).
- **Phase 53 complete** (2026-10-27) — per-transaction "Excluded from reports and goals" flag: checkbox in the Transaction Editor right panel, exclusion from all Reports income/expense aggregations and all 9 goal progress strategies (semantic boundary: balances/net-worth/ATH/avg-price/snapshots untouched), report-cache invalidation on toggle, MCP surface (6 `excluded` params + `SetTransactionExcluded`), trilingual localization. Gap closure fixed a LiteDB legacy-document predicate bug (`!x.IsExcluded` → `($.excl = false)` dropped pre-phase-53 documents; rewritten null-safe). 6/6 plans, 1921 tests green; human UAT passed.
- **Phase 55 complete** (2026-11-23) — Codebase Performance, Architecture & Readability Refactoring: fixed 6 correctness and concurrency bugs across AvgPrice, TransactionTermService, MCP forwarding, and CoinGeckoRateLimiter; optimized Core/App allocations (`FiatCurrency` static dictionary, $O(N)$ `LinkedList<CostLot>` FIFO, $O(1)$ `BtcLoanDetails` effective snapshot, reflection-free generic CQRS/notification dispatchers); added LiteDB indexes (`IsExcluded`, `IsSold`, `StateId`), batched N+1 queries, and single-pass `BtcLoanDetailsDto` JSON deserialization; fixed UI native resource/timer/messenger/DataContext leaks, filtered rate-tick reload storms, and debounced transaction search; aligned ViewModels and MCP `ReportTools` with `Valt.App` CQRS dispatchers and UI analytics exclusions; deduplicated category reports, chart data base classes, CQRS command helpers, and `ConfigurationManager` CSV persistence. 6/6 plans, 1948 tests green.
- **Phase 56 complete** (2026-11-24) — Bitcoin 4-Year Cycle Spiral Chart in Price History: added `BitcoinCycleSpiralCalculator` (1,461-day polar angle mapping from `2009-01-01`, 10-decade $\log_{10}$ radial scaling from `$0.001` to `$1M`, halving milestone detection, and nearest-point hit-testing), `BitcoinCycleSpiralChartControl` custom Avalonia `Control` (concentric log10 price rings, 8 radial spokes, cardinal year labels, $+90^\circ$ rotated price labels, Bitcoin-orange spiral trajectory, halving markers, legend, and clamped hover tooltip), `PriceHistoryViewModel`/`PriceHistoryView.axaml` segmented mode toggle (`Line Chart (5Y)` vs `4-Year Cycle Spiral`), trilingual localization (`en-US`, `pt-BR`, `es`), and 10 NUnit unit tests. 1/1 plans, 1958 tests green.

## Current Milestone: v0.9 Goal Transparency — Transaction Breakdown

**Goal:** Let users see exactly which transactions feed into any goal's total calculation, for deeper analysis and trust in the numbers.

**Target features:**
- "View summary" right-click context menu item on every goal entry in the Goals section (all goal types)
- Modal window with a read-only grid of all contributing transactions: date, description, account, category, amount (fiat + sats), plus a running total column
- Goal-progress strategies expose their contributing transactions through a new App-layer query
- MCP tool for AI assistants to query contributing transactions per goal
- Localization in all 3 languages (en, pt-BR, es)

## Next Milestone Goals

The next milestone will be scoped from `.planning/research/NEW-FEATURES.md`, researched 2026-11-21. Five candidates (in recommended order):

1. Realized gains & tax-lot reporting (builds on AvgPrice cost basis)
2. Budget planner & cash-flow forecasting (recurring income + forward projections)
3. On-chain watch-only wallet sync + UTXO / coin control & privacy labeling
4. DCA analytics & backtesting (builds on DcaGoalType + price history)

_Deferred quality backlog (carried, separate from feature work): v0.4 hardening items (async void cleanup, god-VM refactor, live-API test isolation, handler unit tests)._

## Context

Valt uses a layered architecture: Valt.Core (domain), Valt.App (CQRS), Valt.Infra (LiteDB persistence), and Valt.UI (Avalonia). The v0.4 milestone addressed quality and performance hardening. v0.5 introduced a user-facing Asset Sold History feature that lets users retain records of disposed assets without losing them entirely. The feature spans the domain (sold-state properties), application (commands and queries), infrastructure (persistence and price-update filtering), UI (History modal and Date Sold prompt), MCP tooling, localization, and documentation.

## Constraints

- **Tech stack**: .NET, Avalonia UI, LiteDB, CommunityToolkit.Mvvm — changes must fit existing patterns
- **Test framework**: NUnit with NSubstitute; builders and `DatabaseTest`/`IntegrationTest` bases must remain supported
- **Backward compatibility**: Existing user databases and price databases must continue to work without migration
- **Risk management**: UI restructuring must be done in vertical slices (VM + XAML + tests) to avoid broken bindings

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Latest recorded loan state snapshot wins for calculations | Matches user expectation that manually recorded current state overrides initial setup | Implemented in `BtcLoanDetails` calculations; `AssetQueries` selects `MaxBy(s => s.EffectiveDate)` — Phase 6-7 |
| Immutable initial loan setup values | Prevents accidental edits that would corrupt historical calculations | Initial setup fields remain unchanged; state snapshots are appended separately — Phase 6 |
| Auto-seed existing loans on load/migration | Avoids a one-shot migration script and keeps every loan queryable through the same timeline model | `AssetDetailsSerializer` seeds a snapshot from setup values when deserializing legacy loans — Phase 6 |
| Store history inside `BtcLoanDetails` JSON | Reuses existing `AssetDetailsSerializer` mechanism; avoids new collection complexity | Snapshots persisted as JSON array inside `BtcLoanDetailsDto` — Phase 6 |
| Two-step ViewModel simplification (services first, child-VMs second) | Builder extraction is low-risk and testable; child-VM restructuring requires XAML changes and carries binding risk | Phases 17-20 extract services; Phases 21-24 restructure VMs and XAML |
| Skip research for v0.4 | No new features or external integrations; scope is refactor and hardening of existing code | — |
| Add `IsSold` and `DateSold` as first-class properties on the `Asset` aggregate and `AssetEntity` | First-class properties keep sold-state behavior explicit and queryable; avoids hidden JSON blob semantics | Domain, persistence, and tests all use `IsSold`, `DateSold`, `PreviousVisibility` — v0.5 (Phase 29) |
| Filter sold assets at the query layer (`IAssetQueries`) | Centralizes semantics so the UI, reports, MCP, and background jobs all share the same active/sold definition | `GetAllAsync` returns active assets only; `GetSoldAsync` returns sold assets — v0.5 (Phase 29) |
| Reuse the existing `AssetViewModel` and `AssetDTO` mapping for the History details panel | Avoids duplicating per-type layout logic and keeps the History card consistent with the main asset card | `SoldAssetDetailsCard` binds to `AssetViewModel` — v0.5 (Phase 30) |
| Keep `IsSold` independent from `Visible` | Ensures undo restores the prior visibility state rather than conflating sold-state with visibility | `MarkAsSold` captures `PreviousVisibility`; `UndoSale` restores `Visible` — v0.5 (Phase 29) |
| Keep `Asset.New` backward-compatible with optional sold-state defaults | Existing call sites continue to compile without modification | `isSold: false`, `dateSold: null`, `previousVisibility: true` defaults — v0.5 (Phase 29) |
| Reuse `AssetUpdatedEvent` for sold-state changes | Avoids expanding the event surface in the foundational plan | `MarkAsSold` and `UndoSale` emit `AssetUpdatedEvent` — v0.5 (Phase 29) |
| Store `DateSold` as `DateOnly?` directly on `AssetEntity` | Matches the domain model; round-trip test proves LiteDB serialization | Date-only serialization verified — v0.5 (Phase 29) |
| Inject `IClock` into `MarkAssetAsSoldValidator` and `MarkAssetAsSoldHandler` | Keeps future-date rejection and default-to-today behavior testable and deterministic | Unit tests use `FakeClock` — v0.5 (Phase 29) |
| Place the `AssetPriceUpdaterJob` sold-asset skip guard in `ShouldUpdatePrice` | Keeps the `IAssetRepository` contract stable and the data-fetch behavior consistent with other consumers | `ShouldUpdatePrice` returns `false` when `asset.IsSold` — v0.5 (Phase 29) |
| Return structured dialog result from `SoldAssetHistory` modal | Caller-side refresh is explicit and avoids coupling `AssetsViewModel` to internal modal messages | `WasRestored` flag drives active-list refresh — v0.5 (Phase 30) |
| Use `SoldAssetHistory_DateSold_Title` with a literal colon via `Run` elements | Avoids an unplanned extra resource key and matches the existing Title+colon pattern | `_Label` key removed; 20-key set preserved — v0.5 (Phase 31) |
| Document public docs terminology from app language files to avoid UI/docs drift | Keeps English/Portuguese public docs aligned with the actual Valt UI strings | `.claude/docs/assets.md` and ROADMAP.md updated to match `language.resx`/`language.pt-BR.resx` — v0.6 (Phase 32) |
| Code wins over stale planning docs when conflicts arise | Ensures planning artifacts remain authoritative and accurate for future phases | Updated REQUIREMENTS.md, ROADMAP.md, and `.claude/docs/assets.md` to match code reality — v0.6 (Phase 32) |
| Use exact app-verbatim labels in documentation pages | Prevents drift between the UI and public docs; uses the app's language files as the source of truth | Fixed Expenses and Settings pages use labels directly from `language.resx`/`language.pt-BR.resx` — v0.6 (Phases 36, 38) |
| Replace drifted MCP tool tables as whole units from code-verified research | Full replacement removes stale descriptions and adds 32 missing real tools, landing documented count at 90 = code truth | 7 pre-existing category tables replaced in both languages; 28 phantom names eliminated — v0.6 (Phase 38) |
| Apply a 12-check content review matrix to all v0.6 documentation files | Satisfies QA-03 and creates an auditable artifact for future docs work | `38-QA-CHECKLIST.md` created with 18 files × 12 checks — v0.6 (Phase 38) |
| Reused `IsBtcMetricsEmpty` for both monthly and category breakdown views | Avoids a second empty-state property and keeps the existing XAML border binding unchanged | `ReportsViewModel` toggles `IsBtcMetricsEmpty` between `Months.Count` and `SpentByCategory.Count` — Phase 40 |
| Cache `_lastBtcMetricsData` for category/monthly toggle empty-state recompute | Switches views instantly without a database round-trip | Toggle handler recomputes empty state from cached DTO — Phase 40 |
| Implement BTC loan simulation as a pure static calculator in `Valt.Core` | Keeps the engine free of DI, persistence, and UI dependencies so downstream UI/MCP phases consume it without reimplementing math | `BtcLoanSimulationCalculator` is a public static class in `Valt.Core.Modules.Assets.Simulation` — Phase 44 |
| Use a daily `decimal` loop for compound interest instead of `Math.Pow` | Avoids floating-point drift and keeps totals reconcilable with generated schedule rows | Compound path computes `dailyInterest = runningPrincipal * apr / 365m` and adds it to both accrued interest and running principal; fees are excluded — Phase 44 |
| Extend `IGoalProgressCalculator` with `GetContributingTransactionsAsync` returning per-strategy rows; aggregates re-derived as row sums | Selection logic stays in one place per strategy and cannot drift from the progress math — Phase 49 |
| `BitcoinHodl` is transaction-based (progress sums `BitcoinToFiat` sale fiat-leg) — included as 9th supported type; only `NetWorthBtc` is `NotSupported` | Code-verified correction of the smart-discuss assumption; user-confirmed | Sale rows read `ToFiatAmount`/`ToAccountId` (production mapping nulls `FromFiatAmount`) — Phase 49 |
| Amount fields on `ContributingTransactionRow` are non-negative magnitudes; sign lives only in `Contribution`/`RunningTotal` | `BtcValue` core invariant rejects negative sats; magnitude contract matches all existing assertions | `FiatAmount`/`SatsAmount` absolute; sign on Contribution and RunningTotal deltas — Phase 49 |
| Query awaited before modal opens; `CanViewSummary` is an allow-list of the 9 supported goal-type DTOs | Modal opens fully populated (no loading chrome); a future goal type can't silently fall into a misleading empty state | `ViewSummaryCommand` no-ops on `NotSupported`; NetWorthBtc hidden via allow-list — Phase 50 |
| MCP tool returns typed `Supported=false` for NetWorthBtc and typed `Error` on query failure | `null` means only "goal not found"; MCP clients can distinguish all three outcomes (code review WR-01) | `GetGoalContributingTransactionsMcpResult.Error` + failure-path test — Phase 51 |
| Replace per-call reflection `MethodInfo.Invoke` in CQRS dispatchers and `NotificationPublisher` with cached generic wrapper instances | Eliminates `object[]` allocations, value-type boxing, and reflection overhead on every command, query, and notification dispatch | `CommandDispatcher`, `QueryDispatcher`, and `NotificationPublisher` use `ConcurrentDictionary<Type, ...>` wrapper caches — Phase 55 |
| Use `LinkedList<CostLot>` with running totals in `FifoCalculationStrategy` | Avoids $O(N^2)$ `Queue<T>` array rebuilds on partial sells and repeated LINQ enumerations across lines | Head node updated in $O(1)$ on partial sell; `remainingQty` and `remainingCost` maintained incrementally — Phase 55 |
| Route ViewModels and MCP `ReportTools` exclusively through `Valt.App` CQRS dispatchers and shared exclusion/wealth filters | Eliminates direct repository / `ILocalDatabase` dependencies in ViewModels and keeps MCP financial reports aligned with UI analytics exclusions and asset wealth snapshots | 5 ViewModels migrated to CQRS commands/queries; `ReportTools` applies `GetReportsAnalyticsCategoryFilterExcludedIds()` and `AssetsTotalFiat` — Phase 55 |
| Decouple 4-year cycle polar/log10 spiral math (`BitcoinCycleSpiralCalculator`) from custom Avalonia rendering (`BitcoinCycleSpiralChartControl`) | Enables deterministic headless NUnit verification of polar angle mapping, 10-decade $\log_{10}$ scaling, halving detection, and spatial hit-testing without requiring an Avalonia display server | `BitcoinCycleSpiralCalculator` is a pure static class; `BitcoinCycleSpiralChartControl` caches `StreamGeometry` and uses static `IImmutableSolidColorBrush`/`ImmutablePen` instances — Phase 56 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-11-24 after Phase 56 complete (Bitcoin 4-Year Cycle Spiral Chart in Price History)*
