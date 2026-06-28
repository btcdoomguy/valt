---
phase: quick
plan: 01
quick_id: 260628-pgi
status: complete
subsystem: Valt.UI
started_at: "2026-06-28T00:00:00Z"
completed_at: "2026-06-28T00:05:00Z"
duration: "5 min"
tasks_completed: 1
tasks_total: 1
commit: d2f8685
key_files:
  created: []
  modified:
    - src/Valt.UI/UserControls/BtcInput.axaml.cs
deviations: "None - plan executed exactly as written."
---

# Quick Task 260628-pgi: Fix NullReferenceException in BtcInput

**One-liner:** Guard `BtcInput` against null `BtcValue` bindings so copying a fiat transaction no longer crashes.

## What Changed

Updated `src/Valt.UI/UserControls/BtcInput.axaml.cs` to coerce `null` `BtcValue` bindings to `BtcValue.Empty` at the setter and display-format boundaries.

### Commits

- `d2f8685` — fix(260628-pgi): guard BtcInput against null BtcValue bindings

## Verification

- `dotnet build Valt.sln` succeeded with 0 errors.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None.

## Self-Check: PASSED

- [x] Modified file exists: `src/Valt.UI/UserControls/BtcInput.axaml.cs`
- [x] Commit exists: `d2f8685`
