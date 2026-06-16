# Requirements: Valt

**Defined:** 2026-06-15
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v1 Requirements

### Loan State Timeline (Domain)

- [x] **LOAN-01**: A BTC loan can hold an ordered list of state snapshots, each with an effective date, APR/fee %, current total debt, collateral in sats, amount taken, and optional note
- [x] **LOAN-02**: Existing BTC loans without snapshots are automatically seeded with a single snapshot derived from their initial setup values
- [x] **LOAN-03**: The latest snapshot is used as the source of truth for current total debt, collateral, amount taken, and APR/fee % calculations
- [x] **LOAN-04**: When the latest snapshot is deleted, calculations fall back to the next most recent snapshot; if no snapshots remain, fall back to the initial setup values
- [x] **LOAN-05**: Initial loan setup values remain immutable after creation and are only visible in the original manage-asset screen

### Commands & Queries (Application)

- [ ] **CMD-01**: User can add a new loan state snapshot to an existing BTC loan via `AddLoanStateUpdateCommand`
- [ ] **CMD-02**: User can delete an existing loan state snapshot via `DeleteLoanStateUpdateCommand`
- [ ] **QUERY-01**: System can retrieve the full chronological timeline of snapshots for a BTC loan via `GetLoanStateTimelineQuery`
- [ ] **QUERY-02**: System can retrieve the latest snapshot with calculated prefilled totals for the update screen via `GetLatestLoanStateQuery`
- [ ] **QUERY-03**: Existing loan dashboard and summary queries reflect the latest snapshot for each loan

### Update Loan State Screen (UI)

- [x] **UI-01**: Assets tab context menu for BTC loans includes an "Update Loan State" item
- [x] **UI-02**: The update modal opens prefilled with current calculated totals from the latest snapshot
- [x] **UI-03**: The effective date defaults to today and is editable via the existing `CalendarDatePicker` control
- [x] **UI-04**: The modal uses the same captions/labels as the existing add-loan screen for matching fields
- [x] **UI-05**: Validation prevents saving a snapshot with an empty/invalid effective date and invalid numeric values
- [x] **UI-06**: After saving, the Assets tab refreshes and reflects the new loan state

### Loan State History Screen (UI)

- [ ] **UI-07**: User can open a "Loan State History" screen from the Assets tab context menu or from the update flow
- [ ] **UI-08**: The history screen lists all recorded snapshots in chronological order
- [ ] **UI-09**: User can delete a snapshot from the history screen with a confirmation prompt
- [ ] **UI-10**: The history screen has a button to open the "Update Loan State" screen to add a new snapshot
- [ ] **UI-11**: After deletion, the Assets tab refreshes and falls back to the previous snapshot or initial setup

### Localization & MCP

- [ ] **LOC-01**: New strings (menu items, modal titles, field labels, validation messages, confirmation prompts) are added to `language.resx`, `language.pt-BR.resx`, and `language.es.resx`
- [ ] **MCP-01**: New commands/queries are exposed through MCP `AssetTools` so the AI assistant can read and update loan state

### Documentation & Quality

- [ ] **DOC-01**: `.claude/docs/assets.md` is updated with the new loan state timeline behavior
- [x] **TEST-01**: Domain tests cover snapshot ordering, latest-snapshot calculation, fallback after deletion, and auto-seeding
- [ ] **TEST-02**: Handler tests cover add/delete/query commands and integration with existing loan queries

## v2 Requirements

### Reporting & Analytics

- **REPORT-01**: Loan state history is included in exported reports
- **REPORT-02**: Chart showing debt/collateral/amount-taken evolution over time

### Automation

- **AUTO-01**: Optional automatic snapshot suggestions based on detected BTC price thresholds or repayment dates

## Out of Scope

| Feature | Reason |
|---------|--------|
| Interest accrual between snapshots | The system uses only the latest snapshot; no interpolation or compound interest simulation between states |
| Editing existing snapshots | Snapshots are append-only; users delete and recreate if a correction is needed |
| Non-BTC loan state timeline | Only `BtcLoanDetails` is in scope; other asset types keep their current model |
| Loan state history export | Not required for v1; deferred to v2 reporting |
| Multi-currency snapshots | Snapshot currency matches the loan's original currency |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LOAN-01 | Phase 6 | Complete |
| LOAN-02 | Phase 6 | Complete |
| LOAN-03 | Phase 6 | Complete |
| LOAN-04 | Phase 6 | Complete |
| LOAN-05 | Phase 6 | Complete |
| CMD-01 | Phase 7 | Pending |
| CMD-02 | Phase 7 | Pending |
| QUERY-01 | Phase 7 | Pending |
| QUERY-02 | Phase 7 | Pending |
| QUERY-03 | Phase 7 | Pending |
| UI-01 | Phase 8 | Complete |
| UI-02 | Phase 8 | Complete |
| UI-03 | Phase 8 | Complete |
| UI-04 | Phase 8 | Complete |
| UI-05 | Phase 8 | Complete |
| UI-06 | Phase 8 | Complete |
| UI-07 | Phase 9 | Pending |
| UI-08 | Phase 9 | Pending |
| UI-09 | Phase 9 | Pending |
| UI-10 | Phase 9 | Pending |
| UI-11 | Phase 9 | Pending |
| LOC-01 | Phase 10 | Pending |
| MCP-01 | Phase 10 | Pending |
| DOC-01 | Phase 10 | Pending |
| TEST-01 | Phase 6 | Complete |
| TEST-02 | Phase 7 | Pending |

**Coverage:**

- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-15*
*Last updated: 2026-06-16 after completing 08-02*
