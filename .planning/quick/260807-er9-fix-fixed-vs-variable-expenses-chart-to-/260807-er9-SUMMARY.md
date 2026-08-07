---
status: complete
quick_id: 260807-er9
date: 2026-08-07
---

# Quick Task 260807-er9: Fix Fixed vs Variable Expenses chart to show all 12 months

## Summary

Changed `FixedVsVariableQueries` to emit one entry for every month in the requested date range, including empty, current, and future months. This matches the behavior already used by `BtcDenominatedMetricsQueries` for the "Sats earned & spent" chart.

## Changes Made

- `src/Valt.Infra/Modules/SpendingAnalytics/Queries/FixedVsVariableQueries.cs`
  - Removed the filter that excluded the current month and future months (`< firstOfCurrentMonth`).
  - Added a month-by-month loop from `query.From` to `query.To`, defaulting `FixedTotal` and `VariableTotal` to `0m` when no transactions exist for a month.
  - Removed the now-unused `today` / `firstOfCurrentMonth` variables.

- `tests/Valt.Tests/Reports/FixedVsVariableQueriesTests.cs`
  - Renamed `Should_Exclude_Current_Incomplete_Month` to `Should_Include_Current_Incomplete_Month` and updated assertions to verify the current month is present with its actual values.
  - Renamed `Should_Return_Empty_Months_When_No_Expense_Transactions_In_Range` to `Should_Return_All_Months_When_No_Expense_Transactions_In_Range` and updated assertions to expect a zero-filled entry for each month in the range.

## Verification

- `dotnet build Valt.sln` — succeeded.
- `dotnet test --filter "FullyQualifiedName~FixedVsVariableQueriesTests"` — 8 passed.
- `dotnet test --filter "FullyQualifiedName~Valt.Tests.Reports"` — 76 passed.

## Commit

Code changes committed separately; docs artifacts committed in follow-up.
