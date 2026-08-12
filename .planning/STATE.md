---
gsd_state_version: 1.0
milestone: v0.7
milestone_name: Insights & Metrics Expansion
current_phase: 42
current_phase_name: Loans & Leverage Reports & UI
status: executing
stopped_at: Phase 42 context gathered
last_updated: "2026-08-12T00:05:20.095Z"
last_activity: 2026-08-11
last_activity_desc: Phase 42 execution started
progress:
  total_phases: 5
  completed_phases: 3
  total_plans: 16
  completed_plans: 15
  percent: 62
---

# STATE.md

## Project Reference

See: .planning/PROJECT.md (updated 2026-08-06)

**Core value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.
**Current focus:** Phase 42 — Loans & Leverage Reports & UI

## Current Position

Phase: 42 (Loans & Leverage Reports & UI) — EXECUTING
Plan: 3 of 3
Status: Ready to execute
Last activity: 2026-08-11 — Plan 42-02 complete

## Performance Metrics

**Velocity:**

- Total plans completed: 8
- Average duration: 18 min
- Total execution time: 43 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| Phase 29 | 4/4 | 18 min | 18 min |
| Phase 30 | 3/3 | - | - |
| Phase 31 | 5/5 | - | - |
| Phase 32 | 2/2 | 25 min | 12.5 min |
| 35 | 3 | - | - |
| 36 | 2 | - | - |
| 37 | 2 | - | - |
| Phase 38 | 2/3 | — | — |
| 39 | 8/8 | - | - |
| 40 | 4/4 | 55 min | 13.75 min |
| 41 | 1 | - | - |

**Recent Trend:**

- Last 5 plans: 18 min
- Trend: —

*Updated after each plan completion*
| Phase 29 P02 | 8min | 3 tasks | 9 files |
| Phase 29 P03 | 5min | 3 tasks | 8 files |
| Phase 29 P04 | 5min | 2 tasks | 2 files |
| Phase 30-history-ui-and-details-reuse P01 | 11min | 3 tasks | 9 files |
| Phase 30-history-ui-and-details-reuse P02 | 6 min | 3 tasks | 7 files |
| Phase 30 P03 | 15 | 3 tasks | 2 files |
| Phase 31-mcp-localization-documentation-and-verification P01 | 5min | 2 tasks | 1 files |
| Phase 31 P02 | 20min | 3 tasks | 10 files |
| Phase 31 P03 | 5 min | 1 task | 1 file |
| Phase 31-mcp-localization-documentation-and-verification P04 | 8 | 1 tasks | 2 files |
| Phase 31-mcp-localization-documentation-and-verification P05 | 3 min | 2 tasks | 5 files |
| Phase 32 P01 | 20min | 3 tasks | 11 files |
| Phase 32 P02 | 5 min | 2 tasks | 2 files |
| Phase 33 P01 | 12 min | 2 tasks | 2 files |
| Phase 33 P02 | 5min | 2 tasks | 1 files |
| Phase 33-assets-page-rewrite P03 | 4min | 2 tasks | 2 files |
| Phase 34-reports-page-update P01 | 15 min | 2 tasks | 1 files |
| Phase 34-reports-page-update P02 | 4 min | 2 tasks | 1 files |
| Phase 36-fixed-expenses-page-enhancement P01 | 11 min | 3 tasks | 1 files |
| Phase 36-fixed-expenses-page-enhancement P02 | 3 min | 3 tasks | 1 files |
| Phase 37 P01 | 4 min | 3 tasks | 1 files |
| Phase 37-mcp-server-page-update P02 | 3 min | 3 tasks | 1 files |
| Phase 38 P01 | 16 min | 3 tasks | 9 files |
| Phase 38 P02 | 18 min | - tasks | - files |
| Phase 38 P02 | 18 min | 3 tasks | 2 files |
| Phase 38 P03 | 7 min | 3 tasks | 4 files |
| Phase 39 P01 | 11 min | 3 tasks | 13 files |
| Phase 39 P03 | 10 | 3 tasks | 9 files |
**Per-Plan Metrics:**

