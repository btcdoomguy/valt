---
gsd_state_version: 1.0
milestone: v0.3
milestone_name: milestone
status: Ready for Phase 07
last_updated: "2026-06-15T18:13:14.932Z"
last_activity: 2026-06-15 -- Phase 06 execution completed
progress:
  total_phases: 5
  completed_phases: 1
  total_plans: 6
  completed_plans: 6
  percent: 20
---

# STATE.md

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-15)

**Core value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.
**Current focus:** Phase 07 — Commands & Queries

## Current Position

Phase: 06 (Domain & Persistence Model) — COMPLETE
Plan: 6 of 6 complete
Status: Ready for Phase 07
Last activity: 2026-06-15 -- Phase 06 execution completed

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
