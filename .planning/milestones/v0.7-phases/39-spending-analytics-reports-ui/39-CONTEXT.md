# Phase 39: Spending Analytics Reports & UI - Context

**Gathered:** 2026-08-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Add three spending analytics to the Reports tab so users understand their spending behavior:
1. **Savings rate** — monthly (income − expenses) / income with a trend over time
2. **Burn rate** — current-month gauge: average daily spend and projected month-end total vs the 12-month median
3. **Fixed vs variable ratio** — per-month split of expenses between transactions bound to fixed expenses and everything else

All three panels must render sensible zero/empty states when no transaction data exists for the period (locked by success criterion 4). New capabilities beyond these three metrics belong to other phases (BTC-denominated metrics → Phase 40; MCP/localization/docs → Phase 43).

</domain>

<decisions>
## Implementation Decisions

### Savings Rate (SPA-01)
- **D-01:** Income and expenses come from the same values `MonthlyTotalsReport` already computes per month — no new income/expense semantics.
- **D-02:** Months with zero income are skipped in the trend (gap in the line) — division by zero is undefined; do not show 0% or N/A.
- **D-03:** Negative savings rates are displayed as-is (e.g. `-25%`), matching `MonthlyReportItemViewModel`'s existing +/- formatting pattern.
- **D-04:** The trend covers complete months only — the current incomplete month is excluded.

### Fixed vs Variable Ratio (SPA-03)
- **D-05:** "Fixed" = actual expense transactions bound to fixed expense records; "variable" = all remaining expense transactions. Planned fixed-expense range amounts are NOT used.
- **D-06:** Only records in `Paid` state (bound to an actual transaction) count toward fixed. `ManuallyPaid`, `Ignored`, and `Empty` contribute nothing.
- **D-07:** The ratio is a split of the month's total expenses — fixed + variable = 100% of spend (not measured against income).
- **D-08:** A month with expenses but no bound fixed-expense transactions renders as 100% variable.
- **D-09:** Complete months only — current incomplete month excluded (consistent with D-04).
- **D-10:** When the user has no fixed expenses registered at all, the panel shows an empty state with a hint prompting them to register fixed expenses to unlock the metric.

### Burn Rate (SPA-02)
- **D-11:** Burn rate is a current-month gauge only — no historical per-month trend in this phase.
- **D-12:** Average daily spend = month-to-date expenses / days elapsed; projected month-end total = average daily × days in month.
- **D-13:** The projection is compared against `StatisticsReport`'s existing `MedianMonthlyExpenses` (median of last 12 months) — reuse it, don't compute a new median.
- **D-14:** "Spend" = the same Expenses figure `MonthlyTotalsReport` computes — consistent with all other panels.
- **D-15:** The projection is shown only from day 5 of the month onward. Before day 5, the panel shows spend-so-far and average daily without a projection (early-month projections are noise).

### Panel Layout & Filters
- **D-16:** New panels plug into the existing `DashboardGridPanel` (like Wealth/Statistics/Indicators cards); savings rate and fixed vs variable get charts below alongside the existing charts; burn rate is a dashboard card.
- **D-17:** Savings rate = single line chart of monthly %. Fixed vs variable = stacked bar per month (fixed/variable segments).
- **D-18:** All new panels honor the existing Reports tab filters (accounts, categories, date range), like `MonthlyTotals` and `ExpensesByCategory` already do.

### Cross-Cutting Conventions (carried forward, not re-discussed)
- **D-19:** New queries follow the App-layer CQRS pattern per AGENTS.md and the SpendingEvolution module: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
- **D-20:** Metrics are fiat-denominated (main fiat currency). BTC-denominated spending metrics are Phase 40's scope.
- **D-21:** New user-facing strings may be English-only in this phase; full pt-BR/es localization lands in Phase 43 (same pattern as v0.5 Phases 30→31).

