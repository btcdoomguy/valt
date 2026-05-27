# Architecture Research — Spending Evolution Module

**Date:** 2026-05-27
**Context:** Adding module to existing CQRS + layered architecture

## Integration Pattern

Follow the existing module pattern:

```
Valt.Core (Domain)
  └── Modules/SpendingEvolution/
      └── (Value objects for calculations)

Valt.App (Application)
  └── Modules/SpendingEvolution/
      ├── Queries/
      │   └── GetSpendingEvolutionQuery
      └── DTOs/
          └── SpendingEvolutionDto

Valt.Infra (Infrastructure)
  └── Modules/SpendingEvolution/
      └── Queries/
          └── SpendingEvolutionQueries

Valt.UI (Presentation)
  └── Views/Main/Modals/
      └── SpendingEvolutionModal
  └── Views/Main/Tabs/ (optional)
      └── SpendingEvolutionTab (or Tools menu)
```

## Data Flow

1. **UI** — User selects categories + time range
2. **Query** — `GetSpendingEvolutionQuery(categoryIds, months)`
3. **Handler** — Calculates monthly aggregates from Transactions
4. **Repository** — Reads from LiteDB Transactions collection
5. **Response** — Monthly data points with fiat total + sats total
6. **Chart** — LiveCharts binds to DTO collection

## New Components

- **Query + Handler** — Aggregate transactions by month/category
- **DTO** — Monthly snapshot with fiat amount + sats amount
- **Modal View + ViewModel** — Category selector + chart + indicators
- **Menu integration** — Main menu + context menu registration

## Build Order

1. Domain value objects (if needed)
2. Query + DTO
3. Infrastructure query implementation
4. Modal ViewModel
5. Modal View (XAML)
6. Menu integration
7. Right-click context menu handler
