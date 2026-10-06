# Phase 41: Wealth & Performance Reports & UI - Context

**Gathered:** 2026-08-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Add four wealth & performance metrics to the Reports tab so users can evaluate long-term wealth performance in both fiat and BTC terms:

1. **Net worth CAGR / compound growth** — in fiat and BTC terms
2. **Fiat vs BTC allocation % over time** — based on account type balances
3. **Best and worst months** — ranked by fiat wealth delta
4. **Days under water** — time since the all-time high, surfaced in the existing ATH panel

All new panels must render sensible zero/empty states when no data exists for the period and must honor existing Reports tab conventions where applicable (filter handling, loading flags, dashboard card layout, chart patterns). New capabilities beyond these four metrics (e.g., MCP tool exposure, full localization, module docs) belong to Phase 43.

</domain>

<decisions>
## Implementation Decisions

### Net Worth CAGR (WLT-01)
- **D-01:** CAGR is measured over the user's full tracked lifetime, starting from the first transaction or initial account balance. The selected date range filters the displayed period, not the start point of the compound-growth calculation.
  — **Reversibility:** costly — changing the start point after release would alter the meaning of the metric and could confuse users comparing historical screenshots.
- **D-02:** "Net worth" for CAGR means gross wealth: all accounts plus assets marked `IncludeInNetWorth` (excluding sold assets, per the existing filter). Loans and liabilities are not subtracted in this phase. This matches the existing `AllTimeHighReport` and dashboard total-wealth definition.
  — **Reversibility:** costly — adding liabilities later would change historical values and require new data plumbing.
- **D-03:** CAGR is computed in both fiat (main fiat currency) and BTC (sats) terms. The fiat CAGR uses month-end wealth converted to the main fiat currency; the BTC CAGR uses month-end wealth converted to BTC at that date's rate.

### Fiat vs BTC Allocation (WLT-02)
- **D-04:** Allocation is a balance-sheet split, not a flow split. It shows the percentage of total account balances that is BTC-denominated vs fiat-denominated at each month-end.
- **D-05:** The split is based on account type, not on converted value: accounts whose type is Bitcoin (BTC/SATS) count as 100% BTC; all other account types count as 100% fiat. External assets are excluded from this metric because the user wants an account-only view.
  — **Reversibility:** costly — changing the basis (account type vs converted value vs including assets) would change the chart's meaning and labels.
- **D-06:** Allocation is shown as a trend over time (e.g., stacked area or line chart) covering the selected date range. The current month is included up to today.

### Best and Worst Months (WLT-03)
- **D-07:** Months are ranked by the absolute fiat wealth delta from the previous month-end to the current month-end (gross wealth, same definition as CAGR). Positive deltas are "best"; negative deltas are "worst".
  — **Reversibility:** costly — switching to percentage or BTC delta would change which months appear and require UI label changes.
- **D-08:** Only complete months are ranked. The current incomplete month is excluded, consistent with Phase 39 savings-rate and fixed-vs-variable panels.
- **D-09:** Present the result as a list or table showing the top-N best and top-N worst months (e.g., top 3 each), with the month, ending wealth, and delta. The exact N is left to the planner/executor's discretion.

### Days Under Water (WLT-04)
- **D-10:** Days under water is displayed by extending the existing **All Time High** dashboard panel, not by adding a separate panel. The panel already shows ATH value and decline %; add the count of days since the ATH date.
- **D-11:** Reuse the existing `AllTimeHighReport` calculation and data model. No new drawdown chart is required in this phase.

### Cross-Cutting Conventions (carried forward from Phases 39 and 40, not re-discussed)
- **D-12:** New queries follow the App-layer CQRS pattern per `AGENTS.md`: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
- **D-13:** New panels plug into the existing `DashboardGridPanel` / charts region and honor the existing Reports tab filters where applicable (date range, account selection). Category filters do not apply to wealth-performance metrics.
- **D-14:** New user-facing strings may be English-only in this phase; full pt-BR/es localization lands in Phase 43 (same pattern as Phases 30→31 and 39→40).
- **D-15:** Panels render sensible zero/empty states when no transaction data exists for the period.

