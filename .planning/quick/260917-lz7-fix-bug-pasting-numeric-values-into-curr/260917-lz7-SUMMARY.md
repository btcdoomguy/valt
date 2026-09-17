---
phase: 260917-lz7
plan: 01
subsystem: UI
tags: [ui, bugfix, fiatinput, paste]
dependency_graph:
  requires: []
  provides: [paste-aware FiatInput currency control]
  affects: [all 73 FiatInput usages across modals]
tech-stack:
  added: []
  patterns: [culture-aware decimal parsing in Avalonia TextInputEvent tunnel handler]
key-files:
  created: []
  modified: [src/Valt.UI/UserControls/FiatInput.axaml.cs]
decisions:
  - Paste path uses decimal.TryParse with CurrentUICulture first, InvariantCulture fallback, matching the plan exactly
  - Negative pasted values are ignored (value unchanged) per plan
metrics:
  duration: 5min
  completed_date: "2026-09-17"
  tasks: 1
status: complete
actuals:
  tokens: 3000
  tasks: 1
  commits: 1
---

# Quick Task 260917-lz7: Fix paste into FiatInput currency fields

Pasting a numeric value (e.g. "1.234,56") into any FiatInput field (Update Loan State > Current Interest Paid, etc.) no longer registers as 0 — the tunnel `OnTextInput` handler now parses multi-character paste text as a culture-aware decimal and updates `_rawValue`, while single-digit typing behavior is byte-for-byte unchanged.

## What Was Done

**Task 1 (commit `89b3cc2`):** Patched `OnTextInput` in `src/Valt.UI/UserControls/FiatInput.axaml.cs`:

- `e.Text.Length == 1 && char.IsDigit` → unchanged existing behavior (`_rawValue = _rawValue * 10 + long.Parse(...)`)
- `e.Text.Length > 1` (paste) → trim, `decimal.TryParse` with `NumberStyles.Number` and `CultureInfo.CurrentUICulture`, fallback `CultureInfo.InvariantCulture`; on success (and non-negative) `_rawValue = (long)Math.Round(parsed * 10^_decimalPlaces, MidpointRounding.AwayFromZero)` + `UpdateDisplayValue()`; on parse failure or negative value, `_rawValue` is left unchanged (safe no-op)
- Caret/selection repositioning and unconditional `e.Handled = true` preserved on all paths so the inner TextBox never double-applies pasted text
- `BtcInput` untouched (out of scope); no AXAML or localization changes

## Verification

- `dotnet build Valt.sln --nologo -v q` → **Build succeeded**, 0 errors (79 pre-existing warnings, none from this change)
- No existing test coverage for `FiatInput` (test project targets Core/App/Infra layers); plan specified `tdd="false"` for Task 1

## Deferred / Manual Verification Required

**Task 2 (checkpoint:human-verify, gate=blocking) — left pending, requires human UAT:**

1. Run `dotnet run --project src/Valt.UI/Valt.UI.csproj`
2. Open Update Loan State → paste "1234,56" (pt-BR) / "1234.56" (en-US) into Current Interest Paid → correct value, not 0
3. Paste a value with thousand separators ("1.234,56" / "1,234.56") → parses per locale
4. Type digits 1-2-3 in the same field → behaves exactly as before
5. Paste "abc" → no crash, value unchanged
6. Spot-check one other FiatInput location (Transaction amount or Manage Asset > Current Price)

## Deviations from Plan

None — plan executed exactly as written.
