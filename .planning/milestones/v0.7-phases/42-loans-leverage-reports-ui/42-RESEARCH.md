# Phase 42: Loans & Leverage Reports & UI — Research

**Researched:** 2026-08-11
**Domain:** Avalonia desktop UI, .NET 9, CQRS, BTC-backed loan state timeline, LiveCharts Skia report panels
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Interest/fees monthly breakdown**
  - **D-01:** The monthly breakdown is computed by apportioning the daily APR accrual between consecutive loan-state snapshots across calendar months, and assigning the snapshot's one-time `Fees` to the month of the snapshot's effective date. — **Reversibility:** costly — changes the meaning of the chart values and would require re-labeling the cost axis if a different model is adopted later.
  - **D-02:** Interest and fees are displayed as a single combined carrying-cost series (stacked bar per month), not as separate interest and fees series. — **Reversibility:** costly — would split the chart into two series and require new language keys and legend text.
  - **D-03:** The trend includes the current incomplete month, with interest accrued up to today. — **Reversibility:** reversible.

- **Liquidation-price distance trend**
  - **D-04:** The chart shows a single worst-case (closest) distance-to-liquidation across all active BTC-backed loans per month, not a per-loan series or a debt-weighted average. — **Reversibility:** costly — switching to per-loan or weighted-average would change the chart's legend, axis labels, and data contract.
  - **D-05:** Distance is measured in LTV percentage points (`liquidation LTV - current LTV`), reusing the same semantics as the existing BTC Loans dashboard `ClosestDistance` row. — **Reversibility:** costly — changing the metric would alter labels and comparison points.
  - **D-06:** The trend line is rendered with color-coded risk bands (green / yellow / red) based on margin-call and liquidation thresholds, reusing the existing `DashboardDataBrushes.ForLtv` thresholds. — **Reversibility:** reversible.

- **Trend period and active-loan filter**
  - **D-07:** The trend charts honor the Reports tab's existing date-range selector (the one above the monthly totals / categories sections). A loan contributes to a given month only if it is active on that month-end. — **Reversibility:** reversible.
  - **D-08:** Loans are counted on a strict month-end basis: if a loan is created or repaid mid-month, it does not appear in that month. Interest/fees accrue only up to the month-end or the next snapshot, whichever comes first. — **Reversibility:** reversible.

- **UI grouping and placement**
  - **D-09:** The new reports are grouped into a single "Loans & Leverage Reports" expander section inside the monthly totals group, placed after the Stack velocity section and before the Categories section. It shares the existing date-range selector. — **Reversibility:** costly — moving the section later would require XAML and resx changes.
  - **D-10:** The section shows both charts at once: a stacked-bar chart for combined interest+fees cost and a line chart for worst-case liquidation distance. No toggle is used. — **Reversibility:** reversible.

- **Cross-cutting conventions**
  - **D-11:** New queries follow the App-layer CQRS pattern per `AGENTS.md`: query + contract + DTO in `Valt.App`, implementation in `Valt.Infra`.
  - **D-12:** New panels plug into the existing Reports tab infrastructure: `ReportsViewModel.FetchAllReportsAsync()` parallel fetch, per-panel `IsLoading` / `IsVisible` flags, and debounced filter updates.
  - **D-13:** New user-facing strings are English-only in this phase; full pt-BR/es localization lands in Phase 43.
  - **D-14:** Panels render sensible zero/empty states when no active BTC-backed loans exist or when the selected range has no data.

### the agent's Discretion

- Exact chart height, legend position, and tooltip formatting.
- Specific empty-state wording and icon choice, as long as it follows the existing hint-box style.
- Whether to introduce a dedicated `LoanReports` App/Infra module or place the new queries inside the existing Assets module.
- Whether to expose a single combined query DTO or separate query DTOs for the two charts, provided the UI contract stays simple.

### Deferred Ideas (OUT OF SCOPE)

