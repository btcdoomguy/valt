---
status: complete
phase: 09-loan-state-history-screen
source: [09-VERIFICATION.md]
started: 2026-06-16T17:55:00Z
updated: 2026-06-16T18:03:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Assets tab context menu opens Loan State History
expected: Right-click a BTC-backed loan card and select "Loan State History". Modal opens with snapshot DataGrid.
result: pass

### 2. Update Loan State "View History" link opens history
expected: Open "Update Loan State" from the same context menu, then click "View History". The Loan State History modal opens without closing the update modal.
result: pass

### 3. Delete confirmation dialog wording
expected: Select a snapshot, click "Delete selected". The confirmation dialog shows the snapshot date and notes that calculations will fall back to the previous snapshot.
result: pass

### 4. Last-snapshot delete guard
expected: With only one snapshot remaining, the "Delete selected" button is disabled.
result: pass

### 5. Add-new-state navigation
expected: Click "Add new state" in the history modal. The history modal closes and the Update Loan State modal opens for the same asset.
result: pass

### 6. Assets tab refresh after delete/add
expected: After deleting or adding a snapshot, return to the Assets tab. The loan card values reflect the latest snapshot (or fallback setup values if all snapshots deleted).
result: pass

### 7. pt-BR/es localization
expected: Switch application language to Portuguese and Spanish. History modal title, buttons, context menu label, and delete confirmation resolve correctly.
result: pass

## Summary

total: 7
passed: 7
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

None - all reported issues addressed:
- "View History" button visibility improved by removing the `transparent` style class and adding vertical alignment/font size.
- Snapshot deletion now triggers recalculation: `BtcLoanDetails.CalculateTotalDebt()` accrues simple interest from the effective snapshot's effective date to today.
- "Add new state" no longer crashes: `LoanStateHistoryViewModel.AddNewState()` now uses the history window's `Owner` before closing it, avoiding a closed-owner dialog exception.

---
*Updated after Phase 9 verify-work issue fixes*
