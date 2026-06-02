# Phase 02 Context: Core Implementation

**Phase:** 02 — Core Implementation
**Goal:** Build query layer, chart visualization, category selector, and data calculations
**Date:** 2026-05-27

## Domain

Create the actual functionality inside the Spending Evolution modal:
- Category selector with multi-select tree
- Dual-axis line chart (fiat total + sats total)
- Time range dropdown (12/24/36/48/60 months)
- Cost of living indicators (fiat % increase, BTC % increase)
- Real-time chart updates when categories change

## Decisions

### Data Aggregation Strategy
- **Approach:** Database-side aggregation via LiteDB query
  - Rationale: Faster initial load, handles 5 years of data efficiently
  - Query returns pre-aggregated monthly totals per category
  - ViewModel caches results and filters in-memory for real-time category selection
  - Pattern: Similar to existing TransactionQueries but with aggregation pipeline

### Chart Implementation
- **Library:** LiveChartsCore.SkiaSharpView (existing)
- **Pattern:** Reuse existing dual-axis chart classes (WealthOverviewChartData, MonthlyTotalsChartData)
- **Colors:** Use existing color palette (Fiat = blue Secondary500, BTC = orange Accent400)
- **Axes:** Left Y-axis for fiat total, right Y-axis for sats total
- **Series:** LineSeries<ObservablePoint> with fill, following existing styling

### Category Selector
- **Component:** TreeView with checkboxes (reuse ManageCategories pattern)
- **Model:** CategoryTreeElement with IsSelected property
- **Behavior:** Parent categories auto-select/deselect all children
- **Data source:** Reuse existing GetCategoriesQuery

### Time Range
- **Options:** 12, 24, 36, 48, 60 months via ComboBox dropdown
- **Default:** 24 months
- **Date calculation:** From (today - N months) to today

### Cost of Living Calculation
- **Fiat increase:** ((last month total - first month total) / first month total) * 100
- **BTC increase:** ((last month sats - first month sats) / first month sats) * 100
- **Display:** "+X%" or "-X%" with color coding (green for decrease, red for increase)
- **Edge case:** If first month is 0, show "N/A"

### Missing PriceInSats Handling
- **Strategy:** Show warning indicator when any transaction lacks PriceInSats
- **Calculation:** Exclude transactions without PriceInSats from sats totals
- **UI:** Yellow warning banner at top of modal: "Some transactions are missing BTC price data"

## Code Context

### Reusable Assets
- **LiveCharts dual-axis:** `WealthOverviewChartData.cs` and `MonthlyTotalsChartData.cs` — copy pattern
- **Category tree:** `CategoryTreeElement` from ManageCategories — add IsSelected property
- **Transaction queries:** `TransactionQueries.cs` — extend with aggregation method
- **CQRS pattern:** Query + Handler in Valt.App, implementation in Valt.Infra

### Files to Create
```
src/Valt.App/Modules/SpendingEvolution/
├── Queries/
│   ├── GetSpendingEvolutionQuery.cs
│   └── GetSpendingEvolutionQueryHandler.cs
└── DTOs/
    └── SpendingEvolutionMonthDto.cs

src/Valt.Infra/Modules/SpendingEvolution/
└── Queries/
    └── SpendingEvolutionQueries.cs

src/Valt.UI/Views/Main/Modals/SpendingEvolution/
├── SpendingEvolutionChartData.cs
└── (update existing files)
```

### Files to Modify
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionView.axaml` — Add chart, category selector, time dropdown, indicators
- `src/Valt.UI/Views/Main/Modals/SpendingEvolution/SpendingEvolutionViewModel.cs` — Add chart data, query dispatching, category loading
- `src/Valt.UI/Extensions.cs` — Register new queries
- `src/Valt.App/Extensions.cs` — Register query handler
- `src/Valt.Infra/Extensions.cs` — Register query implementation

## Architecture Decisions

### Query Responsibility
- **Valt.App:** Define query (GetSpendingEvolutionQuery), DTO (SpendingEvolutionMonthDto), handler
- **Valt.Infra:** Implement query using LiteDB aggregation
- **Valt.UI:** Consume query via IQueryDispatcher, bind results to chart

### Data Flow
1. ViewModel loads on modal open
2. Load categories via existing GetCategoriesQuery
3. Load monthly data via GetSpendingEvolutionQuery (date range, selected categories)
4. Cache monthly data in ViewModel
5. Bind chart data to LiveCharts Series
6. On category selection change: filter cached data, update chart
7. On time range change: re-query with new date range

## Canonical Refs

- `.planning/PROJECT.md` — Project context
- `.planning/REQUIREMENTS.md` — Requirements UI-03 through DATA-07
- `.planning/ROADMAP.md` — Phase 2 goal and success criteria
- `.planning/phases/01-foundation/01-CONTEXT.md` — Phase 1 decisions (modal size, menu placement)
- `src/Valt.UI/Views/Main/Tabs/Reports/WealthOverviewChartData.cs` — Dual-axis chart pattern
- `src/Valt.UI/Views/Main/Tabs/Reports/MonthlyTotalsChartData.cs` — Monthly chart pattern
- `src/Valt.UI/Views/Main/Modals/ManageCategories/Models/CategoryTreeElement.cs` — Category tree model
- `src/Valt.Infra/Modules/Budget/Transactions/Queries/TransactionQueries.cs` — Query pattern
- `src/Valt.App/Modules/Budget/Transactions/Queries/GetTransactionsQuery.cs` — CQRS query pattern

## Deferred Ideas

None (all scope items fit within Phase 2)

## Notes

- Performance target: <500ms for 5 years of data
- Follow existing naming conventions and patterns
- Use existing localization strings where possible (Phase 3 will add new strings)
- Chart should update smoothly when categories change (no flicker)
- Consider using ObservableCollection for chart data to enable automatic UI updates