- Per-loan drill-down
- CSV export
- Public docs
- Localization
- MCP tool exposure (all belong to Phase 43 or the v2 backlog)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LON-01 | User can view total interest/fees paid and per-month breakdown from loan state timeline | Use `LoanStateSnapshot` timeline, simple APR accrual split by calendar month, fees assigned to snapshot month; convert to main currency. |
| LON-02 | User can view liquidation-price distance trend over time | Use month-end active-loan filter, historical BTC/fiat rates, existing LTV distance formula, color-band thresholds. |
</phase_requirements>

## Research Summary

Phase 42 adds two loan-specific analytics to the Reports tab. The data source is the existing `BtcLoanDetails` / `LoanStateSnapshot` timeline, and the output surface is the existing Reports tab infrastructure (`ReportsViewModel`, `DateCalendarSelector`, LiveCharts Skia `CartesianChart`, `Expander` sections, and `DashboardDataBrushes`).

The key research outcome is that **no new domain model is required**: interest accrual, liquidation distance, and currency conversion already exist in `BtcLoanDetails`, `GetBtcLoansDashboardHandler`, `IReportDataProvider`, and `CurrencyConversionService`. The work is primarily a read-model projection over snapshots plus UI wiring.

**Primary recommendation:** Create a dedicated `Valt.App/Modules/LoanReports` (and `Valt.Infra/Modules/LoanReports`) CQRS module, expose a single `GetLoanReportsQuery` that returns both cost and distance data, and implement the UI by reusing `StackVelocityChartData` (line chart) and `FixedVsVariableChartData` / `BtcDenominatedMetricsChartData` (stacked-bar chart) patterns.

## Domain Findings

### Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Project snapshots to monthly cost/distance | API / Backend (`Valt.App` + `Valt.Infra`) | — | Heavy date arithmetic and rate lookups belong in the App/Infra read model. |
| Render charts and empty states | UI (`Valt.UI`) | — | LiveCharts bindings, `IsLoading`/`IsVisible` flags, and localization live in the View layer. |
| Historical BTC/fiat rates | Infrastructure (`Valt.Infra.Modules.Reports`) | — | `IReportDataProvider` is the existing abstraction for pre-indexed historical rates. |

### Loan State Snapshot Model

The BTC loan timeline is already modeled as an ordered list of immutable `LoanStateSnapshot` value objects [VERIFIED: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs:91`]. Each snapshot records:

```csharp
// Source: src/Valt.Core/Modules/Assets/Details/LoanStateSnapshot.cs
public sealed class LoanStateSnapshot
{
    public string PlatformName { get; }
    public long CollateralSats { get; }
    public decimal LoanAmount { get; }
    public string CurrencyCode { get; }
    public decimal Apr { get; }
    public decimal LiquidationLtv { get; }
    public decimal MarginCallLtv { get; }
    public decimal Fees { get; }
    public decimal? FixedTotalDebt { get; }
    public decimal TotalBorrowed { get; }
    public decimal InterestAccruedUntilDate { get; }
    public decimal CurrentTotalDebt => TotalBorrowed + InterestAccruedUntilDate + Fees;
    public DateOnly EffectiveDate { get; }
    // ...
}
```

The latest snapshot wins for all current-state calculations, and legacy loans without snapshots are auto-seeded from setup values [VERIFIED: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs:164-182`].

### Interest Accrual Formula

Simple interest from a snapshot's effective date is already implemented in the domain:

```csharp
// Source: src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs:242-249
private decimal CalculateAccruedInterestForSnapshot(LoanStateSnapshot snapshot)
{
    var daysSinceSnapshot = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - snapshot.EffectiveDate.DayNumber;
    if (daysSinceSnapshot <= 0)
        return 0m;

    return Math.Round(snapshot.TotalBorrowed * snapshot.Apr / 365 * daysSinceSnapshot, 2);
}
```

This is the exact formula to split across calendar months. For fixed-debt snapshots, no additional interest accrues [VERIFIED: `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs:222-224`].

### Liquidation Distance Formula

The existing dashboard uses:

