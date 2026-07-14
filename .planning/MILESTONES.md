# Milestones

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
