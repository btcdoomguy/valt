# Milestones

## v0.6 Documentation Site Refresh (Shipped: 2026-07-17)

**Phases completed:** 7 phases, 17 plans, 42 tasks

**Key accomplishments:**

- Corrected factual errors in Valt public documentation (LiteDB database, CSV import/export, four main tabs, nine asset types, and Reports export behavior) across Portuguese and English pages, with the sibling `valt-docs` site building cleanly.
- Aligned ROADMAP.md and .claude/docs/assets.md with the code-corrected four main tabs and nine asset types, then verified the sibling `valt-docs` site builds cleanly with `mkdocs build --strict`.
- Rewrote the bilingual public Assets page to replace the outdated sale-transaction section with the actual Mark as Sold semantics, Asset Sold History, BTC-backed Loans, and Asset Groups sections.
- Verified the English Assets page already mirrors the Portuguese rewrite with Mark as Sold, Sold Asset History, BTC-Backed Loans, and Asset Groups sections, using terminology from language.resx.
- Ran the final docs quality gate for the Assets page rewrite: strict MkDocs build is green, bilingual heading parity is verified, and the content review checklist confirms the Portuguese and English pages are accurate, complete, and consistent with the Valt app language files.
- Rewrote the Portuguese Reports page to accurately document the custom BTC price simulation, current summary dashboards, wealth overview line chart, monthly totals line chart + data grid, categories horizontal bar charts, and the correct export behavior.
- Rewrote the English Reports page to mirror the Portuguese rewrite, using exact English UI labels from `language.resx`, and verified bilingual parity and MkDocs strict build.
- Updated Portuguese Goals page with all 10 current goal types, a dedicated price-data section, staleness/recalculation semantics, and three worked examples, verified by `mkdocs build --strict`.
- Mirrored the updated Portuguese Goals page into English with exact app terminology and verified bilingual heading parity and MkDocs strict build.
- Marked GOAL-01, GOAL-02, and GOAL-03 as Complete in REQUIREMENTS.md after verifying both Goals pages satisfy the requirements and the documentation site builds cleanly.
- Portuguese Fixed Expenses docs now cover the four record states with exact app labels, the yearly overview modal with conceptual out-of-range detection, and the account-vs-currency binding exclusivity — all sourced from app code and language files.
- English Fixed Expenses docs now mirror the Portuguese page exactly — four record states with exact app labels, yearly overview with conceptual out-of-range detection, and account-vs-currency exclusivity note — with the strict MkDocs build passing clean.
- Portuguese MCP Server page now documents the complete current toolset: AssetTools (28 tools, 5 grouped subsections, parameter tables for the 7 loan-state/sold-asset tools) plus IndicatorTools, with the stale 45-tool claim corrected to 80+ and an Ativos cross-link added.
- English MCP Server page now mirrors the complete Portuguese toolset documentation: AssetTools (28 tools, 5 grouped subsections, parameter tables for the 7 loan-state/sold-asset tools) plus IndicatorTools, with the stale 45-tool claim corrected to 80+ and an Assets cross-link — verified by bilingual parity greps and a green `mkdocs build --strict` gate.
- Created the bilingual Settings & Configuration page, wired it into the MkDocs navigation with dual translations, and converted three existing bare settings references into cross-links — strict build now translates 19 navigation elements.
- Replaced 7 drifted MCP tool category tables in both pt-BR and en-US docs with 61 code-verified tool names, eliminating 28 phantom names and restoring the documented count to 90 tools.
- Closed QA-03 by applying the 12-check content review matrix to all 18 v0.6 documentation files, produced the required 38-QA-CHECKLIST.md artifact, and marked Phase 38 / v0.6 milestone complete.

**Known deferred items at close:** 7 (see STATE.md Deferred Items)

**Known gaps/issues:**

- 38-VERIFICATION.md shows `gaps_found` because it was generated before the 38-03 QA sweep plan was executed; the QA-03 gap is closed by the committed 38-QA-CHECKLIST.md.

---

## v0.5 Asset Sold History (Shipped: 2026-07-14)

**Phases completed:** 3 phases, 12 plans, 27 tasks

**Key accomplishments:**

- Asset aggregate gains first-class IsSold, DateSold, and PreviousVisibility properties with MarkAsSold/UndoSale methods, LiteDB persistence mapping, and builder/test support.
- CQRS command/validator/handler pipelines for marking assets as sold and undoing sales, backed by domain and handler unit tests with IClock-driven date validation.
- Active/sold read-model split with AssetDTO sold-state fields, a new GetSoldAssets query, and query tests proving sold assets are excluded from active views and totals.
- AssetPriceUpdaterJob now returns false early in `ShouldUpdatePrice` when `asset.IsSold` is true, and unit tests prove the price provider is never invoked for sold assets.
- Built a chromeless 850x650 Asset Sold History modal with a DataGrid list, per-type details card, and Restore Asset undo action, integrated into the Assets tab toolbar.
- Wired the active Assets view to the MarkAssetAsSold command via a DateSoldPrompt modal, replacing the old "Sell" action with confirmation, date selection, optional proceeds transaction, and view refresh.
- Structured dialog result and caller-side refresh so a restored asset reappears in the main Assets tab immediately after undoing a sale from History.
- Exposed mark-sold, undo-sale, and list-sold-asset operations through the embedded MCP server via three new `AssetTools` methods that dispatch existing App-layer commands and queries.
- Asset module documentation now covers the sold-state domain behavior, the Sold Asset History UI flow, and the three new MCP tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`).
- NUnit integration tests prove the AssetTools mark/undo/list cycle and notification behavior end-to-end, with a restored GetLatestLoanState tool that unblocked the test project.

**Known deferred items at close:** 6 (see STATE.md Deferred Items)

**Known gaps/issues:**

- Two live-API integration tests (`CoinGeckoProviderTests`, `BitcoinDominanceProviderTests`) fail with `403 Forbidden` in the current environment; unrelated to v0.5 and deferred as out-of-scope.

---
