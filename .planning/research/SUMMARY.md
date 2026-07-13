# Project Research Summary

**Project:** Valt — Asset Sold History feature (v0.5)
**Domain:** Personal finance / bitcoin-denominated investment tracking desktop app
**Researched:** 2026-07-13
**Confidence:** HIGH

## Executive Summary

Valt v0.5 adds a sold-asset history capability to the existing Assets module. The feature is a state-change concern rather than a new domain: it adds a `Sold` boolean and a `DateSold` date to the current `Asset` aggregate, filters sold assets out of active views and totals, and exposes a dedicated History modal for browsing and undoing sales. Experts build this kind of feature by keeping the state on the aggregate, separating active/sold queries at the infrastructure layer, and reusing the existing per-type display mapping for the history details panel.

The recommended approach is to implement a vertical slice inside the current .NET 10 / Avalonia 12 / LiteDB 5 / CQRS stack without adding new packages or frameworks. Because LiteDB is schemaless, the new fields can be added without a migration. The main risk is incomplete filtering: sold assets must be excluded from every active query (main grid, totals, reports, price updater) while remaining queryable for the History screen. The other key risk is over-engineering the sale as a transaction or ledger entry, which would complicate undo and violate the v0.5 scope of record-keeping only. These are mitigated by using a simple reversible flag and centralizing filters in `IAssetQueries`.

## Key Findings

### Recommended Stack

No new packages are required. The Asset Sold History feature is a vertical slice inside the existing Valt stack and reuses the same controls, MVVM framework, persistence store, and CQRS patterns already in production. See [STACK.md](./STACK.md) for the full technology and integration breakdown.

**Core technologies:**
- **.NET 10 / `net10.0`** — runtime and BCL; already targeted by all projects.
- **Avalonia 12.0.3** — existing UI framework; the History screen follows the same `Window` + `CustomTitleBar` + `DataGrid` pattern used by `LoanStateHistoryView`.
- **Avalonia.Controls.DataGrid 12.0.0** — already referenced; used for the sold-asset list.
- **LiteDB 5.0.21** — embedded document store; adding nullable `IsSold`/`DateSold` to `AssetEntity` is backward-compatible.
- **CommunityToolkit.Mvvm 8.4.2** — source-generated commands/properties used by every existing ViewModel.
- **ModelContextProtocol.AspNetCore 1.3.0** — existing MCP tool attributes; new tools can be added without infrastructure changes.

### Expected Features

The feature set is well-defined and limited to record-keeping. Sale price, capital gains, and tax-lot tracking are explicitly out of scope for v0.5. See [FEATURES.md](./FEATURES.md) for the full feature matrix and dependency graph.

**Must have (table stakes):**
- Mark asset as sold — core action that creates a sold record.
- Hide sold assets from the active Assets view — once sold, the asset is no longer a current holding.
- Exclude sold assets from totals and net worth — sold assets must not affect wealth or leverage calculations.
- History / archive list of sold assets — users need a way to see disposed assets without losing them.
- Undo Sell — users accidentally mark things sold; restoring is standard.
- Prompt for Date Sold when missing — a sale record is meaningless without a date.
- MCP exposure — AI assistants must be able to mark sold and undo.

**Should have (competitive):**
- Per-type asset details in History — reuse existing `AssetDTO`/`AssetViewModel` mapping for richer history details.
- History toolbar button on Assets tab — mirrors the established BTC loan "View History" pattern.
- Date Sold defaults to today, allows past dates — matches common investment apps.
- Undo Sell in the History screen — reduces context switching.

**Defer (v0.5.x / v0.6+):**
- History filters by year or asset type — useful once the list grows.
- Bulk mark as sold — if users ask for it.
- Sale price, capital gains, and tax-lot tracking — requires new domain concepts and is out of scope.
- Realized P&L in History — depends on sale price tracking.

### Architecture Approach

Add `IsSold` and `DateSold` as first-class properties on the `Asset` aggregate root and `AssetEntity`, not inside the JSON details blob. Filter sold assets at the query layer (`IAssetQueries`) so the UI, reports, MCP, and background jobs all share the same semantics. Reuse the existing `AssetViewModel` and `AssetDTO` mapping for the History details panel. See [ARCHITECTURE.md](./ARCHITECTURE.md) for the component diagram, data flow, and build order.

