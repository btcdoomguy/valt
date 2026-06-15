# Roadmap: Valt

**Milestone:** v0.3 Loan State Timeline
**Created:** 2026-06-15
**Continues from:** Phase 5

## Milestone Goal

Let users record the evolving state of BTC loans over time and use the latest recorded snapshot as the single source of truth for current-value calculations.

## Phases

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 6 | Domain & Persistence Model | Complete | LOAN-01, LOAN-02, LOAN-03, LOAN-04, LOAN-05, TEST-01 | 2026-06-15 |
| 7 | Commands & Queries | 4/4 | Complete   | 2026-06-15 |
| 8 | Update Loan State Screen | Build the update modal and context-menu item, prefilled with current totals | UI-01, UI-02, UI-03, UI-04, UI-05, UI-06 | 6 |
| 9 | Loan State History Screen | Build the history modal with list, delete, and add-new-state actions | UI-07, UI-08, UI-09, UI-10, UI-11 | 5 |
| 10 | Polish & Verification | Localize strings, update docs, verify end-to-end flow | LOC-01, MCP-01, DOC-01 | 3 |

**Total phases:** 5
**Total v1 requirements mapped:** 26
**Coverage:** 26/26 ✓

## Phase Details

### Phase 6: Domain & Persistence Model

**Goal:** Add loan state timeline to `BtcLoanDetails`, persist it, and make the latest snapshot drive calculations.

**Requirements:** LOAN-01, LOAN-02, LOAN-03, LOAN-04, LOAN-05, TEST-01

**Success criteria:**

1. `BtcLoanDetails` exposes an ordered collection of `LoanStateUpdate` value objects.
2. Each snapshot contains effective date, APR/fee %, current total debt, collateral sats, amount taken, and optional note.
3. `CalculateCurrentValue()` and related calculation methods use the latest snapshot when present.
4. Deleting the latest snapshot falls back to the previous snapshot, then to initial setup values.
5. Initial setup values are never modified by state updates.
6. Existing loans are auto-seeded with a snapshot derived from their setup values on load/deserialization.
7. `AssetDetailsSerializer` and `BtcLoanDetailsDto` persist the timeline in JSON.
8. Domain unit tests cover ordering, latest-snapshot selection, fallback, and auto-seeding.

**Plans:** 6/6 plans complete

Plans:
**Wave 1**

- [x] 06-01-PLAN.md — Create LoanStateSnapshot value object and wire immutable snapshot storage into BtcLoanDetails

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 06-02-PLAN.md — Route all BtcLoanDetails calculations through the latest snapshot and verify query consumers
- [x] 06-03-PLAN.md — Persist snapshots in AssetDetailsSerializer and auto-seed legacy loans on deserialization

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 06-04-PLAN.md — Add domain and serializer tests for snapshot behavior and auto-seeding

**Wave 4** *(gap closure from 06-VERIFICATION.md)*

- [x] 06-05-PLAN.md — Fix auto-seeding, date parsing, and domain validation gaps identified in verification

**Wave 5** *(blocked on Wave 4 completion)*

- [x] 06-06-PLAN.md — Add missing tests and correct auto-seed assertions

### Phase 7: Commands & Queries

**Goal:** Implement add/delete/query handlers and update existing loan queries to use the latest snapshot.

**Requirements:** CMD-01, CMD-02, QUERY-01, QUERY-02, QUERY-03, TEST-02, MCP-01

**Success criteria:**

1. `AddLoanStateUpdateCommand` appends a snapshot and re-saves the asset.
2. `DeleteLoanStateUpdateCommand` removes a snapshot by ID/date and re-saves the asset.
3. `GetLoanStateTimelineQuery` returns the full chronological list of snapshots for a loan.
4. `GetLatestLoanStateQuery` returns the latest snapshot plus calculated prefilled totals.
5. `AssetQueries`, `GetAssetSummaryQuery`, and `GetBtcLoansDashboardQuery` reflect the latest snapshot.
6. Handler/integration tests verify add, delete, query, and dashboard refresh behavior.
7. MCP `AssetTools` exposes the new commands/queries.

**Plans:** 4/4 plans complete

Plans:

**Wave 1**

- [x] 07-01-PLAN.md — Add and delete loan-state snapshot commands

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 07-02-PLAN.md — Timeline and latest-state snapshot queries

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 07-03-PLAN.md — Update existing queries to use latest snapshot and preserve snapshots on edit

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 07-04-PLAN.md — Expose commands/queries through MCP AssetTools and integration tests

### Phase 8: Update Loan State Screen

**Goal:** Build the update modal and context-menu item, prefilled with current calculated totals.

**Requirements:** UI-01, UI-02, UI-03, UI-04, UI-05, UI-06

**Success criteria:**

1. Assets tab context menu for BTC loans shows "Update Loan State".
2. Selecting it opens a new modal following existing patterns.
3. Modal fields are prefilled with values from the latest snapshot.
4. Effective date defaults to today and is editable via `CalendarDatePicker`.
5. Field labels match the existing add-loan screen captions.
6. Validation rejects invalid dates and numeric values.
7. Saving refreshes the Assets tab and reflects the new state.

**Plans:** 1/1 plans complete

Plans:

**Wave 1**

- [ ] 08-01-PLAN.md — Add modal infrastructure, implement prefill/save/wiring, and add ViewModel tests

### Phase 9: Loan State History Screen

**Goal:** Build the history modal with list, delete, and add-new-state actions.

**Requirements:** UI-07, UI-08, UI-09, UI-10, UI-11

**Success criteria:**

1. User can open "Loan State History" from the Assets tab context menu or update flow.
2. History lists all snapshots in chronological order with key fields visible.
3. Each entry has a delete action with a confirmation prompt.
4. A button opens the "Update Loan State" screen to add a new snapshot.
5. After deletion, the Assets tab refreshes and calculations fall back correctly.

### Phase 10: Polish & Verification

**Goal:** Localize strings, update docs, and verify the end-to-end flow.

**Requirements:** LOC-01, MCP-01, DOC-01

**Success criteria:**

1. All new user-facing strings exist in `language.resx`, `language.pt-BR.resx`, and `language.es.resx`.
2. `.claude/docs/assets.md` documents the new loan state timeline behavior.
3. Full manual/integration verification passes for add, update, delete, and fallback flows.
4. Build passes (`dotnet build Valt.sln`) and relevant tests pass (`dotnet test`).

## Dependencies

- Phase 6 must complete before Phase 7.
- Phase 7 must complete before Phase 8.
- Phase 8 and Phase 9 can be developed in parallel after Phase 7, but both depend on it.
- Phase 10 depends on Phase 8 and Phase 9.

## Notes

- Phase numbering continues from the previous milestone (Phase 5), so this milestone spans Phases 6-10.
- No `.planning/phases/` directories are created by this roadmap; they are generated when each phase is planned via `/gsd-plan-phase [N]`.

---
*Roadmap created: 2026-06-15*
*Last updated: 2026-06-15 after initial creation*