```csharp
// Source: src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs:131-141
var simulatedLtvs = loansWithLtvForRisk
    .Select(l => new { Loan = l, SimulatedLtv = CalculateSimulatedLtv(l) ?? l.CurrentLtv!.Value })
    .ToList();

var closest = simulatedLtvs
    .OrderBy(x => x.Loan.LiquidationLtv!.Value - x.SimulatedLtv)
    .First();
closestDistance = closest.Loan.LiquidationLtv!.Value - closest.SimulatedLtv;
```

The LTV simulation uses the same `CollateralSats / 100_000_000 * btcPrice` collateral value [VERIFIED: `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs:77-91`]. The distance is therefore `LiquidationLtv - CurrentLtv` in percentage points.

### Multi-Currency Conversion Chain

Existing pattern is source currency → USD → main currency [VERIFIED: `src/Valt.Infra/Modules/Currency/Services/CurrencyConversionService.cs:17-64`]. The dashboard handler already demonstrates this for BTC-collateralized loans [VERIFIED: `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs:38-54`]. For historical month-end LTV, the BTC/fiat rates should come from `IReportDataProvider.GetUsdBitcoinPriceAt(date)` and `GetFiatRateAt(date, currency)` [VERIFIED: `src/Valt.Infra/Modules/Reports/IReportDataProvider.cs:26-27`].

### Reports Tab Wiring

