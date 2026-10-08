---
phase: 40-btc-denominated-metrics-reports-ui
plan: 02
subsystem: ui
tags: [avalonia, livecharts, cqrs, litedb, sats, btc-metrics, reports, localization]

requires:
  - phase: 40-btc-denominated-metrics-reports-ui
    plan: 01
    provides: "BTC-denominated metrics backend, DTOs, chart-data classes, ViewModel observables, XAML expanders, and English localization keys."

provides:
  - Per-category sats-spent aggregation returned in `BtcDenominatedMetricsDataDto.SpentByCategory`.
  - Horizontal row-chart category mode in `BtcDenominatedMetricsChartData` with per-bar category colors.
  - Monthly/By category toggle wired through `ReportsViewModel.IsBtcMetricsCategoryView` and `ReportsView.axaml`.
  - Comprehensive 11-test suite covering every scope decision in D-01 through D-09 and D-11.
  - English placeholder entries for all new BTC metrics strings in `pt-BR` and `es` resx files.

affects:
  - Phase 43 (full pt-BR/es translations, module docs, MCP tool exposure, end-to-end verification).

actuals:
  tokens: 11410
  tasks: 3
  commits: 4

tech-stack:
  added: []
  patterns:
    - Per-category row chart mirroring `ExpensesByCategoryChartData` with dynamic `ChartHeight` and `ScrollViewer`.
    - Toggle-controlled view visibility using `ObservableProperty` + `OnPropertyChanged(nameof(ChartData))` to avoid re-fetching.
    - Baseline month projection so every month in the date range emits a zero-value point for line-chart continuity.
    - Type-gated internal-transfer exclusion (FiatToFiat/BitcoinToBitcoin only) preserving BTC trade velocity.

key-files:
  created: []
  modified:
    - src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
    - src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml
    - tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs
    - src/Valt.UI/Lang/language.pt-BR.resx
    - src/Valt.UI/Lang/language.es.resx

key-decisions:
  - "Emit a zero-value month for every month in the date range so the stack velocity line chart has no gaps (D-11)."
  - "Narrow internal-transfer exclusion to FiatToFiat/BitcoinToBitcoin only, so BTC purchases and sales contribute to stack velocity (D-09)."
  - "Added English placeholders for new BTC metrics strings to pt-BR and es resx files now; full translations remain Phase 43 work per D-19, but all three language files must contain the keys per AGENTS.md."

patterns-established:
  - "Toggle-driven chart visibility without re-fetching: store last fetched data and refresh the active series on toggle."
  - "Per-category chart data classes keep LiveCharts row-series disposal in `Dispose()` and recreate on refresh."

requirements-completed:
  - BTC-02
  - BTC-03

coverage:
  - id: D1
    description: "Per-category sats spent breakdown returned by the query and rendered as a horizontal row chart."
    requirement: BTC-02
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs#Should_Aggregate_Sats_Spent_By_Category"
        status: pass
      - kind: unit
        ref: "tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs#Should_Honor_Category_Exclusion"
        status: pass
    human_judgment: false
  - id: D2
    description: "Monthly/By category toggle switches the visible chart in the Sats earned & spent panel without re-fetching."
    requirement: BTC-02
    verification:
      - kind: other
        ref: "dotnet build Valt.sln + grep ReportsView.axaml IsBtcMetricsCategoryView bindings"
        status: pass
    human_judgment: true
    rationale: "Live interaction between the ToggleSwitch and the two chart panels must be verified visually; automated UI tests are not in this plan."
  - id: D3
    description: "Comprehensive query test suite locks down every scope decision (native BTC signs, purchase/sale handling, internal-transfer exclusion, missing-rate skip, current month, baseline zeros, filters)."
    requirement: BTC-01
    verification:
      - kind: unit
        ref: "dotnet test --filter FullyQualifiedName~BtcDenominatedMetricsQueriesTests"
        status: pass
    human_judgment: false
  - id: D4
    description: "Stack velocity line chart receives a zero-value baseline for every month in the date range."
    requirement: BTC-03
    verification:
      - kind: unit
        ref: "tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs#Should_Return_Zero_Velocity_Months_On_Baseline"
        status: pass
    human_judgment: false
  - id: D5
    description: "All new BTC metrics and stack velocity strings are present in the three language resx files."
    requirement: BTC-02
    verification:
      - kind: other
        ref: "grep Reports_BtcMetrics_* / Reports_StackVelocity_* in language.resx, language.pt-BR.resx, language.es.resx"
        status: pass
    human_judgment: false

duration: 25min
completed: 2026-08-06
status: complete
---

# Phase 40 Plan 02: Sats Spent per Category Breakdown & Comprehensive Tests Summary

**Added per-category sats-spent breakdown, a Monthly/By category toggle in the Reports UI, and an 11-test suite that locks down the BTC-denominated metrics scope decisions.**

## Performance

