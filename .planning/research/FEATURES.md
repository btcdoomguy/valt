# Feature Research: Asset Sold History

**Domain:** Personal finance / investment tracking (desktop app for bitcoiners)  
**Researched:** 2026-07-13  
**Confidence:** MEDIUM — cross-checked multiple low-confidence web sources against the Valt project context.

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist for a sold-asset history. Missing these makes the feature feel broken.

| Feature | Why Expected | Complexity | Notes |
|---------|------------|------------|-------|
| **Mark asset as sold** | This is the core action that creates a sold record. | LOW | Add a `Sold` boolean and a `DateSold` field to the `Asset` aggregate. Emit an `AssetUpdatedEvent` so the UI refreshes. |
| **Hide sold assets from the active Assets view** | Once sold, an asset is no longer a current holding. | LOW | Filter `IAssetQueries.GetAllAsync()` and `GetVisibleAsync()` to `Sold == false`. Also hide sold assets from the main grid DataGrid. |
| **Exclude sold assets from totals and net worth** | Sold assets should not contribute to wealth or leverage calculations. | LOW | `AssetSummaryDTO`/`GetSummaryAsync` should skip assets where `Sold == true`. `IncludeInNetWorth` is orthogonal: a sold asset is excluded regardless of that flag. |
| **History / Archive list of sold assets** | Users need a way to see disposed assets without losing them. | MEDIUM | Add a new `GetSoldAssetsQuery` and a History modal/list showing asset name, type, and date sold. |
| **Undo Sell** | Users accidentally mark things sold; restoring is standard. | LOW | A command that clears `Sold` and `DateSold` and re-emits `AssetUpdatedEvent`. The asset returns to the active grid in its original display order. |
| **Prompt for Date Sold when missing** | A sale record is meaningless without a date. | LOW | Inline validation or a small modal; default to `DateTime.Today` but allow any past date. |
| **MCP exposure** | AI assistants must be able to mark sold and undo. | LOW | Add `MarkAssetSold` and `UndoAssetSale` tools in `AssetTools.cs`; reuse existing command patterns. |

### Differentiators (Competitive Advantage)

Features that are not universal but fit Valt's existing architecture and user value.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Per-type asset details in History** | Valt already supports 8 distinct asset types (stocks, ETFs, real estate, leveraged positions, BTC loans, etc.). Reusing the existing type-specific mapping gives a richer history view than simple name+date lists. | MEDIUM | Reuse `AssetDTO`/`MapToDto` and `AssetViewModel` formatting helpers so the History details panel shows the same fields as the main card. |
| **History toolbar button on Assets tab** | Mirrors the "View History" pattern already established for BTC loan state timelines. | LOW | Add a toolbar button to `AssetsView.axaml` and `AssetsViewModel` that opens the History modal. |
| **Date Sold defaults to today, allows past dates** | Matches common investment apps; useful for recording past sales. | LOW | Avalonia `CalendarDatePicker` with validation and a sensible default. |
| **Undo Sell in the History screen** | Reduces context switching; users fix mistakes where they discover them. | LOW | Bind an `UndoSellCommand` to the History row or details panel. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem related but are explicitly out of scope for this milestone.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Record sale price, capital gains, or tax lots** | Tax reporting seems valuable. | It is explicitly out of scope per `PROJECT.md`; requires cost-basis strategies, tax-year logic, and reporting that Valt does not have. | Defer to a future tax/gains milestone. Keep the current milestone as record-keeping only. |
| **Auto-detect sold assets from external data** | Convenience. | Valt has no broker or exchange integrations that would allow automated sell detection. | Manual "Mark as Sold" action only. |
| **Hard-delete sold assets from history** | Some users want to clean up. | Deleting destroys the record-keeping value the feature is built for. | Hide/archive via the `Sold` flag; keep the asset in the database. |
| **Permanently archive sold assets to a separate table/collection** | Cleaner separation. | Adds migration and query complexity for a desktop app; a flag is simpler and backward compatible. | Use a `Sold` flag on the existing `Asset` entity. |

## Feature Dependencies

```
Mark Asset Sold
    ├── requires──> Date Sold capture
    ├── requires──> Active-view filter (Sold == false)
    └── requires──> Totals exclusion (Sold == false)

History Screen
    ├── requires──> Sold flag query (GetSoldAssets)
    ├── requires──> Date Sold field
    └── requires──> Per-type details panel (AssetDTO mapping)

Undo Sell
    └── requires──> Reversible Sold flag (no immutable transaction ledger)

MCP Tools
    ├── requires──> MarkAssetSold command
    └── requires──> UndoAssetSale command
```

### Dependency Notes

- **Mark Asset Sold requires Date Sold**: A sale is not meaningful without a date. The command should require a `DateSold` value and validate it is not in the future.
- **Active-view filter requires Sold flag**: Every query that feeds the main UI (`GetAllAsync`, `GetVisibleAsync`) must exclude `Sold == true`. The Reports and summary queries also need to filter.
- **History Screen requires AssetDTO mapping**: The details panel should not build new formatting logic; it should reuse `AssetQueries.MapToDto` and `AssetViewModel` so all type-specific fields (leverage, LTV, rental income, etc.) render correctly.
- **Undo Sell requires a reversible flag**: Because the milestone does not record sale transactions, the only thing to undo is the `Sold`/`DateSold` state. Do not introduce a ledger; otherwise undo becomes complex.

## MVP Definition

### Launch With (v0.5)

