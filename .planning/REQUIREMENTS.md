# Requirements: Valt

**Defined:** 2026-07-14
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v1 Requirements

### Content Accuracy

- [x] **ACC-01**: The Installation page correctly states that Valt uses LiteDB as its local database (not SQLite).
- [x] **ACC-02**: The FAQ page no longer claims there is no automatic import functionality.
- [x] **ACC-03**: The Getting Started page lists all current main application tabs (Transactions, Reports, Average Prices, Assets).
- [x] **ACC-04**: The Assets page correctly lists all nine current asset types, including `BtcLoan` and `BtcLending`.
- [x] **ACC-05**: The Reports page removes the outdated "in development" note for report export if the feature is available, or accurately describes its current status.

### Feature Coverage — Assets

- [x] **AST-01**: The Assets page documents the Asset Sold History feature, including Mark as Sold, Date Sold, History screen, and Undo Sale.
- [x] **AST-02**: The Assets page documents BTC-backed loans, including collateral, APR, LTV, liquidation, margin call, and the loan-state timeline.
- [x] **AST-03**: The Assets page documents Asset Groups and how assets are grouped in the UI.

### Feature Coverage — Reports

- [ ] **RPT-01**: The Reports page documents the custom BTC price simulation feature.
- [ ] **RPT-02**: The Reports page documents the current dashboard components, including wealth summary, BTC stack, and leverage/loan summary.
- [ ] **RPT-03**: The Reports page accurately describes report export behavior.

### Feature Coverage — Goals

- [ ] **GOAL-01**: The Goals page documents all current goal types, including `SaveFiat`, `SavingsRate`, and `NetWorthBtc`.
- [ ] **GOAL-02**: The Goals page explains the price-data asterisk behavior for goals that require exchange-rate data.
- [ ] **GOAL-03**: The Goals page documents automatic recalculation and staleness behavior.

### Feature Coverage — Fixed Expenses

- [ ] **FXE-01**: The Fixed Expenses page documents record states (Paid, ManuallyPaid, Ignored, Empty).
- [ ] **FXE-02**: The Fixed Expenses page documents the yearly overview and out-of-range detection.
- [ ] **FXE-03**: The Fixed Expenses page clarifies the account-vs-currency binding exclusivity.

### Feature Coverage — MCP Server

- [ ] **MCP-01**: The MCP Server page documents the `AssetTools` category and its tools.
- [ ] **MCP-02**: The MCP Server page documents loan-state tools (`AddLoanStateUpdate`, `DeleteLoanStateUpdate`, `GetLoanStateTimeline`, `GetLatestLoanState`).
- [ ] **MCP-03**: The MCP Server page documents sold-asset tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`).

### Structure and Navigation

- [ ] **NAV-01**: The `mkdocs.yml` navigation is updated to include any new top-level pages or sections.
- [ ] **NAV-02**: New or renamed pages have consistent titles in both Portuguese and English navigation translations.
- [ ] **NAV-03**: A new Settings & Configuration page is added if it is created, or explicitly deferred to a future milestone.

### Quality Assurance

- [x] **QA-01**: Every content change in a Portuguese page is mirrored in the corresponding English `.en.md` file.
- [x] **QA-02**: The documentation site builds successfully with `mkdocs build` without errors or broken internal links.
- [x] **QA-03**: A content review checklist is applied to all updated pages for accuracy, completeness, and tone.

## v2 Requirements

### Future Documentation Enhancements

- **DOC-VID-01**: Add video walkthroughs or screenshots to key feature pages.
- **DOC-CLI-01**: Document the MCP server command-line configuration options.
- **DOC-MOB-01**: Document mobile or web port considerations if the project expands platforms.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Changes to Valt application code | This milestone updates only the public documentation site (`valt-docs`). |
| New application features (e.g., tax-lot tracking, sale price recording) | These are application features, not documentation updates. |
| Full redesign of the documentation site theme or branding | Out of scope; the existing Material theme is retained. |
| Translation to languages other than pt-BR and en-US | Only the existing two languages are maintained. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ACC-01 | Phase 32 | Complete |
| ACC-02 | Phase 32 | Complete |
| ACC-03 | Phase 32 | Complete |
| ACC-04 | Phase 32 | Complete |
| ACC-05 | Phase 32 | Complete |
| AST-01 | Phase 33 | Complete |
| AST-02 | Phase 33 | Complete |
| AST-03 | Phase 33 | Complete |
| RPT-01 | Phase 34 | Pending |
| RPT-02 | Phase 34 | Pending |
| RPT-03 | Phase 34 | Pending |
| GOAL-01 | Phase 35 | Pending |
| GOAL-02 | Phase 35 | Pending |
| GOAL-03 | Phase 35 | Pending |
| FXE-01 | Phase 36 | Pending |
| FXE-02 | Phase 36 | Pending |
| FXE-03 | Phase 36 | Pending |
| MCP-01 | Phase 37 | Pending |
| MCP-02 | Phase 37 | Pending |
| MCP-03 | Phase 37 | Pending |
| NAV-01 | Phase 38 | Pending |
| NAV-02 | Phase 38 | Pending |
| NAV-03 | Phase 38 | Pending |
| QA-01 | Phase 38 | Complete |
| QA-02 | Phase 38 | Complete |
| QA-03 | Phase 38 | Complete |

**Coverage:**

- v1 requirements: 26 total
- Mapped to phases: 26
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-14*
*Last updated: 2026-07-14 after v0.6 roadmap creation*