| Plan | Duration | Tasks | Files |
|------|----------|-------|-------|
| Phase 40-btc-denominated-metrics-reports-ui P01 | 15 | 3 tasks | 13 files |
| Phase 40-btc-denominated-metrics-reports-ui P02 | 25min | 3 tasks | 7 files |
| Phase 40 P03 | 5min | 1 tasks | 1 files |
| Phase 40 P04 | 10min | 2 tasks | 1 files |
| Phase 41 P01 | 15 | 3 tasks | 7 files |
| Phase 42 P01 | 45 | 3 tasks | 14 files |
| Phase 42 P02 | 55 | 2 tasks | 2 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [v0.5 research]: Add `IsSold` and `DateSold` as first-class properties on the `Asset` aggregate root and `AssetEntity`, not inside the JSON details blob.
- [v0.5 research]: Filter sold assets at the query layer (`IAssetQueries`) so the UI, reports, MCP, and background jobs all share the same semantics.
- [v0.5 research]: Reuse the existing `AssetViewModel` and `AssetDTO` mapping for the History details panel.
- [v0.5 research]: Keep `IsSold` independent from `Visible` so the two concepts do not collide on undo or filtering.
- [Phase 29]: Asset.New kept backward-compatible optional parameters with sold-state defaults — Existing Asset.New call sites must continue to compile without modification. Added optional parameters with active defaults (isSold: false, dateSold: null, previousVisibility: true).
- [Phase 29]: Reused AssetUpdatedEvent instead of introducing new event types — Existing state-change methods (SetVisibility, SetIncludeInNetWorth) emit AssetUpdatedEvent. New MarkAsSold/UndoSale methods follow the same pattern to avoid expanding the event surface in this foundational plan.
- [Phase 29]: DateSold stored as DateOnly? directly on AssetEntity, verified by round-trip test — D-01 specifies DateOnly? on the entity. Research warned that LiteDB BSON natively supports DateTime, not DateOnly. The round-trip repository test proves DateOnly? serializes correctly for this codebase.
- [Phase 29]: Followed SetAssetVisibility command/validator/handler pattern for MarkAssetAsSold and UndoAssetSale
- [Phase 29]: Injected IClock into MarkAssetAsSoldValidator and Handler for future-date rejection and default-to-today behavior
- [Phase ?]: Filtered active vs. sold assets at the IAssetQueries layer so UI, reports, and MCP share the same semantics — Centralizing the filter in the query layer prevents consumers from diverging on sold-state semantics.
- [Phase ?]: Made AssetDTO sold-state fields non-required to avoid breaking existing design-time sample data and with expressions — Non-required init-only properties preserve existing object initializers and record with-expressions that do not set the new fields.
- [Phase ?]: Kept the AssetPriceUpdaterJob sold-asset skip guard in ShouldUpdatePrice so the IAssetRepository contract remains unfiltered — The plan called for an unfiltered repository call with filtering in ShouldUpdatePrice; placing the guard there avoids changing the IAssetRepository contract and keeps the job's data-fetch behavior consistent with other consumers.
- [Phase 30-history-ui-and-details-reuse]: Hardcoded English language strings for the new History UI; Phase 31 will add the corresponding resx entries.
- [Phase 30-history-ui-and-details-reuse]: Reused the existing AssetViewModel and AssetDTO mapping for the details card, keeping NetWorth/Visibility indicators hidden.
- [Phase 30-history-ui-and-details-reuse]: Hardcoded English strings for new user-facing text — Phase 31 will add the corresponding resx entries.
- [Phase 30-history-ui-and-details-reuse]: Combined Tasks 2 and 3 into a single commit to keep the build green — Renaming the command in the ViewModel alone broke the XAML binding until the view was also updated, so both changes were committed together.
- [Phase ?]: Reused CloseDialog typed-result mechanism instead of adding AssetSummaryUpdatedMessage subscription to AssetsViewModel to avoid coupling and refresh loops — Modal result is more explicit and avoids potential infinite loops since AssetsViewModel also sends AssetSummaryUpdatedMessage after its own loads
- [Phase ?]: Left AssetSummaryUpdatedMessage broadcast in modal for other listeners while making caller-side refresh authoritative for active Assets list — Keeps unrelated totals/listeners updated without making the Assets tab depend on the message for its own refresh
- [Phase ?]: Removed the unplanned SoldAssetHistory_DateSold_Label key and used SoldAssetHistory_DateSold_Title with a literal colon via Run elements to match the plan's 20-key list and Title+colon pattern.
- [Phase 31-03]: Updated Asset module docs as a first-class deliverable tied to v0.5 feature completeness, keeping the Domain/Application/UI/MCP sections in sync with the sold-state feature.
- [Phase 31-05]: v0.5 Asset Sold History milestone is ready for `/gsd-verify-work`: all feature tests pass and the user has signed off the end-to-end UI smoke test.
- [Phase ?]: Code wins over stale requirements docs: updated REQUIREMENTS.md ACC-03 and ACC-04 to match the four main tabs and nine asset types found in the Valt application code.
- [Phase ?]: All public docs terminology mirrors the app language files (language.resx / language.pt-BR.resx) to avoid UI/docs drift.
- [Phase ?]: Code wins over stale planning docs: updated ROADMAP.md success criteria and .claude/docs/assets.md to match the four main tabs and nine asset types found in the Valt application code. — When planning docs conflict with the code, correct the docs to reflect the code. This keeps ROADMAP.md success criteria and the internal Assets module doc authoritative for future phases.
- [Phase ?]: Added Record Proceeds prompt note to Mark as Sold section because app language files and AssetsViewModel.cs confirm it is a real wired feature. — Content accuracy: the prompt exists in language.resx / language.pt-BR.resx and is invoked in AssetsViewModel.cs after MarkAssetAsSoldCommand succeeds.
- [Phase ?]: Extended the export note to mention transaction CSV and average-price CSV export from the AvgPrice tab. — CsvExportService exposes both ExportTransactionsAsync and ExportAvgPriceLinesAsync, so the docs should accurately reflect both export paths.
- [Phase ?]: Documented Posições Alavancadas and Empréstimos BTC as conditional panels visible only when user has corresponding data. — ReportsViewModel binds IsLeveragePositionsVisible and IsBtcLoansVisible based on the presence of visible leveraged positions or active BTC-backed loans.
- [Phase ?]: Used exact English UI labels from language.resx for the English Reports page mirror — The plan requires app-aligned terminology; this keeps English docs consistent with the English UI and avoids drift.
- [Phase 36-fixed-expenses-page-enhancement]: Tightened DICAS paragraph to name exact right-click labels Ignorar para essa data and Marcar como pago (old prose covered ignore only vaguely and never mark-as-paid) — Plan optional-tightening clause applied: existing DICAS text did not already cover the right-click mark-as-paid action, so exact labels from language.pt-BR.resx were used while preserving link-to-transaction instructions
- [Phase 36-fixed-expenses-page-enhancement]: [Phase 36-fixed-expenses-page-enhancement] Mirrored wave-1 TIPS tightening to English page (exact labels Ignore for this date / Mark as paid) for full bilingual parity per QA-01 — Wave 1 rewrote the Portuguese DICAS paragraph with exact right-click labels; mirroring only the three structural additions would leave the English TIPS paragraph stale, violating QA-01 full-mirror requirement.
- [Phase ?]: [Phase 37-01]: Appended AssetTools and IndicatorTools sections after CurrencyTools on the Portuguese MCP Server page — Placement discretion D-01: zero disruption to the 8 pre-existing sections; newest categories last matches page evolution.
- [Phase ?]: [Phase 37-01]: Used RESEARCH Code Examples as copy-paste-grade content verbatim for all 28 AssetTools + IndicatorTools rows — D-05 verbatim-from-code rule: the code is the spec; every tool/parameter name grep-verified in-session against AssetTools.cs (28 tools) and IndicatorTools.cs.
- [Phase ?]: [Phase 37-01]: Pre-existing MCP doc name drift (CreateDCAGoal, GetAvgPriceProfiles, GetWealthHistory, CreateAccount, AddBitcoinToBitcoinTransfer) left untouched, logged for Phase 38 QA — D-04 scope boundary: no retrofit or name fixes in pre-existing categories; verified byte-identical via diff.
- [Phase 37]: [Phase 37-02]: Mirrored all wave-1 PT additions to the English MCP Server page with full bilingual parity (QA-01) — EN descriptions track code [Description] attributes per D-05; pre-existing EN name drift untouched per D-04; MCP-01/02/03 already flipped to Complete by wave 1 metadata commit, verified in place per orchestrator instruction
- [Phase 38-01]: Created the Settings & Configuration page (NAV-03) rather than deferring it — the app exposes 10+ undocumented settings across three tabs and three existing pages referenced the settings screen without a link target.
- [Phase 38-01]: Placed the Settings page in the Guide section after Basic Concepts, matching the onboarding flow that ends in app configuration.
- [Phase 38-01]: Repeated the localhost-only MCP security warning verbatim from the existing MCP page in the Avançado / Advanced section, satisfying the T-38-01 threat mitigation.
- [Phase 38-01]: Cross-linked the Seu Arquivo de Dados / Your Data File section to Instalação / Installation instead of duplicating backup/password guidance, satisfying T-38-02.
- [Phase 38-01]: Documented the language combo as listing all system cultures with the three app languages pinned at the top, and the app as translated into three languages (requires restart), matching SettingsViewModel behavior.
- [Phase ?]: Replaced 7 drifted MCP tool tables as whole units from RESEARCH Example 1 — Full replacement removes stale descriptions and adds 32 missing real tools, landing documented count at 90 = code truth
- [Phase ?]: Committed both language files in a single docs(38-02) commit per QA-01 — Bilingual mirror discipline requires PT and EN changes to be committed together
- [Phase ?]: Left 'mais de 80 ferramentas' / '80+ tools' intro untouched — Post-fix documented count is 90, so the existing claim remains true with no count edit
- [Phase ?]: Preserved Categorias/Categories section byte-identical — It was the only clean pre-existing category and required no changes
- [Phase 39]: Savings rate uses AllIncomeInFiat/AllExpensesInFiat from IMonthlyTotalsReport — Matches existing Monthly totals panel numbers and avoids a second income/expense aggregation (D-01).
- [Phase 39]: Burn rate median is read from IStatisticsReport.MedianMonthlyExpenses — Reuses the existing 12-month median calculation instead of recomputing it (D-13).
- [Phase 39]: Burn rate projection is gated to day >= 5 — User-specified threshold to avoid early-month projection noise (D-15).
- [Phase ?]: 39-03: Followed existing BtcLoans/Leverage panel wiring for ReportsViewModel observables, refresh triggers, and disposal
- [Phase ?]: 39-03: Placed burn rate card after Statistics card in DashboardGridPanel per UI-SPEC default ordering
- [Phase ?]: 39-03: Used English-only resx strings (D-21); pt-BR/es files untouched for Phase 43 localization pass
- [Phase ?]: 39-03: Used TransactionGridResources.Credit for projection <= median and Debt for projection > median, matching MonthlyReportItemViewModel convention
- [Phase ?]: 39-03: Kept burn-rate card always visible (IsVisible=true) even on empty/error states, unlike conditional BtcLoans/Leverage panels
- [Phase 40-04]: Reused `IsBtcMetricsEmpty` for both monthly and category breakdown views so the existing XAML empty-state border needs no second property.
- [Phase 40-04]: Cached `_lastBtcMetricsData` for the category/monthly toggle so empty-state recomputes without a database round-trip.
- [Phase ?]: [40-01] Mirrored the SpendingEvolution/SavingsRate module layout for BtcDenominatedMetrics to keep the App/Infra split consistent.
- [Phase ?]: [40-01] Added the Earned/Spent/Velocity series-name language keys in Task 2 so chart-data classes could compile before Task 3 localization.
- [Phase ?]: [40-01] Added error-state observables (IsBtcMetricsError, IsStackVelocityError) in Task 3 to keep the ViewModel wiring task focused on data flow.
- [Phase ?]: [40-01] Left SpentByCategory empty in the query DTO; per-category breakdown intentionally deferred to Plan 40-02.
- [Phase ?]: [40-02] Emit a zero-value month for every month in the date range so the stack velocity line chart has no gaps (D-11).
- [Phase ?]: [40-02] Narrow internal-transfer exclusion to FiatToFiat/BitcoinToBitcoin only, so BTC purchases and sales contribute to stack velocity (D-09).
- [Phase ?]: [40-02] Added English placeholders for new BTC metrics strings to pt-BR and es resx files now; full translations remain Phase 43 work per D-19, but all three language files must contain the keys per AGENTS.md.
- [Phase ?]: Asset value approximation: used AssetDTO.CurrentValue for every active day because historical asset prices are not available; documented as approximation in a code comment.
- [Phase ?]: English-only UI string added now; pt-BR/es localization deferred to Phase 43 per D-14.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260714-exz | The History page displays the description on the grid in a very weird format, check the picture: /home/vmabellini/Pictures/valt3 | 2026-07-14 | 87a83ae | [260714-exz-the-history-page-displays-the-descriptio](./quick/260714-exz-the-history-page-displays-the-descriptio/) |
| 260714-hbv | it is still bad. take a look at /home/vmabellini/Pictures/valt3/new.png. it should align properly. just remove the icon and render the description as plain text. also, the button Restore Asset should not occupy the entire horizontal space | 2026-07-14 | 4607275 | [260714-hbv-it-is-still-bad-take-a-look-at-home-vmab](./quick/260714-hbv-it-is-still-bad-take-a-look-at-home-vmab/) |
| 260714-i1p | when I mark as sold, the system asks for date but only month/year. it should use a date picker like the one on the transaction tab to also pick the day | 2026-07-14 | d79acfa | [260714-i1p-when-i-mark-as-sold-the-system-asks-for-](./quick/260714-i1p-when-i-mark-as-sold-the-system-asks-for-/) |
| 260714-kzm | Update Avalonia nuget packages from 12.0.3 to 12.1.0, research breaking changes first | 2026-07-14 | 36ddfe7 | [260714-kzm-update-avalonia-nuget-packages-from-12-0](./quick/260714-kzm-update-avalonia-nuget-packages-from-12-0/) |
| 260717-iqe | after I close a database, when I try to open another one the app got stuck because all background services are stopped | 2026-07-17 | 6ffdf19 | [260717-iqe-after-i-close-a-database-when-i-try-to-o](./quick/260717-iqe-after-i-close-a-database-when-i-try-to-o/) |
| 260804-f94 | TransactionsView left column: shorten "View All Accounts" to "View All" and move plus-icon add button to a labeled "Add new" button beside it | 2026-08-04 | 29aed14 | [260804-f94-transactions-left-column-buttons](./quick/260804-f94-transactions-left-column-buttons/) |
| 260804-u3u | Add pt-BR and es translations for the new Phase 39 Reports strings (Burn Rate, Savings Rate, Fixed vs Variable) | 2026-08-05 | 502281f | [260804-u3u-add-pt-br-and-es-translations-for-the-ne](./quick/260804-u3u-add-pt-br-and-es-translations-for-the-ne/) |
| 260804-tyf | Restore DashboardData right-text foreground color to previous Text100Brush after RowItem.RightTextForeground change | 2026-08-05 | 15b7aa0 | [260804-tyf-restore-dashboarddata-right-text-foregro](./quick/260804-tyf-restore-dashboarddata-right-text-foregro/) |
| 260806-v3s | Add color-coded thresholds to dashboard data panels: BTC Loans LTVs, stack pledged; Indicators Mayer Multiple and Fear & Greed; All-time high difference; Leverage %; Statistics YoY and Sats YoY evolutions | 2026-08-06 | ae3405f | [260806-v3s-add-color-coded-thresholds-to-dashboard-](./quick/260806-v3s-add-color-coded-thresholds-to-dashboard-/) |
| 260807-er9 | Fix Fixed vs Variable Expenses chart to show all 12 months including current and future months, matching Sats earned & spent chart behavior | 2026-08-07 | 8aabdca | [260807-er9-fix-fixed-vs-variable-expenses-chart-to-](./quick/260807-er9-fix-fixed-vs-variable-expenses-chart-to-/) |
| 260807-ff0 | Add explanatory labels to Reports tab panels with full translations | 2026-08-07 | 386175f | [260807-ff0-add-explanatory-labels-to-reports-tab-pa](./quick/260807-ff0-add-explanatory-labels-to-reports-tab-pa/) |