- [ ] **Sold flag + Date Sold** — Add domain fields and an `Asset.MarkAsSold(DateOnly)` / `UndoSale()` API.
- [ ] **Hide from active Assets tab** — Filter main grid queries by `Sold == false`.
- [ ] **Exclude from totals** — Update `GetSummaryAsync` to skip sold assets.
- [ ] **History button on Assets toolbar** — Open a History modal from the main Assets tab.
- [ ] **History list with sale date** — Show sold assets sorted by `DateSold` descending.
- [ ] **Per-type details summary in History** — Selecting a sold asset shows the same type-specific details as the main asset card.
- [ ] **Undo Sell action** — Restore the asset from the History screen.
- [ ] **Date Sold prompt** — Default to today, require a date when the user marks an asset sold without one.
- [ ] **MCP tool exposure** — Add `MarkAssetSold` and `UndoAssetSale` to `AssetTools`.
- [ ] **Localization** — Update `language.resx`, `language.pt-BR.resx`, and `language.es.resx` for new strings.
- [ ] **Documentation update** — Update `.claude/docs/assets.md` with sold-history behavior.

### Add After Validation (v0.5.x)

- [ ] **History filters by year or asset type** — Useful once the history list grows.
- [ ] **Bulk mark as sold** — If users ask to sell many assets at once.

### Future Consideration (v0.6+)

- [ ] **Sale price, capital gains, and tax-lot tracking** — Requires new domain concepts (cost basis, proceeds, tax years) and is explicitly out of scope.
- [ ] **Realized P&L in History** — Depends on sale price tracking; defer.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Sold flag + Date Sold | HIGH | LOW | P1 |
| Hide sold assets from active view | HIGH | LOW | P1 |
| Exclude sold assets from totals | HIGH | LOW | P1 |
| History screen | HIGH | MEDIUM | P1 |
| Undo Sell | MEDIUM | LOW | P1 |
| Date Sold prompt | MEDIUM | LOW | P1 |
| Per-type details in History | MEDIUM | MEDIUM | P1 |
| MCP tool exposure | MEDIUM | LOW | P1 |
| History filters | MEDIUM | LOW | P2 |
| Bulk mark as sold | LOW | MEDIUM | P3 |

**Priority key:**  
- P1: Must have for launch  
- P2: Should have, add when possible  
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | My Stocks Portfolio | FinTide | TradingView | Investing.com | Valt Approach |
|---------|---------------------|---------|-------------|---------------|---------------|
| Hide/archive sold positions | Menu toggle: "Hide closed positions" | Asset Archive for fully sold positions | Toggle "Display sold holdings" (off by default) | "Close Position" moves it out of active list | `Sold` flag + active-query filter |
| Date sold | N/A (uses transactions) | N/A | Transaction date | Closing date required | `DateSold` field on `Asset` |
| Undo / reopen | N/A | N/A | Edit/delete transaction | Edit position manually | Dedicated `Undo Sell` command |
| Details in history | N/A | Realized P&L summary | Holdings view | Detailed quote link | Reuse existing `AssetDTO` details panel |
| Sale price / commission | N/A | N/A | Yes | Yes | Explicitly out of scope |

## UX Flow Examples

### 1. Marking an asset as sold from the main view

1. User selects an asset in the Assets DataGrid.
2. Context menu or toolbar shows **"Mark as Sold"**.
3. If no `DateSold` is present, a small modal or inline prompt appears with `DateSold` defaulting to today.
4. User confirms; the command sets `Sold = true` and `DateSold`.
5. The asset immediately disappears from the active grid.
6. The Assets summary panel recalculates and no longer includes the sold asset.
7. The loan / wealth Reports also refresh because the asset is excluded from totals.

### 2. Browsing sold assets in History

1. User clicks the **"History"** button on the Assets toolbar.
2. A History modal opens listing only sold assets.
3. The list is sorted by `DateSold` descending, with columns: Name, Type, Date Sold.
4. User selects a row; the right-hand details panel renders the same type-specific summary used on the main Assets card (quantity, price, value, LTV, rental income, etc.).
5. The user can close the modal or choose an action on the selected asset.

### 3. Undoing a sale

1. In the History screen, the user selects a sold asset and clicks **"Undo Sell"**.
2. A confirmation dialog may be shown (optional, low-risk reversible action).
3. The command clears `Sold = false` and `DateSold = null`.
4. The asset reappears in the main Assets grid at its original `DisplayOrder`.
5. Totals and reports recalculate and include the restored asset.

### 4. Marking as sold via MCP

1. AI assistant calls `MarkAssetSold(assetId, dateSold)`.
2. The command validates the asset exists and the date is not in the future.
3. On success, `McpDataChangedNotification` is published so the UI refreshes.
4. To revert, the assistant calls `UndoAssetSale(assetId)`.

## Sources

- **My Stocks Portfolio help** — "Hide closed positions" toggle (webfetch, LOW confidence)
- **FinTide** — "Asset Archive" moves fully sold positions out of the active portfolio (webfetch, LOW confidence)
- **TradingView Portfolios** — "Display sold holdings" toggle, with sold holdings hidden by default (webfetch, LOW confidence)
- **Investing.com support** — Close Position requires closing date, amount sold, sale price, and commission (webfetch, LOW confidence)
- **MoneyManagerEx docs** — SELL transactions in the Stocks & Shares module (webfetch, LOW confidence)
- **Ghostfolio GitHub/API** — Transaction types include `BUY` and `SELL`, modeling sales as transactions rather than flags (webfetch, LOW confidence)
- **Valt project documentation** — `.claude/docs/assets.md` and `.planning/PROJECT.md` define the existing asset domain, loan-state history UI, and v0.5 scope (HIGH confidence, project context)
