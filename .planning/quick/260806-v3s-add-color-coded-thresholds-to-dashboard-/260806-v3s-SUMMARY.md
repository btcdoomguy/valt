---
status: complete
---

# Quick Task 260806-v3s: Add color-coded thresholds to dashboard data panels

## Summary
Added green/yellow/red color coding to key dashboard data panels based on configurable thresholds, extending the existing Burn Rate panel pattern.

## Changes Made
- Added `DashboardWarning` brush to theme resources and `Warning` property to `TransactionGridResources`.
- Created `DashboardDataBrushes` helper with threshold-based color selection for:
  - LTV (<=60 green, <=75 yellow, >75 red)
  - Stack pledged (<=40 green, >40 red)
  - Mayer Multiple (<=0.8 green, <1.2 yellow, >=1.2 red)
  - Fear & Greed (<=30 red, <=70 yellow, >70 green)
  - ATH difference / leverage % / YoY evolution (three-tier thresholds)
- Applied the brushes to dashboard rows in:
  - `BtcLoansPanelViewModel`
  - `IndicatorsPanelViewModel`
  - `LeveragePositionsPanelViewModel`
  - `ReportsViewModel` (All-time high decline and statistics evolutions)
- Updated affected unit tests to initialize `TransactionGridResources` for testing.

## Verification
- `dotnet build Valt.sln` succeeded with 0 errors.
- UI test suite passed: `dotnet test tests/Valt.Tests/Valt.Tests.csproj --filter "FullyQualifiedName~UI"` (357 passed).
- Full solution test run had 4 unrelated failures (CoinGecko API 403, MCP tests).

## Artifacts
- Plan: `260806-v3s-PLAN.md`
- This summary: `260806-v3s-SUMMARY.md`
