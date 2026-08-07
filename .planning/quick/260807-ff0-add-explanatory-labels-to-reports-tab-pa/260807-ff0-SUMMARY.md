---
status: complete
quick_id: 260807-ff0
date: 2026-08-07
---

# Quick Task 260807-ff0: Add explanatory labels to Reports tab panels with full translations

## Summary

Added concise explanatory labels below the header of each non-summary panel on the Reports tab. Each label explains what the panel shows and how it is calculated, with full translations in English, Portuguese, and Spanish.

## Changes Made

- `src/Valt.UI/Lang/language.resx`
  - Added `Reports_WealthOverview_Description`
  - Added `Reports_MonthlyTotals_Description`
  - Added `Reports_FixedVariable_Description`
  - Added `Reports_BtcMetrics_Description`
  - Added `Reports_StackVelocity_Description`
  - Added `Reports_ByCategories_Description`

- `src/Valt.UI/Lang/language.pt-BR.resx` and `src/Valt.UI/Lang/language.es.resx`
  - Added the same six keys with Portuguese and Spanish translations.

- `src/Valt.UI/Lang/language.Designer.cs`
  - Added public static string properties for each new resource key.

- `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`
  - Added a styled `TextBlock` at the top of each non-summary panel content area (Wealth Overview, Monthly totals, Fixed vs variable expenses, Sats earned & spent, Stack velocity, Categories).
  - Descriptions use `FontSizeSmall`, `Text500Brush`, text wrapping, and a max width of 900 for readability.

## Verification

- `dotnet build Valt.sln` — succeeded.
- `dotnet test --filter "FullyQualifiedName~ReportsViewModelTests"` — 12 passed.

## Commit

Code changes committed separately; docs artifacts committed in follow-up.