**Major components:**
1. **`Asset` / `AssetEntity`** — own the `IsSold`/`DateSold` fields and `Sell()`/`UndoSell()` mutation rules.
2. **`IAssetQueries` / `AssetQueries`** — add `GetActiveAssetsAsync`, `GetSoldAssetsAsync`, and update `GetVisibleAsync`/`GetSummaryAsync` to exclude sold assets.
3. **CQRS commands/queries** — `SellAssetCommand`, `UndoSellAssetCommand`, `GetActiveAssetsQuery`, `GetSoldAssetsQuery`.
4. **UI ViewModels** — `AssetsViewModel` switches to active queries; new `SoldAssetHistoryViewModel` reuses `AssetViewModel` for details.
5. **MCP tools** — `AssetTools` adds `SellAsset`, `UndoSellAsset`, and `GetSoldAssets`.
6. **Background jobs** — `AssetPriceUpdaterJob` skips sold assets to reduce API usage.

### Critical Pitfalls

See [PITFALLS.md](./PITFALLS.md) for the full checklist and phase mapping.

1. **Forgetting to filter sold assets from every active view and calculation** — centralize `Sold == false` filtering in `IAssetQueries` and audit all direct reads of `ILocalDatabase.GetAssets()`.
2. **Treating the sale as an immutable transaction before the app is ready** — use a simple reversible `Sold` flag and `DateSold` on the `Asset` entity; do not create a ledger or transaction record for the sale itself.
3. **Breaking backward compatibility with existing databases** — make `IsSold` default to `false` and `DateSold` nullable; avoid strict non-nullable deserialization.
4. **Overloading the `Visible` flag to mean "sold"** — keep `IsSold` independent from `Visible` so the two concepts do not collide on undo or filtering.
5. **Rebuilding per-type details logic instead of reusing existing mapping** — bind the History details panel to the same `AssetViewModel`/`AssetDTO` used on the main Assets tab.

## Implications for Roadmap

Based on research, the feature should be delivered in three tightly-scoped phases. The dependency chain is: domain/persistence → CQRS → UI/MCP. Because the architecture is additive and the patterns are already established in the codebase, most phases can be executed with standard Valt patterns rather than fresh research.

### Phase 1: Domain, Persistence, and Active-View Filtering
**Rationale:** The sold flag and query filters are prerequisites for every other part of the feature. Until the domain and query layers know about `IsSold`/`DateSold`, no UI work can be safely integrated.
**Delivers:** `Asset`/`AssetEntity` with `IsSold`/`DateSold`, mapping updates, `SellAsset`/`UndoSellAsset` commands, `GetActiveAssets`/`GetSoldAssets` queries, and updated summary/visible filters.
**Addresses:** Sold flag + Date Sold, hide from active view, exclude from totals, undo sell, MCP exposure (commands).
**Avoids:** Critical pitfalls 1, 2, 3, 4 — centralized filtering, no transaction ledger, backward compatibility, independent `Visible` flag.
**Research flag:** Standard patterns — no dedicated research phase needed; the patterns mirror `SetAssetVisibility` and `LoanStateHistory`.

### Phase 2: History UI and Reuse of Details Mapping
**Rationale:** Once the query layer exposes active and sold asset lists, the History modal can be built by reusing existing `AssetViewModel` and `DataGrid` patterns. This keeps the UI work isolated and reduces duplication.
**Delivers:** `SoldAssetHistoryView` + `ViewModel`, `ApplicationModalNames.SoldAssetHistory`, History toolbar button, per-type details panel, and Undo Sell action in the History screen.
**Addresses:** History screen, per-type details in History, undo sell in History, history toolbar button.
**Avoids:** Critical pitfalls 1, 5 — filtering already in place; details panel reuses existing mapping.
**Research flag:** Standard patterns — the `LoanStateHistory` modal is the direct template; no extra research needed.

### Phase 3: MCP, Localization, and Documentation
**Rationale:** These are the final integration and polish layers. MCP tools depend on the new commands; localization and docs are required before the feature can ship.
**Delivers:** `SellAsset`/`UndoSellAsset`/`GetSoldAssets` MCP tools, new localization strings in `en/pt/es`, `assets.md` documentation update, and end-to-end verification.
**Addresses:** MCP exposure, localization, documentation update.
**Avoids:** UX pitfalls around missing confirmation, hidden History button, and incomplete localization.
**Research flag:** Standard patterns — MCP tool pattern already exists; localization is mechanical.

### Phase Ordering Rationale

- **Dependencies are bottom-up:** domain → queries → UI → MCP. Reversing the order would force UI developers to work against unvalidated assumptions about filter behavior.
- **Filtering is the highest-risk concern:** If sold assets leak into totals or reports, the feature feels broken. Phase 1 addresses this before any UI is exposed.
- **UI reuse reduces risk:** Phase 2 deliberately reuses existing `AssetViewModel` and `DataGrid` patterns rather than inventing new display logic, preventing the duplicated-formatting trap.
- **MCP and docs are final gates:** Phase 3 ensures the feature is complete across all layers (AI assistant, user-visible strings, and project documentation).