- `ReportsViewModel.FetchAllReportsAsync()` runs fetches in parallel via `Task.WhenAll` [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:397-410`].
- Filter changes are debounced 300 ms [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:174-175`].
- Date-range selector for monthly-totals group uses `FilterRange` and `FilterMainDate` [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:115-118`].
- Existing conditional panels (`BtcLoans`, `Leverage`, `BurnRate`) use per-panel `IsXLoading` and `IsXVisible` flags wired from a sub-ViewModel [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:123-128` and `OnBtcLoansPanelPropertyChanged` at lines 1266-1274].
- The new section must be placed inside the monthly-totals `Border`, after Stack velocity and before the Categories `Expander` [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml:248-667`].

### Risk Color Thresholds

`DashboardDataBrushes.ForLtv` already maps green/yellow/red bands:

```csharp
// Source: src/Valt.UI/Views/Main/Tabs/Reports/DashboardDataBrushes.cs:11-16
public static IBrush ForLtv(decimal ltv) => ltv switch
{
    <= 60m => TransactionGridResources.Credit,
    <= 75m => TransactionGridResources.Warning,
    _ => TransactionGridResources.Debt
};
```

The liquidation-distance chart can reuse these thresholds for the line/area color, although the semantics are inverted (lower distance = higher risk).

## Technical Options

### Option A: Module placement

| Approach | Where | Pros | Cons |
|----------|-------|------|------|
| A1 — New top-level module | `Valt.App/Modules/LoanReports`, `Valt.Infra/Modules/LoanReports` | Mirrors `SpendingAnalytics`/`BtcDenominatedMetrics`; keeps report logic out of asset-management module. | Slightly more folders; handler auto-registration is assembly-wide so no extra DI scan work. |
| A2 — Extend Assets module | `Valt.App/Modules/Assets/Queries/GetLoanReports`, `Valt.Infra/Modules/Assets/Queries/LoanReportsQueries` | Reuses existing `IAssetQueries`/loan data proximity; smallest conceptual jump from `GetBtcLoansDashboard`. | Blurs "asset management" with "report analytics"; may grow large if more loan reports are added later. |

**Recommendation:** A1. Create a dedicated `LoanReports` module. It is report-domain code, and the existing pattern for report analytics (`SpendingAnalytics`, `BtcDenominatedMetrics`) is a top-level module. The new module can still depend on `IAssetQueries` and the existing Asset DTOs.

### Option B: Query / DTO shape

| Approach | DTO shape | Pros | Cons |
|----------|-----------|------|------|
| B1 — Single query, single DTO | `GetLoanReportsQuery` → `LoanReportsDataDto` with both `Costs` and `DistanceTrend` | One handler, one dispatch, one fetch method in `ReportsViewModel`; matches D-12's simple UI contract. | DTO is slightly wider; less reusable if future panels need only one metric. |
| B2 — Separate queries / DTOs | `GetLoanCostsQuery` + `GetLiquidationDistanceTrendQuery` | Fine-grained; easier to test one concern at a time; two parallel fetches in `ReportsViewModel`. | Two handlers, two contracts, more boilerplate for what is always fetched together. |

**Recommendation:** B1. Expose a single `GetLoanReportsQuery` returning a combined `LoanReportsDataDto`. The two charts share the same filter, active-loan set, and month list; a single backend projection avoids duplicate DB reads and keeps the UI contract simple.

### Option C: Interest computation strategy

| Approach | How | Pros | Cons |
|----------|-----|------|------|
| C1 — Reconstruct from snapshots | For each month, find the effective snapshot, accrue daily interest up to month-end or next snapshot, assign fees to snapshot month. | Matches D-01 exactly; uses existing domain math; no dependency on monthly deltas. | Must handle snapshot boundaries carefully across months. |
| C2 — Delta between consecutive snapshots | Compute `CurrentTotalDebt` deltas month-over-month and attribute to cost. | Simple arithmetic. | Obscures interest vs fees; does not separate accrual from added principal/collateral changes; violates D-01/D-02 semantics. |

**Recommendation:** C1. Reconstruct monthly carrying cost directly from snapshots using the existing simple-interest formula.

### Option D: Liquidation distance visualization

| Approach | How | Pros | Cons |
|----------|-----|------|------|
| D1 — Single colored line | Line series whose stroke/fill color changes per segment or per point based on risk band. | Clean, matches D-04/D-06; easy to read trend. | LiveCharts color-per-point requires a custom mappers or segmented series. |
| D2 — Background zones / multiple series | Draw horizontal-zone annotations plus a line; or split into Healthy/Warning/Danger series. | Clear risk categorization. | More complex; diverges from existing simple line patterns. |

**Recommendation:** D1. Use a single `LineSeries<ObservablePoint>` and color the line/area with a mapper or by generating a gradient. If color-per-point is awkward in LiveCharts, color the whole line with the current risk band (similar to `DashboardDataBrushes.ForLtv`) as a pragmatic fallback.

## Recommended Approach

### Backend

1. **Create module**
   - `Valt.App/Modules/LoanReports/`
     - `DTOs/LoanReportsDataDto.cs` — combined result.
     - `DTOs/LoanCostMonthDto.cs` — month, combined cost, interest, fees (hide details per D-02, but expose them for tooltips/testing).
     - `DTOs/LiquidationDistanceMonthDto.cs` — month, distance, risk band, closest loan name.
     - `Queries/GetLoanReportsQuery.cs`
     - `Queries/GetLoanReportsHandler.cs`
     - `Contracts/ILoanReportsQueries.cs`
   - `Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs` — implementation.

2. **Query contract**
   ```csharp
   public record GetLoanReportsQuery : IQuery<LoanReportsDataDto>
   {
       public DateOnly From { get; init; }
       public DateOnly To { get; init; }
   }
   ```

3. **DTO contract**
   ```csharp
   public record LoanReportsDataDto
   {
       public required IReadOnlyList<LoanCostMonthDto> CostMonths { get; init; }
       public required IReadOnlyList<LiquidationDistanceMonthDto> DistanceMonths { get; init; }
       public required bool HasActiveLoans { get; init; }
       public required string PrimaryCurrency { get; init; }
   }
   ```

4. **Implementation sketch**
   - Load all active `BtcLoan` assets via `IAssetQueries.GetAllAsync()` or directly via `ILocalDatabase.GetAssets()`.
   - For each month from `From` to `To`:
     - Determine the effective snapshot per loan as of month-end (latest snapshot with `EffectiveDate <= monthEnd`).
     - If the loan status at that point is not `Active`, skip it.
     - **Cost:** for each day from max(snapshot.EffectiveDate, month start) to min(monthEnd, nextSnapshot.EffectiveDate, today), accrue `TotalBorrowed * Apr / 365`. Add snapshot `Fees` to the month of the snapshot's `EffectiveDate`.
     - Convert each loan's monthly cost from loan currency to main currency using `CurrencyConversionService` (or replicate the source → USD → main chain).
     - **Distance:** compute `CurrentLtv = LoanAmount / (CollateralSats / 1e8 * btcPriceInLoanCurrency) * 100`. Use historical BTC price from `IReportDataProvider.GetUsdBitcoinPriceAt(monthEnd)` and fiat rate to loan currency. Distance = `LiquidationLtv - CurrentLtv` (clamped to ≥ 0). Pick the minimum across all active loans.
   - Return every month in the range (zero-filled) so LiveCharts axes stay stable.

5. **DI registration**
   - Add `services.AddSingleton<ILoanReportsQueries, LoanReportsQueries>();` in `Valt.Infra/Extensions.cs` `AddQueries()` [VERIFIED: `src/Valt.Infra/Extensions.cs:273-288`].
   - The `GetLoanReportsHandler` is auto-registered by `AddValtApp()` because it implements `IQueryHandler<,>` [VERIFIED: `src/Valt.App/Extensions.cs:30-39`].

### UI

1. **Chart-data classes**
   - `LoanCostChartData` — stacked column, single series (combined cost), fiat y-axis, month x-axis. Modeled on `FixedVsVariableChartData` [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/FixedVsVariableChartData.cs`].
   - `LiquidationDistanceChartData` — line chart with fill, percentage y-axis, month x-axis. Modeled on `StackVelocityChartData` [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/StackVelocityChartData.cs`].

2. **ViewModel changes**
   - Add to `ReportsViewModel`:
     - `LoanCostChartData`, `LiquidationDistanceChartData`.
     - `IsLoanReportsLoading`, `IsLoanReportsVisible`, `IsLoanReportsEmpty`.
     - `FetchLoanReportsAsync(IReportDataProvider provider)` and add it to `FetchAllReportsAsync`.
   - Subscribe to `AssetSummaryUpdatedMessage` to refresh (already wired for leverage/BTC loans panels) [VERIFIED: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs:288-294`].

