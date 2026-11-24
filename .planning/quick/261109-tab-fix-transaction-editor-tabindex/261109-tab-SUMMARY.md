---
phase: quick-261109-tab
plan: 01
subsystem: ui
tags: [ui, keyboard-navigation, transaction-editor, quick-task]
status: complete
requires: []
provides:
  - Transaction Editor tab order where the "Exclude from goals and reports" checkbox is visited right after the Notes textbox
affects:
  - src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml
tech-stack:
  added: []
  patterns:
    - Explicit TabIndex sequence spanning the window chrome and child editor views
key-files:
  created: []
  modified:
    - src/Valt.UI/Views/Main/Modals/TransactionEditor/TransactionEditorView.axaml
decisions:
  - Assigned TabIndex="11" to the exclusion CheckBox, slotting between Notes (10) and Date picker (12); no other indexes changed
metrics:
  duration: 5m
  completed: 2026-11-09
  commits: 1
---

# Quick 261109-tab: Fix Transaction Editor TabIndex Summary

## What was done

The "Exclude from goals and reports" CheckBox in `TransactionEditorView.axaml` (right pane, docked below Notes) had no `TabIndex`. In Avalonia the default is `0`, so the checkbox was visited at the very start of the tab order — before the Name field — making keyboard navigation feel broken.

Added `TabIndex="11"` to the CheckBox. Final tab order:

1. Child editor fields 0-9 (Name, Category, From, Amount, [Installments] / [To, To Amount])
2. Notes textbox — 10
3. **Exclude from goals and reports checkbox — 11** (was: default 0, first in order)
4. Date picker — 12
5. OK — 13, Cancel — 14

## Verification

- `dotnet build src/Valt.UI/Valt.UI.csproj` — 0 errors (warnings pre-existing).
- Visual inspection of the XAML confirms every interactive element in the window now has a unique, monotonically increasing TabIndex.
