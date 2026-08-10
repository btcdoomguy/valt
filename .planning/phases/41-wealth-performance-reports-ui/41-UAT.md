---
status: complete
phase: 41-wealth-performance-reports-ui
source:
  - 41-01-SUMMARY.md
started: "2026-08-10T18:45:00Z"
updated: "2026-08-10T18:46:00Z"
---

## Current Test

[testing complete]

## Tests

### 1. All Time High panel shows Days under water row
expected: The existing All Time High dashboard panel renders a new "Days under water" row with a non-negative whole number, positioned immediately after the ATH date row.
result: pass
note: User confirmed the row renders correctly. pt-BR/es translations are intentionally deferred to Phase 43 per D-14.

### 2. Asset-aware All Time High report includes active net-worth assets and computes DaysUnderWater
expected: AllTimeHighReport injects IAssetQueries and active net-worth assets are included in daily gross wealth; DaysUnderWater is computed from the peak date.
result: pass
source: automated
coverage_id: D1

### 3. Tests verify asset inclusion/exclusion and DaysUnderWater values
expected: dotnet test --filter "FullyQualifiedName~AllTimeHigh" passes and covers the new behavior.
result: pass
source: automated
coverage_id: D3

## Summary

total: 3
passed: 3
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

[none]

## Deferred Follow-Ups

- test: 1
  idea: "Add pt-BR and es translations for the new Reports.AllTimeHigh.DaysUnderWater string in Phase 43."
  deferred_at: "2026-08-10"
