# Roadmap: Valt

## Milestones

- ✅ **v0.5 Asset Sold History** — Phases 29-31 (shipped 2026-07-14)
- 🚧 **v0.6 Documentation Site Refresh** — Phases 32-38 (planned)

## Phases

<details>
<summary>✅ v0.5 Asset Sold History (Phases 29-31) — SHIPPED 2026-07-14</summary>

- [x] **Phase 29: Domain, Persistence, and Active-View Filtering** — 4/4 plans — completed 2026-07-13
- [x] **Phase 30: History UI and Details Reuse** — 3/3 plans — completed 2026-07-13
- [x] **Phase 31: MCP, Localization, Documentation, and Verification** — 5/5 plans — completed 2026-07-14

</details>

### 🚧 v0.6 — Documentation Site Refresh (Planned)

- [ ] **Phase 32: Factual Fixes and Cross-Page Accuracy** — 5 requirements
- [ ] **Phase 33: Assets Page Rewrite** — 3 requirements
- [ ] **Phase 34: Reports Page Update** — 3 requirements
- [ ] **Phase 35: Goals Page Completion** — 3 requirements
- [ ] **Phase 36: Fixed Expenses Page Enhancement** — 3 requirements
- [ ] **Phase 37: MCP Server Page Update** — 3 requirements
- [ ] **Phase 38: Navigation, New Pages, and Quality Assurance** — 6 requirements

## Phase Details

### Phase 32: Factual Fixes and Cross-Page Accuracy
**Goal**: Readers no longer encounter factual errors or outdated claims on the core guide and reference pages.
**Depends on**: Nothing (first phase of v0.6)
**Requirements**: ACC-01, ACC-02, ACC-03, ACC-04, ACC-05
**Success Criteria** (what must be TRUE):
  1. A reader on the Installation page sees LiteDB named as the local database instead of SQLite.
  2. The FAQ page no longer claims there is no automatic import functionality.
  3. The Getting Started page lists all five current main tabs: Transactions, Reports, Average Price, Assets, and Goals.
  4. The Assets page enumerates all eight current asset types, including `BtcLoan`.
  5. The Reports page accurately describes the current export behavior and removes any outdated "in development" note if the feature is available.
**Plans**: TBD

### Phase 33: Assets Page Rewrite
**Goal**: The Assets page fully explains how asset groups, BTC-backed loans, and Asset Sold History work in the current app.
**Depends on**: Phase 32
**Requirements**: AST-01, AST-02, AST-03
**Success Criteria** (what must be TRUE):
  1. A reader can learn how to mark an asset as sold, set the sale date, open the Asset Sold History screen, and undo a sale.
  2. A reader can understand BTC-backed loans, including collateral, APR, LTV, liquidation, margin call, and the loan-state timeline.
  3. A reader can see how Asset Groups organize assets in the UI and what grouping means for navigation and totals.
**Plans**: TBD

### Phase 34: Reports Page Update
**Goal**: The Reports page reflects the current dashboard, custom BTC price simulation, and export behavior.
**Depends on**: Phase 33
**Requirements**: RPT-01, RPT-02, RPT-03
**Success Criteria** (what must be TRUE):
  1. A reader can learn how to use the custom BTC price simulation feature in reports.
  2. A reader can understand the current dashboard components, including wealth summary, BTC stack, and leverage/loan summary.
  3. A reader can see an accurate description of report export behavior.
**Plans**: TBD

### Phase 35: Goals Page Completion
**Goal**: The Goals page documents every current goal type, price-data behavior, and automatic recalculation semantics.
**Depends on**: Phase 34
**Requirements**: GOAL-01, GOAL-02, GOAL-03
**Success Criteria** (what must be TRUE):
  1. A reader can see all current goal types documented, including `SaveFiat`, `SavingsRate`, and `NetWorthBtc`.
  2. A reader understands the price-data asterisk behavior for goals that require exchange-rate data.
  3. A reader understands when and how goal progress recalculates automatically and what stale progress means.
**Plans**: TBD

### Phase 36: Fixed Expenses Page Enhancement
**Goal**: The Fixed Expenses page documents record states, the yearly overview, and account-vs-currency binding rules.
**Depends on**: Phase 35
**Requirements**: FXE-01, FXE-02, FXE-03
**Success Criteria** (what must be TRUE):
  1. A reader can understand the four record states (Paid, ManuallyPaid, Ignored, Empty) and their effect on a fixed expense.
  2. A reader can understand the yearly overview and how out-of-range records are detected.
  3. A reader understands that a fixed expense is bound to either an account or a currency, not both.
**Plans**: TBD

### Phase 37: MCP Server Page Update
**Goal**: The MCP Server page lists the complete current toolset, including AssetTools, loan-state tools, and sold-asset tools.
**Depends on**: Phase 36
**Requirements**: MCP-01, MCP-02, MCP-03
**Success Criteria** (what must be TRUE):
  1. A reader can see the `AssetTools` category and its purpose on the MCP Server page.
  2. A reader can see the documented loan-state tools (`AddLoanStateUpdate`, `DeleteLoanStateUpdate`, `GetLoanStateTimeline`, `GetLatestLoanState`) and their parameters.
  3. A reader can see the documented sold-asset tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`) and their parameters.
**Plans**: TBD

### Phase 38: Navigation, New Pages, and Quality Assurance
**Goal**: The site navigation is updated, new pages are added or deferred explicitly, and the full documentation site builds and passes review.
**Depends on**: Phase 37
**Requirements**: NAV-01, NAV-02, NAV-03, QA-01, QA-02, QA-03
**Success Criteria** (what must be TRUE):
  1. The `mkdocs.yml` navigation is updated to include any new top-level pages or sections.
  2. New or renamed pages have consistent titles in both Portuguese and English navigation translations.
  3. A Settings & Configuration page exists, or its deferral to a future milestone is explicitly documented.
  4. Every content change in a Portuguese page is mirrored in the corresponding English `.en.md` file.
  5. The documentation site builds successfully with `mkdocs build` without errors or broken internal links.
  6. A content review checklist is applied to all updated pages for accuracy, completeness, and tone.
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 29. Domain, Persistence, and Active-View Filtering | v0.5 | 4/4 | Complete | 2026-07-13 |
| 30. History UI and Details Reuse | v0.5 | 3/3 | Complete | 2026-07-13 |
| 31. MCP, Localization, Documentation, and Verification | v0.5 | 5/5 | Complete | 2026-07-14 |
| 32. Factual Fixes and Cross-Page Accuracy | v0.6 | 0/0 | Not started | - |
| 33. Assets Page Rewrite | v0.6 | 0/0 | Not started | - |
| 34. Reports Page Update | v0.6 | 0/0 | Not started | - |
| 35. Goals Page Completion | v0.6 | 0/0 | Not started | - |
| 36. Fixed Expenses Page Enhancement | v0.6 | 0/0 | Not started | - |
| 37. MCP Server Page Update | v0.6 | 0/0 | Not started | - |
| 38. Navigation, New Pages, and Quality Assurance | v0.6 | 0/0 | Not started | - |

**Total phases:** 10  
**v0.6 plans:** 0/0 (not yet planned)  
**v0.6 tasks:** 0/0 (not yet planned)

---
*Last updated: 2026-07-14 after v0.6 roadmap creation*
