# Phase 42: Loans & Leverage Reports & UI - Context

**Gathered:** 2026-08-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Add two loan-specific analytics to the Reports tab so users can see the real cost and evolving liquidation risk of their BTC-backed loans:

1. **Interest/fees paid** — total lifetime interest and fees plus a per-month breakdown derived from the BTC loan state timeline.
2. **Liquidation-price distance trend** — a trend showing how close the riskiest active loan is to its liquidation threshold over time.

Both reports are conditional: they appear only when the user has active BTC-backed loans, consistent with the existing BTC Loans dashboard panel. All other concerns (MCP tool exposure, full pt-BR/es localization, public docs) are explicitly out of scope and belong to Phase 43.

</domain>

<decisions>
## Implementation Decisions

### Interest/fees monthly breakdown
- **D-01:** The monthly breakdown is computed by apportioning the daily APR accrual between consecutive loan-state snapshots across calendar months, and assigning the snapshot's one-time `Fees` to the month of the snapshot's effective date. — **Reversibility:** costly — changes the meaning of the chart values and would require re-labeling the cost axis if a different model is adopted later.
- **D-02:** Interest and fees are displayed as a single combined carrying-cost series (stacked bar per month), not as separate interest and fees series. — **Reversibility:** costly — would split the chart into two series and require new language keys and legend text.
- **D-03:** The trend includes the current incomplete month, with interest accrued up to today. — **Reversibility:** reversible.

### Liquidation-price distance trend
- **D-04:** The chart shows a single worst-case (closest) distance-to-liquidation across all active BTC-backed loans per month, not a per-loan series or a debt-weighted average. — **Reversibility:** costly — switching to per-loan or weighted-average would change the chart's legend, axis labels, and data contract.
- **D-05:** Distance is measured in LTV percentage points (`liquidation LTV - current LTV`), reusing the same semantics as the existing BTC Loans dashboard `ClosestDistance` row. — **Reversibility:** costly — changing the metric would alter labels and comparison points.
- **D-06:** The trend line is rendered with color-coded risk bands (green / yellow / red) based on margin-call and liquidation thresholds, reusing the existing `DashboardDataBrushes.ForLtv` thresholds. — **Reversibility:** reversible.

### Trend period and active-loan filter
- **D-07:** The trend charts honor the Reports tab's existing date-range selector (the one above the monthly totals / categories sections). A loan contributes to a given month only if it is active on that month-end. — **Reversibility:** reversible.
- **D-08:** Loans are counted on a strict month-end basis: if a loan is created or repaid mid-month, it does not appear in that month. Interest/fees accrue only up to the month-end or the next snapshot, whichever comes first. — **Reversibility:** reversible.

### UI grouping and placement
- **D-09:** The new reports are grouped into a single "Loans & Leverage Reports" expander section inside the monthly totals group, placed after the Stack velocity section and before the Categories section. It shares the existing date-range selector. — **Reversibility:** costly — moving the section later would require XAML and resx changes.
- **D-10:** The section shows both charts at once: a stacked-bar chart for combined interest+fees cost and a line chart for worst-case liquidation distance. No toggle is used. — **Reversibility:** reversible.

### Cross-cutting conventions (carried forward from Phases 39-41, not re-discussed)
- **D-11:** New queries follow the App-layer CQRS pattern per `AGENTS.md`: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
- **D-12:** New panels plug into the existing Reports tab infrastructure: `ReportsViewModel.FetchAllReportsAsync()` parallel fetch, per-panel `IsLoading` / `IsVisible` flags, and debounced filter updates.
- **D-13:** New user-facing strings are English-only in this phase; full pt-BR/es localization lands in Phase 43.
- **D-14:** Panels render sensible zero/empty states when no active BTC-backed loans exist or when the selected range has no data.

### the agent's Discretion
- Exact chart height, legend position, and tooltip formatting.
- Specific empty-state wording and icon choice, as long as it follows the existing hint-box style.
- Whether to introduce a dedicated `LoanReports` App/Infra module or place the new queries inside the existing Assets module.
- Whether to expose a single combined query DTO or separate query DTOs for the two charts, provided the UI contract stays simple.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope and requirements
- `.planning/ROADMAP.md` §Phase 42 — goal, success criteria, phase boundary.
- `.planning/REQUIREMENTS.md` §LON-01/LON-02 — requirement definitions.
- `.planning/PROJECT.md` — project constraints (tech stack, backward compatibility, vertical-slice UI rule).
- `.planning/phases/39-spending-analytics-reports-ui/39-CONTEXT.md` — prior phase decisions on CQRS, filters, English-only strings, panel patterns, and chart wiring.
- `.planning/phases/40-btc-denominated-metrics-reports-ui/40-CONTEXT.md` — prior phase decisions on monthly BTC conversion, filter handling, and section placement.
- `.planning/phases/41-wealth-performance-reports-ui/41-CONTEXT.md` — prior phase decisions on Reports tab layout, date-range handling, and dashboard conventions.