### the agent's Discretion
- Exact placement order of the new dashboard card within `DashboardGridPanel` and chart ordering on the tab — planner/designer picks what fits the existing grid layout.
- Loading-state and empty-state visuals beyond the behaviors specified above — follow existing panel patterns (`IsLoading` text, `IsLarge` row spans).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Scope & Requirements
- `.planning/ROADMAP.md` §Phase 39 — goal, success criteria, phase boundary
- `.planning/REQUIREMENTS.md` §SPA-01/SPA-02/SPA-03 — requirement definitions
- `.planning/PROJECT.md` — project constraints (tech stack, backward compatibility, vertical-slice UI rule)

### Module Documentation
- `.claude/docs/reports.md` — Reports module: `IReportDataProvider`, existing reports, `ReportsViewModel`, chart/dashboard patterns, DI registration
- `.claude/docs/fixedexpenses.md` — FixedExpense domain: `FixedExpenseRecordState` (Paid/ManuallyPaid/Ignored/Empty), transaction binding via `IFixedExpenseRecordService`

### Code Anchors (patterns to reuse)
- `src/Valt.Infra/Modules/Reports/MonthlyTotals/MonthlyTotalsReport.cs` — income/expense source for savings rate and burn rate
- `src/Valt.Infra/Modules/Reports/Statistics/StatisticsReport.cs` — `MedianMonthlyExpenses` (12-month median) reused as burn-rate baseline
- `src/Valt.App/Modules/SpendingEvolution/` + `src/Valt.Infra/Modules/SpendingEvolution/Queries/SpendingEvolutionQueries.cs` — reference implementation of the App-contract + Infra-queries pattern with filter support
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — `DashboardGridPanel` card layout and chart placement
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — `FetchAllReportsAsync` parallel fetch, filter handling, cached `IReportDataProvider`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IReportDataProvider` (`src/Valt.Infra/Modules/Reports/IReportDataProvider.cs`): pre-indexed frozen collections of accounts, categories, transactions-by-date, and historical BTC/fiat rates with binary-search lookups — ideal backing store for the new monthly calculations
- `StatisticsReport.MedianMonthlyExpenses`: the exact 12-month median the burn rate compares against (D-13)
- `MonthlyTotalsReport` per-month `Income`/`Expenses`: the source values for savings rate (D-01) and burn-rate spend (D-14)
- `DashboardData` / `RowItem` / `DashboardGridPanel` / `DashboardPanelViewModel`: dashboard card plumbing for the burn-rate panel (D-16)
- `MonthlyTotalsChartData` / `ExpensesByCategoryChartData`: LiveCharts chart-data wrappers to mirror for the savings-rate line chart and fixed/variable stacked bars
- `IFixedExpenseRecordService` binding state: identifies which transactions count as fixed (D-05/D-06)

### Established Patterns
- Reports consume a cached `IReportDataProvider` per tab lifetime; `ReportsViewModel.FetchAllReportsAsync()` fetches in parallel via `Task.WhenAll` with per-panel `IsXLoading` flags and 300ms debounced filter updates
- New App-layer modules use query/handler + `I*Queries` contract + DTOs in `Valt.App`, implemented in `Valt.Infra` (SpendingEvolution is the freshest example; AGENTS.md mandates this for new features)
- Multi-currency conversion chain: source currency → USD → target fiat
- UI changes in vertical slices (VM + XAML + tests) per PROJECT.md constraints

### Integration Points
- `ReportsView.axaml`: add a new dashboard card to `DashboardGridPanel` and two charts in the charts region
- `ReportsViewModel`: new observable properties, fetch methods wired into `FetchAllReportsAsync`, loading flags
- DI registration for new query implementations (see existing registrations in the Reports/SpendingEvolution modules)
- FilterState/ReportsViewModel filter selections must flow into the new queries (D-18)

</code_context>

<specifics>
## Specific Ideas

- Burn-rate projection threshold: user explicitly specified "project only after at least 5 days" (D-15) — not the offered default of day 3.
- Fixed/variable empty state should actively hint the user to register fixed expenses (D-10), not just show a blank chart.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. (Historical daily-burn trend lines, planned-vs-actual fixed expense drift, and sats-denominated spending all belong to Phase 40 or the v2 backlog and were not requested.)

</deferred>

---

*Phase: 39-Spending Analytics Reports & UI*
*Context gathered: 2026-08-04*