## Deferred Items

Items acknowledged and deferred at v0.6 milestone close (2026-07-17):

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Quality | v0.4 quality/hardening items (async void cleanup, god-VM refactor, live-API test isolation, handler unit tests) | Deferred | v0.6 |
| Debug Session | empty-loan-context — Current Loan Context formatted properties are computed read-only properties | Investigating | v0.6 |
| Debug Session | translation-gaps — UpdateLoanState UI strings added to neutral resx file not code-generated | Diagnosed | v0.6 |
| Debug Session | visual-layout — hardcoded input widths exceed available column space and button widths | Diagnosed | v0.6 |
| Quick Task | 001-copy-modal-perf | Unknown | v0.6 |
| Quick Task | reports-summary-simulation | Missing | v0.6 |
| Quick Task | 260616-rcu-fix-stock-asset-edit-modal-not-loading-s | Unknown | v0.6 |

_Note: 38-VERIFICATION.md shows `gaps_found` because it was generated before 38-03 executed; the QA-03 gap is closed by 38-QA-CHECKLIST.md._

## Session Continuity

Last session: 2026-08-11T21:12:00.536Z
Stopped at: Phase 42 planned; 3 plans ready to execute
Resume file: /home/vmabellini/RiderProjects/valt/.planning/phases/42-loans-leverage-reports-ui/42-01-PLAN.md

## Notes

- v0.6 Documentation Site Refresh milestone shipped on 2026-07-17.
- All 7 v0.6 phases (32–38) complete; 17/17 plans finished; strict MkDocs build green.
- 7 deferred items acknowledged at v0.6 close (see Deferred Items).
- v0.7 roadmap defined 2026-08-04: Phases 39-43, 12/12 requirements mapped (SPA-01..03, BTC-01..03, WLT-01..04, LON-01..02).

## Operator Next Steps

- Execute Phase 42 with /gsd-execute-phase 42
