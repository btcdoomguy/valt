---
gsd_state_version: 1.0
milestone: v0.3
milestone_name: milestone
status: verifying
last_updated: "2026-06-16T15:37:33.535Z"
last_activity: 2026-06-16 -- Phase 08 verified; advanced to Phase 09
progress:
  total_phases: 5
  completed_phases: 3
  total_plans: 13
  completed_plans: 13
  percent: 60
---

# STATE.md

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-15)

**Core value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.
**Current focus:** Phase 08 — update-loan-state-screen

## Current Position

Phase: 9
Plan: Not started
Status: Automated verification passed; human testing required before phase completion
Last activity: 2026-06-16

## Accumulated Context

### Decisions

- Latest recorded loan state snapshot wins for current-value calculations
- Initial loan setup values are immutable
- Existing loans are auto-seeded with an initial state entry
- Loan state history is stored inside `BtcLoanDetails` JSON
- Seeded `CurrentTotalDebt` must match the `LoanStartDate` effective date (`LoanAmount + Fees` or `FixedTotalDebt`)
- Missing `EffectiveDate` or `LoanStartDate` in persisted snapshots is invalid and must throw
- Persisted dates are parsed with `CultureInfo.InvariantCulture`
- Duplicate effective dates in the constructor-supplied snapshot list are invalid
- `UpdateLoanStateViewModel` falls back to `AssetDTO` setup values when no loan-state snapshot exists
- `FiatValue` properties on the update modal use `[Required]` only; `[Range]` is omitted because `FiatValue` does not implement `IComparable`
- English-only localization for new strings in Phase 08; pt-BR/es translations for UpdateLoanState_* and Assets_UpdateLoanState keys are added in Phase 08 gap closure (revised D-07) instead of deferred to Phase 10
- `language.Designer.cs` is maintained manually in this environment because `PublicResXFileCodeGenerator` does not run on Linux builds

### Blockers

(None)

### Todos

(None)

### Completed Plans

- 06-01 — LoanStateSnapshot value object & immutable storage
- 06-02 — Snapshot-driven calculations & query consumer verification
- 06-03 — Snapshot persistence & legacy auto-seeding
- 06-04 — Domain & serializer test coverage
- 06-05 — Close serializer & domain validation gaps
- 06-06 — Correct & extend test coverage
- 08-01 — Update Loan State screen modal, wiring, and ViewModel tests
- 08-02 — Localize Update Loan State context menu, modal labels, and validation message keys
- 08-03 — Fix Current Loan Context refresh, localized validation messages, and modal layout

## Performance Metrics

| Phase | Plan | Duration | Notes |
|-------|------|----------|-------|
| Phase 08 P03 | 2 min | 2 tasks | 2 files |
