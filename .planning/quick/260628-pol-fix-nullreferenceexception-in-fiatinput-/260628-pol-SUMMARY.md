---
phase: quick
plan: 260628-pol
subsystem: Valt.UI
status: complete
tags: [bugfix, null-safety, FiatInput, crash]
dependency_graph:
  requires: []
  provides: []
  affects:
    - src/Valt.UI/UserControls/FiatInput.axaml.cs
tech_stack:
  added: []
  patterns:
    - Null-coalescing guards for Avalonia DirectProperty bindings
key_files:
  created: []
  modified:
    - src/Valt.UI/UserControls/FiatInput.axaml.cs
decisions:
  - Kept the fix inside FiatInput rather than changing callers, because multiple binding sources may legitimately push null.
metrics:
  duration: 2 min
  completed_date: "2026-06-28"
---

# Quick Task 260628-pol: Fix NullReferenceException in FiatInput

**One-liner:** Make `FiatInput` coerce null `FiatValue` bindings to `FiatValue.Empty` so editing or copying a Bitcoin-only transaction no longer crashes.

## What Changed

Updated `src/Valt.UI/UserControls/FiatInput.axaml.cs` to treat a null `FiatValue` as `FiatValue.Empty`:

- **Setter (`FiatValue`)**: `var safeValue = value ?? FiatValue.Empty;` is now used before `SetAndRaise` and before computing `_rawValue`.
- **`UpdateFiatValue()`**: `_fiatValue ??= FiatValue.Empty;` guards the comparison that previously dereferenced `_fiatValue.Value`.
- **`UpdateDisplayValue()`**: `_fiatValue ??= FiatValue.Empty;` adds defense-in-depth before the control refreshes its display.

No other logic (decimal places, currency symbol placement, calculator properties, or display formatting) was changed.

## Verification

- `dotnet build Valt.sln` completed successfully with **0 errors**.
- Pre-existing warnings in unrelated files (MCP tools, ViewModels, tests) were left untouched.

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None. The planned mitigation for `T-quick-01` (coerce null at the UI control boundary) is implemented.

## Self-Check: PASSED

- [x] Modified file `src/Valt.UI/UserControls/FiatInput.axaml.cs` exists and contains the expected null-coalescing guards.
- [x] Build `dotnet build Valt.sln` reports 0 errors.
- [x] Commit `7a39a6c` exists in git history.