3. **XAML**
   - Add a new `Expander` inside the monthly-totals group `Border` after Stack velocity and before Categories.
   - Use the same loading/empty/error pattern as the Stack velocity section:
     - `IsVisible="{Binding !IsLoanReportsLoading}"`
     - `Border` chart when `!IsLoanReportsEmpty`
     - `Border Classes="hint-box"` when `IsLoanReportsEmpty`

4. **Localization**
   - English-only in this phase (D-13). Add keys such as:
     - `Reports_LoanReports_Title`
     - `Reports_LoanReports_CostDescription`
     - `Reports_LoanReports_DistanceDescription`
     - `Reports_LoanReports_EmptyHeading`
     - `Reports_LoanReports_EmptyBody`
   - Add to `language.resx` and `language.Designer.cs`; pt-BR/es land in Phase 43.

## Risks & Open Questions

| # | Risk / Open Question | Impact | Mitigation |
|---|----------------------|--------|------------|
| R1 | **Mid-month snapshots and month-end filter.** D-08 says a loan created/repaid mid-month does not contribute to that month. The query must treat `EffectiveDate > monthEnd` as "not yet active" and `Status == Repaid` as inactive even before month-end. | Medium — boundary off-by-one errors could produce empty or double-counted months. | Unit-test exact month boundaries. |
| R2 | **Currency conversion for historical month-end LTV.** `IReportDataProvider.GetUsdBitcoinPriceAt(date)` and `GetFiatRateAt(date, currency)` use closest-on-or-before date. If a loan currency has no historical rate on a given month-end, conversion returns 0 and may break LTV calculation. | Medium — missing fiat rates are possible for non-main currencies. | Defensive fallback: skip that month for that loan or show 0 distance; log a warning. |
| R3 | **Custom BTC price simulation.** The dashboard uses `CustomBtcPriceState.CustomBtcPriceUsd` for simulated LTV. The trend chart likely should also honor the custom price so the UI is consistent. | Low-medium — inconsistency with dashboard if ignored. | Accept `CustomBtcPriceUsd` in the query and use it when present, mirroring `GetBtcLoansDashboardQuery` [VERIFIED: `src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardQuery.cs:6-12`]. |
| R4 | **Fixed-debt loans.** `FixedTotalDebt` snapshots do not accrue daily interest; the cost is effectively baked into the fixed delta. D-01 still applies (fees assigned to snapshot month, interest = 0). | Low — fixed-debt loans are less common but must not accrue phantom interest. | Explicit branch: if `snapshot.FixedTotalDebt.HasValue`, monthly interest = 0; fees still assigned. |
| R5 | **Color-per-point in LiveCharts.** LiveChartsCore.SkiaSharp does not trivially support different colors per point in a line series. | Low — risk band color may have to apply to the whole line or use a gradient. | Prototype in a spike if needed; fallback to a single line color updated per refresh. |
| R6 | **Performance over long date ranges.** The query iterates every month × every active loan × every snapshot. With many loans/years this could be O(n·m). | Low — number of BTC-backed loans per user is small; can optimize later if needed. | Keep projection in memory and avoid N+1 DB calls by loading all assets and rates once. |

