# Requirements: Valt v0.5 Asset Sold History

**Defined:** 2026-07-13
**Core Value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.

## v1 Requirements

Requirements for the Asset Sold History milestone. Each maps to roadmap phases.

### Asset Sold State

- [ ] **ASSET-01**: User can mark an asset as sold with a recorded `Date Sold`
- [ ] **ASSET-02**: Sold assets are hidden from the main Assets tab grid
- [ ] **ASSET-03**: Sold assets are excluded from asset totals, net worth, and leverage calculations
- [ ] **ASSET-04**: User can undo a sale, restoring the asset to the active view with its prior visibility state
- [ ] **ASSET-05**: `Date Sold` defaults to today and accepts past dates when not provided at sale time
- [ ] **ASSET-06**: Asset price updater skips sold assets to avoid unnecessary API calls

### History Screen

- [ ] **HIST-01**: User can open a History screen from the Assets toolbar (beside Add Asset and Manage Groups)
- [ ] **HIST-02**: History screen lists sold assets sorted by `Date Sold` descending with name, type, and date sold
- [ ] **HIST-03**: Selecting a sold asset in History shows a per-type details summary using the same mapping as the main asset card
- [ ] **HIST-04**: User can undo a sale directly from the History screen

### MCP Tools

- [ ] **MCP-01**: AI assistant can mark an asset as sold via MCP tool
- [ ] **MCP-02**: AI assistant can undo a sale via MCP tool
- [ ] **MCP-03**: AI assistant can list sold assets via MCP tool

### Localization & Documentation

- [ ] **DOCS-01**: All new user-facing strings are localized in `language.resx`, `language.pt-BR.resx`, and `language.es.resx`
- [ ] **DOCS-02**: `.claude/docs/assets.md` is updated with sold-history behavior and MCP impact

### Verification

- [ ] **TEST-01**: Unit tests cover `SellAsset` and `UndoSellAsset` command validation and state changes
- [ ] **TEST-02**: Query tests verify active-asset and sold-asset filters and totals exclusion
- [ ] **TEST-03**: End-to-end verification covers mark sold, history browse, details panel, undo, and totals refresh

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### History Enhancements

- **HIST-05**: User can filter history by year or asset type
- **HIST-06**: User can bulk mark multiple assets as sold

### Advanced Sale Tracking

- **ASSET-07**: User can record sale price, proceeds, and commission
- **ASSET-08**: System calculates realized capital gains/losses per sold asset
- **ASSET-09**: Tax-lot tracking and tax-year reporting for sold assets

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Sale price, capital gains, or tax-lot tracking | Out of scope for v0.5; record-keeping only. Requires new domain concepts and a dedicated tax/gains milestone. |
| Auto-detect sold assets from external data | Valt has no broker/exchange integrations; manual Mark as Sold only. |
| Hard-delete sold assets from history | Defeats the record-keeping purpose of the feature. |
| Separate archive collection/table for sold assets | Adds migration/query complexity; a flag on the existing entity is simpler and backward-compatible. |
| Realized P&L in History | Depends on sale price tracking, which is deferred. |
| v0.4 quality/hardening items | Deferred to a future quality milestone; v0.5 is focused on Asset Sold History. |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| ASSET-01 | Phase 1 | Pending |
| ASSET-02 | Phase 1 | Pending |
| ASSET-03 | Phase 1 | Pending |
| ASSET-04 | Phase 1 | Pending |
| ASSET-05 | Phase 1 | Pending |
| ASSET-06 | Phase 1 | Pending |
| HIST-01 | Phase 2 | Pending |
| HIST-02 | Phase 2 | Pending |
| HIST-03 | Phase 2 | Pending |
| HIST-04 | Phase 2 | Pending |
| MCP-01 | Phase 3 | Pending |
| MCP-02 | Phase 3 | Pending |
| MCP-03 | Phase 3 | Pending |
| DOCS-01 | Phase 3 | Pending |
| DOCS-02 | Phase 3 | Pending |
| TEST-01 | Phase 1 | Pending |
| TEST-02 | Phase 1 | Pending |
| TEST-03 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-13*
*Last updated: 2026-07-13 after initial definition*