- **Duration:** 25 min
- **Started:** 2026-08-06T14:50:00Z
- **Completed:** 2026-08-06T15:15:32Z
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments
- Aggregated sats spent by category in the BTC-denominated metrics query, returning category name, icon unicode, and color in `SpentByCategory`.
- Added a horizontal row-chart mode and dynamic `ChartHeight` to `BtcDenominatedMetricsChartData`, matching the existing `ExpensesByCategoryChartData` pattern.
- Wired a Monthly/By category `ToggleSwitch` in `ReportsView.axaml` and `ReportsViewModel` so the panel switches views without re-fetching data.
- Expanded `BtcDenominatedMetricsQueriesTests` to 11 tests covering fiat conversion, native BTC sign handling, BTC purchase/sale exclusion from earned/spent, inclusion in stack velocity, internal-transfer exclusion, missing-rate skip, current incomplete month, baseline zero-velocity months, category aggregation, category exclusion, and account filtering.
- Adjusted the query to emit a zero-value baseline for every month in the date range (D-11) and narrowed the internal-transfer exclusion to `FiatToFiat`/`BitcoinToBitcoin` only so BTC trades count in stack velocity (D-09).
- Synced English placeholder entries for all new BTC metrics strings to `language.pt-BR.resx` and `language.es.resx` to satisfy the project localization rule.

## Task Commits

Each task was committed atomically:

1. **Task 1: Sats spent per category breakdown** — `9dde8c2` (feat)
2. **Task 2: Comprehensive query tests** — `600dac6` (test)
3. **Task 3: Final verification and localization placeholders** — `cc6cc61` (chore)

**Plan metadata:** `docs(40-02): complete 40-02 plan` (final metadata commit)

## Files Created/Modified

- `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs` — category aggregation, baseline month fill, type-gated internal-transfer exclusion.
- `src/Valt.UI/Views/Main/Tabs/Reports/BtcDenominatedMetricsChartData.cs` — category row series, labels/axes, disposal.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — `IsBtcMetricsCategoryView` toggle, cached last data, no-op refresh.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — toggle and dual monthly/category chart panels.
- `tests/Valt.Tests/Reports/BtcDenominatedMetricsQueriesTests.cs` — 11-test suite covering scope decisions.
- `src/Valt.UI/Lang/language.pt-BR.resx` — English placeholder keys for BTC metrics strings.
- `src/Valt.UI/Lang/language.es.resx` — English placeholder keys for BTC metrics strings.

## Decisions Made
- Emit a zero-value month for every month in the date range so the stack velocity line chart has no gaps (D-11).
- Narrow internal-transfer exclusion to `FiatToFiat`/`BitcoinToBitcoin` only, so BTC purchases and sales contribute to stack velocity (D-09).
- Add English placeholders for new BTC metrics strings to `pt-BR` and `es` resx files now; full translations remain Phase 43 work per D-19, but all three language files must contain the keys per AGENTS.md.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Query emitted only months with transactions and excluded BTC trades from velocity**
- **Found during:** Task 2 (test-driven verification)
- **Issue:** The original query skipped any month without a qualifying transaction, leaving gaps in the stack velocity line chart, and treated any transaction with a `ToAccountId` in the user's account list as an internal transfer, which incorrectly excluded `FiatToBitcoin`/`BitcoinToFiat` trades from velocity.
- **Fix:** Added a baseline loop that emits every month in the date range with zero values, and narrowed the internal-transfer guard to `FiatToFiat`/`BitcoinToBitcoin` only.
- **Files modified:** `src/Valt.Infra/Modules/BtcDenominatedMetrics/Queries/BtcDenominatedMetricsQueries.cs`
- **Verification:** `Should_Return_Zero_Velocity_Months_On_Baseline` and `Should_Include_Btc_Purchases_And_Sales_In_Stack_Velocity` now pass.
- **Committed in:** `600dac6` (Task 2 commit)

### AGENTS.md-Driven Adjustments

**2. English placeholders added to `pt-BR` and `es` resx files despite D-19 English-only phase**
- **Found during:** Task 3 (localization verification)
- **Issue:** D-19 defers full pt-BR/es translations to Phase 43, but AGENTS.md requires all three language files to contain new string keys.
- **Fix:** Added English-value placeholder entries for `Reports_BtcMetrics_*` and `Reports_StackVelocity_*` to both non-English resx files.
- **Files modified:** `src/Valt.UI/Lang/language.pt-BR.resx`, `src/Valt.UI/Lang/language.es.resx`
- **Verification:** Build succeeds; keys are present in all three resx files.
- **Committed in:** `cc6cc61` (Task 3 commit)

---

**Total deviations:** 2 (1 Rule 1 bug fix, 1 AGENTS.md-driven localization adjustment)
**Impact on plan:** Both adjustments are necessary for correctness and project rule compliance. No scope creep beyond the phase boundary.

## Issues Encountered
- `dotnet test` reports two pre-existing external HTTP 403 failures in `BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` and `CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date`. These are unrelated to the BTC-denominated metrics work and are environment-dependent live API tests.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 40 is implementation-complete; remaining Phase 43 work covers MCP tool exposure, full translations, module docs, and end-to-end verification.
- No blockers.

---
*Phase: 40-btc-denominated-metrics-reports-ui*
*Completed: 2026-08-06*