### Module documentation
- `.claude/docs/assets.md` — Assets module, `BtcLoanDetails`, `LoanStateSnapshot`, loan-state commands/queries, and DTOs.
- `.claude/docs/reports.md` — Reports module, `IReportDataProvider`, `ReportsViewModel`, chart/dashboard patterns, and DI registration.

### Code anchors (patterns to reuse)
- `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs` — snapshot semantics, APR accrual, liquidation-distance calculation, and total-debt calculation.
- `src/Valt.Core/Modules/Assets/Details/LoanStateSnapshot.cs` — snapshot fields (`EffectiveDate`, `InterestAccruedUntilDate`, `Fees`, `TotalBorrowed`, `Apr`, `LiquidationLtv`).
- `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs` — active-loan filtering, currency conversion, weighted LTV aggregation, and existing dashboard calculations.
- `src/Valt.App/Modules/Assets/Commands/AddLoanStateUpdate/AddLoanStateUpdateHandler.cs` — snapshot creation semantics and field meanings.
- `src/Valt.UI/Views/Main/Tabs/Reports/Panels/BtcLoans/BtcLoansPanelViewModel.cs` — existing dashboard rows, formatting, and conditional visibility.
- `src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataBrushes.cs` — LTV/leverage risk color thresholds.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml` — section layout and `Expander` placement.
- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs` — `FetchAllReportsAsync`, filter handling, and panel property wiring.
- `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs` — historical BTC/fiat rates and pre-indexed data for cross-month calculations.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BtcLoanDetails` and `LoanStateSnapshot` (`src/Valt.Core/Modules/Assets/Details/`) already model the timeline, APR accrual, fees, and liquidation distance. The new reports can reuse these calculations rather than duplicating loan math.
- `GetBtcLoansDashboardHandler` (`src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/`) provides patterns for active-loan filtering, fiat conversion to main currency, and BTC-price-aware LTV simulation.
- `BtcLoansPanelViewModel` (`src/Valt.UI/Views/Main/Tabs/Reports/Panels/BtcLoans/`) contains the existing conditional visibility logic and risk color usage that the new section should mirror.
- `DashboardDataBrushes` (`src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataBrushes.cs`) supplies reusable color thresholds for LTV risk bands.
- Existing chart-data classes (`MonthlyTotalsChartData`, `ExpensesByCategoryChartData`, `StackVelocityChartData`) demonstrate the LiveCharts wrapper pattern for the new charts.
- `IReportDataProvider` provides historical BTC/fiat rates and pre-indexed transactions/accounts for month-end computations that depend on past prices.

### Established Patterns
- Reports consume a cached `IReportDataProvider` per tab lifetime; `ReportsViewModel.FetchAllReportsAsync()` fetches in parallel via `Task.WhenAll` with per-panel `IsXLoading` flags and 300ms debounced filter updates.
- New App-layer modules use query/handler + `I*Queries` contract + DTOs in `Valt.App`, implemented in `Valt.Infra` (`SpendingEvolution` is the freshest example; `AGENTS.md` mandates this for new features).
- Multi-currency conversion chain: source currency → USD → target currency, used by the existing dashboard handler and report provider.
- UI changes are done in vertical slices (VM + XAML + tests) per `PROJECT.md` constraints.
- English-only strings in this phase; full localization in Phase 43.

### Integration Points
- Add a new `Expander` section in `ReportsView.axaml` after the Stack velocity section and before the Categories section, inside the monthly totals group.
- Add new observable properties and fetch methods in `ReportsViewModel` and wire them into `FetchAllReportsAsync`.
- Create new chart-data classes (e.g., `LoanCostsChartData`, `LiquidationDistanceChartData`) and query/DTO classes in `Valt.App`/`Valt.Infra`.
- Register the new query implementation in DI (see existing `GetBtcLoansDashboard` or `SpendingEvolution` registrations).
- The new section uses the same `DateCalendarSelector` (date range) as the monthly totals and BTC-denominated metrics sections.

</code_context>

<specifics>
## Specific Ideas

- The interest/fees chart should answer "how much did my loans cost me per month?" Treat interest as a continuous APR accrual split by calendar month and fees as discrete charges recorded on the snapshot effective date.
- The liquidation-distance chart should be a single line of the worst-case (closest to liquidation) loan, colored by risk bands so the user can immediately see when risk is rising.
- The new reports should feel like a natural continuation of the v0.7 analytics: placed in the monthly totals group, sharing the date selector, and following the same loading/empty/visibility patterns as the existing sections.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within the phase boundary. Any per-loan drill-down, CSV export, public docs, localization, or MCP tool exposure belongs to Phase 43 or the v2 backlog.

</deferred>

---

*Phase: 42-Loans & Leverage Reports & UI*
*Context gathered: 2026-08-11*
