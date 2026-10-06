---
status: complete
phase: 45-simulator-modal-ui-inputs-results-panel
source:
  - 45-01-SUMMARY.md
  - 45-02-SUMMARY.md
started: "2026-08-21T00:00:00Z"
updated: "2026-08-21T00:10:00Z"
---

## Current Test

[testing complete]

## Tests

### 1. BTC Loan Simulator modal opens and renders with inputs and results panels
expected: Launch Valt, open the BTC Loan Simulator from the Tools menu. A chromeless modal appears with a custom title bar, input fields on the left and results panel on the right showing total to repay, breakdown, liquidation price, APR, and distance to liquidation.
result: pass

### 2. Live recalculation and visual fidelity match the approved UI-SPEC
expected: Changing any input (collateral, amount, LTV, dates, rate, fees, or interest mode) updates the results panel live without closing the modal. Fiat values show currency symbol, sats values and conversion basis appear when BTC price is available, and distance-to-liquidation color switches between green (safe) and red (at/below liquidation).
result: pass

### 3. Live recalculation from all inputs through BtcLoanSimulationCalculator
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D2

### 4. Total to repay, principal, interest, and fees breakdown rows
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D3

### 5. Fiat and sats values with BTC price conversion basis and fallback when unavailable
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D4

### 6. Liquidation BTC price derived from LTV and total debt
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D5

### 7. Effective APR displays the fee-inclusive rate formatted to 2 decimals
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D1

### 8. Distance to liquidation shown as a signed percentage
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D2

### 9. Distance-to-liquidation color is red when current BTC price is at or below liquidation price and green otherwise
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D3

### 10. Currency and interest-mode changes trigger fresh recalculation; invalid inputs clear results
expected: All automated tests pass.
result: pass
source: automated
coverage_id: D4

## Summary

total: 10
passed: 10
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

[none yet]
