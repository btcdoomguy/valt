# Roadmap: Valt

**Milestone:** v0.5 Asset Sold History  
**Created:** 2026-07-13  
**Continues from:** Phase 28

## Overview

This milestone delivers the Asset Sold History feature: users can mark assets as sold, record a sale date, hide sold assets from the active Assets view and calculations, browse sold assets in a dedicated History screen, inspect per-type details, and undo a sale. Phases continue from v0.4, which ended at Phase 28.

## Milestone Goal

Let users mark assets as sold, record the sale date, hide them from the active Assets view and calculations, browse sold assets in a dedicated History screen, inspect per-type details, and undo a sale.

## Phases

- [x] **Phase 29: Domain, Persistence, and Active-View Filtering** - Add sold state to the asset aggregate, persistence, and queries; ensure sold assets are excluded from active views, totals, and price updates. (completed 2026-07-13)
- [ ] **Phase 30: History UI and Details Reuse** - Build the History modal with sold-asset listing, per-type details panel, and Undo Sell action.
- [ ] **Phase 31: MCP, Localization, Documentation, and Verification** - Expose sold-asset operations through MCP, localize new strings, update documentation, and run end-to-end verification.

## Phase Details

### Phase 29: Domain, Persistence, and Active-View Filtering

**Goal:** Users can mark assets as sold, and sold assets are automatically excluded from active views and calculations while remaining reversible.

**Depends on:** Phase 28 (v0.4 completed)

**Requirements:** ASSET-01, ASSET-02, ASSET-03, ASSET-04, ASSET-05, ASSET-06, TEST-01, TEST-02

**Success Criteria** (what must be TRUE):

1. User can mark an asset as sold and record a Date Sold (defaulting to today, accepting past dates).
2. Sold assets are hidden from the main Assets tab and excluded from asset totals, net worth, and leverage calculations.
3. User can undo a sale to restore the asset to the active view with its prior visibility state.
4. Asset price updater skips sold assets during price updates.
5. Unit tests pass for sell/undo command validation and active/sold query filters.

**Plans:** 4/4 plans complete

Plans:
**Wave 1**

- [x] 29-01-PLAN.md — Domain, persistence, and test-builder sold-state foundation

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 29-02-PLAN.md — MarkAssetAsSold and UndoAssetSale commands with validation tests
- [x] 29-03-PLAN.md — Active/sold query split, DTO fields, and query tests
- [x] 29-04-PLAN.md — AssetPriceUpdaterJob skips sold assets and job tests

**UI hint:** yes

### Phase 30: History UI and Details Reuse

**Goal:** Users can browse sold assets in a dedicated History screen and inspect per-type details with undo capability.

**Depends on:** Phase 29

**Requirements:** HIST-01, HIST-02, HIST-03, HIST-04

**Success Criteria** (what must be TRUE):

1. User can open a History screen from the Assets toolbar.
2. History screen lists sold assets sorted by Date Sold descending with name, type, and date sold.
3. Selecting a sold asset shows a per-type details summary matching the main asset card.
4. User can undo a sale directly from the History screen.

**Plans:** TBD

**UI hint:** yes

### Phase 31: MCP, Localization, Documentation, and Verification

**Goal:** The feature is fully accessible to AI assistants, localized, documented, and verified end-to-end.

**Depends on:** Phase 30

**Requirements:** MCP-01, MCP-02, MCP-03, DOCS-01, DOCS-02, TEST-03

**Success Criteria** (what must be TRUE):

1. AI assistant can mark an asset as sold, undo a sale, and list sold assets via MCP tools.
2. All new user-facing strings are localized in English, Portuguese, and Spanish.
3. Asset documentation is updated with sold-history behavior and MCP impact.
4. End-to-end verification passes for mark sold, history browse, details panel, undo, and totals refresh.

**Plans:** TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 29 → 30 → 31

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 29. Domain, Persistence, and Active-View Filtering | 4/4 | Complete    | 2026-07-13 |
| 30. History UI and Details Reuse | 0/TBD | Not started | - |
| 31. MCP, Localization, Documentation, and Verification | 0/TBD | Not started | - |

**Total phases:** 3
**Total v1 requirements mapped:** 18
**Coverage:** 18/18 ✓

---
*Last updated: 2026-07-13 after milestone v0.5 roadmap creation*