### Research Flags

Phases likely needing deeper research during planning:
- **None** — the existing codebase provides clear templates for toggle commands, date-bearing mutations, history modals, and MCP tools. No external API or unfamiliar technology is involved.

Phases with standard patterns (skip research-phase):
- **Phase 1:** Domain flag + CQRS commands/queries follow `SetAssetVisibility`, `AddLoanStateUpdate`, and `GetVisibleAssets` patterns.
- **Phase 2:** History modal follows `LoanStateHistoryView` pattern; details reuse `AssetViewModel`.
- **Phase 3:** MCP tools follow `AssetTools` pattern; localization follows existing `language.resx` workflow.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Based on the existing Valt project files (`Directory.Packages.props`, csproj files, current Avalonia/LiteDB/CQRS stack). No new packages are required. |
| Features | MEDIUM | Cross-checked against project context (`.planning/PROJECT.md`, `.claude/docs/assets.md`) and several portfolio-app UX patterns from web sources (lower confidence). Feature set is constrained and aligns with v0.5 scope. |
| Architecture | HIGH | Derived directly from current Valt source files (`Asset.cs`, `AssetEntity.cs`, `AssetQueries.cs`, `AssetTools.cs`, `AssetsViewModel`, `LoanStateHistoryView`). The patterns are already implemented elsewhere. |
| Pitfalls | MEDIUM | Grounded in project context and standard desktop-app UX patterns; some competitor UX references are low-confidence web sources, but the prevention strategies are concrete and testable. |

**Overall confidence:** HIGH

### Gaps to Address

- **Existing `SellAsset` behavior today:** The current UI may already call `DeleteAssetCommand` after recording a sale transaction. Verify the exact current flow before replacing it with `SellAssetCommand`.
- **Date Sold default:** Confirm whether the transaction editor's saved date should be used directly as `DateSold`, or whether a separate date prompt is required. The research recommends defaulting to the transaction date or today.
- **Reports module impact:** Audit report queries that read assets directly (outside `IAssetQueries`) to ensure sold assets are excluded consistently.
- **MCP `GetAssets` semantics:** Decide whether `GetAssets` should return all assets (including sold, with the new flag) or only active assets. Research recommends keeping "all assets" semantics and adding `GetSoldAssets` separately.
- **AssetPriceUpdaterJob filter:** Confirm the job currently iterates all assets so the `!IsSold` filter can be added cleanly.

## Sources

### Primary (HIGH confidence)
- Valt project source files:
  - `Directory.Packages.props` — package versions and compatibility.
  - `src/Valt.Core/Modules/Assets/Asset.cs` — aggregate structure and mutation patterns.
  - `src/Valt.Infra/Modules/Assets/AssetEntity.cs` and `Extensions.cs` — persistence mapping.
  - `src/Valt.Infra/Modules/Assets/Queries/AssetQueries.cs` — query filtering and summary logic.
  - `src/Valt.App/Modules/Assets/Commands/SetAssetVisibility/` — template for simple toggle commands.
  - `src/Valt.App/Modules/Assets/Commands/AddLoanStateUpdate/` — template for date-bearing mutation commands.
  - `src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` and `AssetsView.axaml` — existing sell flow and UI layout.
  - `src/Valt.UI/Views/Main/Modals/LoanStateHistory/LoanStateHistoryViewModel.cs` and `.axaml` — modal history pattern.
  - `src/Valt.Infra/Mcp/Tools/AssetTools.cs` — existing MCP tool patterns.
- `.planning/PROJECT.md` — v0.5 scope and constraints.
- `.claude/docs/assets.md` — existing asset domain documentation.

### Secondary (MEDIUM confidence)
- Avalonia built-in control documentation for `DataGrid`, `ContentControl`, and `CalendarDatePicker` — no new package required.

### Tertiary (LOW confidence)
- My Stocks Portfolio help — "Hide closed positions" toggle.
- FinTide — "Asset Archive" for fully sold positions.
- TradingView Portfolios — "Display sold holdings" toggle.
- Investing.com support — close position requires closing date, amount sold, sale price, and commission.
- MoneyManagerEx docs — SELL transactions in the Stocks & Shares module.
- Ghostfolio GitHub/API — transaction types include `BUY` and `SELL`, modeling sales as transactions rather than flags.

---
*Research completed: 2026-07-13*
*Ready for roadmap: yes*
