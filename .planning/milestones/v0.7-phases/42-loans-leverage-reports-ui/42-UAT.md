---
status: diagnosed
phase: 42-loans-leverage-reports-ui
source:
  - 42-01-SUMMARY.md
  - 42-02-SUMMARY.md
  - 42-03-SUMMARY.md
started: 2026-08-12T00:00:00Z
updated: 2026-08-12T00:40:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Build the solution with the new LoanReports module
expected: |
  `dotnet build Valt.sln` completes without errors; the new `Valt.App.Modules.LoanReports` query, handler, contract, and DTOs are present.
result: pass

### 2. LoanReportsQueries projects monthly cost and liquidation distance
expected: |
  The `LoanReportsQueries` implementation returns monthly interest+fees cost and worst-case liquidation distance based on loan snapshots and current debt.
result: issue
reported: "Dashboard Data panel shows current accumulated interest about R$ 14000, but the Loans & Leverage chart shows more than R$ 400000 for this month."
severity: major

### 3. Reports tab shows Loans & Leverage Reports section with charts
expected: |
  When at least one active BTC-backed loan exists, the Reports tab displays a "Loans & Leverage Reports" Expander containing a stacked-bar cost chart and a line liquidation-distance chart.
result: skipped
reason: "Deferred follow-up: it's good but it would be nice if the user could see when hovering for the chart column caption the info about each loan and the distance of each one from liquidation"

### 4. LoanReportsQueriesTests pass
expected: |
  `dotnet test --filter FullyQualifiedName~LoanReportsQueriesTests` passes and covers empty state, APR accrual, fees, fixed debt, month-end boundaries, currency conversion, liquidation distance, and the current incomplete month.
result: pass

### 5. Query handles edge cases and missing rates gracefully
expected: |
  `dotnet test --filter FullyQualifiedName~LoanReports` passes; interest accrual uses inclusive day counts, fixed debt stays stable, currency conversion works, and missing rates do not crash the report.
result: pass

### 6. ReportsViewModelTests pass
expected: |
  `dotnet test --filter FullyQualifiedName~ReportsViewModelTests` passes; `FetchLoanReportsAsync` dispatches the query and sets visibility, empty, loading, and error flags correctly.
result: pass

### 7. Full solution test suite runs with only pre-existing failures
expected: |
  `dotnet test Valt.sln` runs; the only failures are the four pre-existing ones listed in 42-03-SUMMARY.md (external API / unrelated asset tests).
result: pass

## Summary

total: 7
passed: 5
issues: 1
pending: 0
skipped: 1
blocked: 0

## Gaps

- gap_id: G-42-2
  truth: "The LoanReportsQueries implementation returns monthly interest+fees cost and worst-case liquidation distance based on loan snapshots and current debt."
  status: failed
  reason: "User reported: Dashboard Data panel shows current accumulated interest about R$ 14000, but the Loans & Leverage chart shows more than R$ 400000 for this month."
  severity: major
  test: 2
  root_cause: "In LoanReportsQueries.GetLoanReportsAsync, the monthly interest and fees accumulators are declared once per month and converted to the main currency inside the per-loan loop after already accumulating values from earlier loans. Re-converting the mixed-currency accumulator for each additional loan compounds the monthly cost."
  artifacts:
    - path: "src/Valt.Infra/Modules/LoanReports/Queries/LoanReportsQueries.cs"
      issue: "interest and fees accumulators converted inside per-loan loop after accumulating cross-loan values"
    - path: "src/Valt.App/Modules/Assets/Queries/GetBtcLoansDashboard/GetBtcLoansDashboardHandler.cs"
      issue: "Reference correct pattern: convert each loan's accrued interest individually before summing"
    - path: "tests/Valt.Tests/Reports/LoanReportsQueriesTests.cs"
      issue: "No assertion for monthly cost with multiple active loans of mixed currencies"
  missing:
    - "Refactor monthly cost loop so each loan's interest and fees are computed in the loan's currency, converted to main currency once, then added to monthly totals"
    - "Keep running totals strictly in main currency and move conversion out of the per-loan loop"
    - "Add a test covering monthly cost with multiple active loans in different currencies"
  debug_session: ".planning/debug/loanreports-cost-discrepancy.md"

## Coverage Validation Errors

The coverage blocks in the SUMMARY files were malformed and could not be auto-passed. They are being verified manually in this UAT.

- 42-01-SUMMARY.md: `verification.kind` must be one of `unit`, `integration`, `e2e`, `automated_ui`, `manual_procedural`, or `other` (not `build`).
- 42-02-SUMMARY.md: `verification.kind` must be one of `unit`, `integration`, `e2e`, `automated_ui`, `manual_procedural`, or `other` (not `test`).
- 42-03-SUMMARY.md: `verification.kind` must be one of `unit`, `integration`, `e2e`, `automated_ui`, `manual_procedural`, or `other` (not `test`), and `verification.status` must be one of `pass`, `fail`, or `unknown` (not `pass-with-known-failures`).

## Deferred Follow-Ups

- test: 3
  idea: "it's good but it would be nice if the user could see when hovering for the chart column caption the info about each loan and the distance of each one from liquidation"
  deferred_at: 2026-08-12