### Assumptions

- **A1:** The query should honor the existing `FilterRange` (year/all selector) exactly like Fixed vs Variable and BTC metrics. This is implied by D-07 and D-12.
- **A2:** The liquidation-distance trend uses historical BTC price at each month-end, not today's price. D-05 implies a point-in-time LTV, and D-07 ties it to the date range.
- **A3:** The combined cost series in the chart will be a single stacked column for total interest+fees, not two series, per D-02. The DTO may still expose separate values for tooltips/tests.

## Validation Architecture

> Enabled because `workflow.nyquist_validation` is `true` in `.planning/config.json` [VERIFIED: `.planning/config.json:7`].

### Test Framework

| Property | Value |
|----------|-------|
| Framework | NUnit 4 + NSubstitute |
| Config file | `tests/Valt.Tests/Valt.Tests.csproj` (no custom `.runsettings`) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~LoanReports"` |
| Full suite command | `dotnet test Valt.sln` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| LON-01 | Monthly interest/fees cost is computed from snapshots | unit | `dotnet test --filter "FullyQualifiedName~LoanReportsQueriesTests"` | ❌ Wave 0 |
| LON-01 | Fees are assigned to snapshot effective month | unit | same | ❌ Wave 0 |
| LON-01 | Fixed-debt loans do not accrue monthly interest | unit | same | ❌ Wave 0 |
| LON-01 | Costs are converted to main currency | unit | same | ❌ Wave 0 |
| LON-02 | Worst-case liquidation distance is computed per month-end | unit | same | ❌ Wave 0 |
| LON-02 | Only active loans on month-end contribute | unit | same | ❌ Wave 0 |
| LON-01/02 | Empty state when no active loans | unit/UI | `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` | ❌ new test needed |
| Integration | DI registration resolves handler and queries | integration | `dotnet test --filter "FullyQualifiedName~IntegrationTest"` | ✅ existing |

### Wave 0 Gaps

- [ ] `tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs` — covers LON-01/LON-02 backend logic.
- [ ] `tests/Valt.Tests/UI/Screens/ReportsViewModelTests.cs` — add tests for `IsLoanReportsVisible` wiring and fetch invocation.
- [ ] UI test for chart-data class `LoanCostChartData` / `LiquidationDistanceChartData` (optional; can be covered by VM tests).
- [ ] Add English resx strings and Designer property generation.

### Sampling Rate

- **Per task commit:** `dotnet test --filter "FullyQualifiedName~LoanReports"`
- **Per wave merge:** `dotnet test Valt.sln`
- **Phase gate:** Full suite green before `/gsd-verify-work`

## RESEARCH COMPLETE
