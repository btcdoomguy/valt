# Phase 33: Assets Page Rewrite - Context

**Gathered:** 2026-07-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Rewrite the public Valt **Assets** page (`valt-docs/docs/funcionalidades/ativos.md`) to document the current v0.5 asset features: Asset Sold History (Mark as Sold, Date Sold, History screen, Undo Sale), BTC-backed Loans (collateral, APR, LTV, liquidation, margin call, loan-state timeline), and Asset Groups (how assets are grouped in the UI). All changes must be mirrored in the English `.en.md` translation. No Valt application code is modified in this phase.

</domain>

<decisions>
## Implementation Decisions

### Selling semantics
- **D-01:** The existing "Vendendo um Ativo" section currently describes a sale transaction with P&L (quantity, price, date), which does not match the actual app behavior. It will be rewritten as a **"Mark as Sold"** section that explains the actual feature: right-click an asset, select "Mark as Sold", optionally enter a Date Sold, and the asset is hidden from the active view and moved to the Asset Sold History screen.

### Asset Sold History placement
- **D-02:** Asset Sold History (History screen, Undo Sale, Date Sold) will be documented as an **inline section on the Assets page**, not as a separate sub-page. The main Assets page will contain a dedicated section explaining how to open the History screen and restore a sold asset.

### BTC-backed Loans depth
- **D-03:** BTC-backed loans will be documented as a **concise conceptual overview** on the Assets page, covering what the loan type is, collateral, APR, LTV, liquidation, margin call, and the loan-state timeline. No worked example with multiple snapshots is required; keep the page consistent with the style of other feature pages.

### Asset Groups placement
- **D-04:** Asset Groups will be documented as an **inline section on the Assets page** only. No standalone Asset Groups page is required for this phase. The section will explain how assets are grouped in the UI and how grouping affects navigation and totals.

### the agent's Discretion
- None — all discussed areas were decided by the user.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap and requirements
- `.planning/ROADMAP.md` §Phase 33 — goal, success criteria, and requirements (AST-01, AST-02, AST-03).
- `.planning/REQUIREMENTS.md` — v0.6 documentation requirements; AST-01 through AST-03 are the scope for this phase.
- `.planning/PROJECT.md` — v0.6 milestone context; documentation site only, no app code changes.
- `.planning/STATE.md` — current milestone state and accumulated context.

### Documentation source of truth (internal module docs)
- `.claude/docs/assets.md` — accurate source for asset types, Asset Sold History, BTC-backed loans, BTC lending, Asset Groups, and MCP tools.

### Code references for content accuracy
- **Asset aggregate and sold state:** `src/Valt.Core/Modules/Assets/Asset.cs`
- **Asset types enum:** `src/Valt.Core/Modules/Assets/AssetTypes.cs`
- **BTC-backed loan details:** `src/Valt.Core/Modules/Assets/Details/BtcLoanDetails.cs`
- **Loan state snapshot:** `src/Valt.Core/Modules/Assets/Details/LoanStateSnapshot.cs`
- **Assets tab UI:** `src/Valt.UI/Views/Main/Tabs/Assets/AssetsView.axaml`, `src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs`
- **Sold Asset History modal:** `src/Valt.UI/Views/Main/Modals/SoldAssetHistory/SoldAssetHistoryView.axaml`
- **Update Loan State modal:** `src/Valt.UI/Views/Main/Modals/UpdateLoanState/UpdateLoanStateView.axaml`
- **Loan State History modal:** `src/Valt.UI/Views/Main/Modals/LoanStateHistory/LoanStateHistoryView.axaml`
- **MCP asset tools:** `src/Valt.Infra/Mcp/Tools/AssetTools.cs`
- **App terminology:** `src/Valt.UI/Lang/language.resx`, `src/Valt.UI/Lang/language.pt-BR.resx`, `src/Valt.UI/Lang/language.es.resx`

### Public docs source and site configuration
- `../valt-docs/docs/funcionalidades/ativos.md` — Portuguese Assets page to rewrite.
- `../valt-docs/docs/funcionalidades/ativos.en.md` — English Assets page to mirror.
- `../valt-docs/mkdocs.yml` — site configuration (no new pages expected in this phase).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable assets
- `.claude/docs/assets.md` contains accurate, current descriptions of asset behavior, sold-state semantics, BTC-backed loan calculations, and UI flows. It can be used as source material for the public docs.
- The Valt app language files (`src/Valt.UI/Lang/language.resx`, `language.pt-BR.resx`, `language.es.resx`) provide canonical Portuguese/English/Spanish terminology for UI labels and feature names.
- The existing public docs page already covers asset types, basic/real-estate/leveraged creation, price sources, and management. The rewrite adds the missing sold-history, loan, and grouping sections.

### Established patterns
- The public docs site is a Material for MkDocs site with Portuguese source files and `.en.md` English translations.
- The `valt-docs` repository is separate from the main `valt` application repository; this phase works across both repositories.
- Factual corrections from Phase 32 established that app language files and code are the source of truth for docs terminology.
- Phase 32 also established that source-evidence HTML comments should be placed above factual claims derived from the code.

### Integration points
- The plan must open/edit files in `../valt-docs` (sibling to the current workspace), not inside the current `valt` repository.
- `mkdocs build --strict` in `../valt-docs` should be used as a final verification gate to catch broken links and build errors.
- No new navigation entries are expected in this phase because all new content stays inline on the existing Assets page.

</code_context>

<specifics>
## Specific Ideas

- Rewrite the "Vendendo um Ativo" section to match the actual "Mark as Sold" feature. Explain that marking as sold does not record a sale transaction or calculate P&L; it simply moves the asset record to the History screen with a date.
- Add an inline "Asset Sold History" section that explains how to open the History screen from the Assets toolbar, what information is shown (Name, Type, Date Sold), and how to restore a sold asset with the Undo Sale action.
- Add a "BTC-backed Loans" section that explains the loan type, collateral, APR, LTV, liquidation, margin call, and the loan-state timeline. Mention that the latest recorded state snapshot wins for calculations and that existing loans are auto-seeded from their setup values.
- Add a "Asset Groups" section that explains how to create/manage groups, how assets are assigned to groups, and how grouping affects navigation and totals in the Assets tab.
- Mirror every Portuguese edit in the English `.en.md` file using the same terminology as the app language files.
- Keep the existing asset-type table and creation sections intact unless they conflict with the new content; the goal is to add missing sections, not to delete the existing page structure.

</specifics>

<deferred>
## Deferred Ideas

- A standalone "Asset Groups" page is deferred to a future phase if the inline section becomes too large or if navigation reorganization is desired.
- Video walkthroughs, screenshots, or diagrams for the Assets page are out of scope for this phase (see v2 requirements DOC-VID-01).
- Tax-lot tracking, sale price recording, and capital gains calculations remain out of scope as per the v0.5 milestone decisions.

</deferred>

---

*Phase: 33-Assets Page Rewrite*
*Context gathered: 2026-07-15*