### the agent's Discretion
- Exact panel ordering within the Reports tab and the choice of chart type (stacked area, line, or dual line) for the allocation trend, as long as it appears after Phase 39 and Phase 40 panels.
- Loading-state and empty-state visuals beyond the behaviors specified above — follow existing panel patterns (`IsLoading` text, `IsLarge` row spans, zero labels).
- Whether the best/worst months list shows top 3, top 5, or all months, as long as the list is scannable and respects the layout grid.
- Specific CAGR formula edge cases (e.g., negative starting wealth, zero starting wealth) — handle gracefully without crashing; display "N/A" or omit the metric when undefined.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Scope & Requirements
- `.planning/ROADMAP.md` §Phase 41 — goal, success criteria, phase boundary
- `.planning/REQUIREMENTS.md` §WLT-01/WLT-02/WLT-03/WLT-04 — requirement definitions
- `.planning/PROJECT.md` — project constraints (tech stack, backward compatibility, vertical-slice UI rule)
- `.planning/phases/39-spending-analytics-reports-ui/39-CONTEXT.md` — prior phase decisions that carry forward (CQRS, filters, English-only strings, panel patterns, complete-months-only behavior)
- `.planning/phases/40-btc-denominated-metrics-reports-ui/40-CONTEXT.md` — prior phase decisions on monthly BTC conversion, filter handling, and panel placement

### Module Documentation
- `.claude/docs/reports.md` — Reports module: `IReportDataProvider`, existing reports, `ReportsViewModel`, chart/dashboard patterns, DI registration
- `.claude/docs/budget.md` — Accounts, transactions, categories

### Code Anchors (patterns to reuse)
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs` — existing daily wealth totals and ATH/max-drawdown calculation; reuse for CAGR denominator and days-since-ATH
- `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighData.cs` — existing ATH data model
- `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs` — month-end `FiatTotal`/`BtcTotal` and transaction flows; reuse for month-end wealth deltas
- `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs` — pre-indexed frozen collections and binary-search rate lookups
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — `DashboardGridPanel` card layout and chart placement
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — `FetchAllReportsAsync` parallel fetch, filter handling, cached `IReportDataProvider`
- `src/Valt.Infra/Modules/Reports/Statistics/StatisticsReport.cs` — example of monthly aggregation and `IReportDataProvider` consumption
- `src/Valt.App/Modules/SpendingEvolution/` + `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` — reference implementation of App-contract + Infra-queries pattern with filter support

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AllTimeHighReport` (`src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs`): already computes daily gross wealth and tracks the ATH date. Ideal for CAGR denominator (month-end wealth) and for days-since-ATH.
- `MonthlyTotalsReport` (`src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs`): already computes month-end `FiatTotal`/`BtcTotal` and can be extended or queried to produce month-over-month deltas for best/worst months.
- `IReportDataProvider` (`src/Valt.Infra/Modules/Reports/IReportDataProvider.cs`): pre-indexed accounts, transactions-by-date, and historical rates — ideal backing store for month-end calculations and account-type classification.
- `DashboardData` / `RowItem` / `DashboardGridPanel` / `DashboardPanelViewModel`: dashboard card plumbing for the days-under-water extension and any new summary cards.
- `MonthlyTotalsChartData` / `ExpensesByCategoryChartData`: LiveCharts chart-data wrappers to mirror for the allocation-over-time chart.

### Established Patterns
- Reports consume a cached `IReportDataProvider` per tab lifetime; `ReportsViewModel.FetchAllReportsAsync()` fetches in parallel via `Task.WhenAll` with per-panel `IsXLoading` flags and 300ms debounced filter updates.
- New App-layer modules use query/handler + `I*Queries` contract + DTOs in `Valt.App`, implemented in `Valt.Infra` (SpendingEvolution is the freshest example; `AGENTS.md` mandates this for new features).
- Multi-currency conversion chain: source currency → USD → target currency. For BTC conversion, target = BTC.
- UI changes in vertical slices (VM + XAML + tests) per `PROJECT.md` constraints.

### Integration Points
- `ReportsView.axaml`: extend the existing ATH dashboard panel and add a new allocation chart panel + best/worst months panel below the Phase 39/40 charts.
- `ReportsViewModel`: new observable properties, fetch methods wired into `FetchAllReportsAsync`, loading flags.
- DI registration for new query implementations (see existing registrations in the Reports/SpendingEvolution modules).
- Date-range and account filters from `FilterState`/`ReportsViewModel` must flow into the new queries; category filters are ignored for wealth-performance metrics.

</code_context>

<specifics>
## Specific Ideas

- Extend the existing All Time High panel with the days-since-ATH number rather than creating a new panel — keeps ATH-related info in one place.
- Allocation is intentionally account-type based (BTC account = BTC, everything else = fiat) rather than converting every balance to BTC, so the metric is stable and easy to reason about.
- CAGR is lifetime-only to give users a long-term view of their wealth growth; the selected date range controls the displayed window, not the start point.
- Best/worst months exclude the current incomplete month to avoid ranking a month that is still changing.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 41-Wealth & Performance Reports & UI*
*Context gathered: 2026-08-10*
