# Domain Pitfalls: Asset Sold History

**Domain:** Personal finance / investment tracking (desktop app for bitcoiners)  
**Researched:** 2026-07-13  
**Confidence:** MEDIUM — based on project context and cross-checked UX patterns from portfolio apps.

## Critical Pitfalls

### Pitfall 1: Forgetting to filter sold assets from every active view and calculation

**What goes wrong:**  
Sold assets keep appearing in the main Assets grid, totals, or Reports. The feature looks broken because disposed assets still affect wealth, leverage, and net-worth numbers.

**Why it happens:**  
Developers add the `Sold` flag and filter the main grid query, but miss the summary query, the Reports module, or any ad-hoc asset enumerations in the infrastructure layer.

**How to avoid:**  
Add the `Sold == false` filter in a single place: the query layer (`IAssetQueries.GetAllAsync`, `GetVisibleAsync`, `GetSummaryAsync`). Audit all call sites that read from `ILocalDatabase.GetAssets()` directly and route them through the query interface or apply the same filter.

**Warning signs:**  
- Sold asset still visible after marking it sold.  
- Total portfolio value does not drop after a sale.  
- Report charts include a removed asset.

**Phase to address:**  Phase 1 — Mark as Sold + Active view filtering.

---

### Pitfall 2: Treating the sale as an immutable transaction before the app is ready

**What goes wrong:**  
A "Mark as Sold" action records a sale transaction or a separate ledger entry. Undoing then requires deleting or reversing a transaction, which is complex and may introduce partially-completed data.

**Why it happens:**  
Portfolio apps like Ghostfolio or MMEX model sales as SELL transactions, so developers instinctively follow that pattern. But the v0.5 scope is record-keeping only, not tax-lot tracking.

**How to avoid:**  
Use a simple reversible `Sold` flag + `DateSold` on the existing `Asset` entity. The "Undo Sell" action then just clears the flag and date. No transaction history is created; this matches the PROJECT.md scope of "sold history is for record-keeping only."

**Warning signs:**  
- Undo Sell starts requiring sale price, commission, or capital-gains recalculation.  
- Domain events suggest a ledger or transaction is being created.

**Phase to address:**  Phase 1 — Mark as Sold / Undo Sell.

---

### Pitfall 3: Breaking backward compatibility with existing databases

**What goes wrong:**  
Adding a `Sold` field to the domain or DTO causes deserialization failures for existing user data. The app crashes or reports corrupt data on first run after the update.

**Why it happens:**  
LiteDB stores the `AssetEntity` as JSON. If the new fields are non-nullable and the serializer is strict, legacy documents will fail to load. A migration is required if the field is not optional.

**How to avoid:**  
Make `Sold` default to `false` and `DateSold` nullable in the DTO and entity. Use a safe default in the serializer (e.g., `JsonSerializerOptions` with `DefaultIgnoreCondition` or explicit default handling) so legacy documents deserialize correctly without a migration. This aligns with the PROJECT.md constraint: "Existing user databases ... must continue to work without migration."

**Warning signs:**  
- Unit tests fail when loading pre-existing asset documents.  
- Integration tests using real local database files fail on deserialization.  
- `DatabaseTest` base-class tests report `NullReferenceException` or missing fields.

**Phase to address:**  Phase 1 — Domain / persistence changes.

---

### Pitfall 4: Overloading the `Visible` flag to mean "sold"

**What goes wrong:**  
Sold assets are hidden by setting `Visible = false`. Later, users cannot hide active assets without accidentally archiving them, or they cannot distinguish truly hidden assets from sold ones.

**Why it happens:**  
The `Asset` entity already has a `Visible` property. It is tempting to reuse it for the sold-state filter to avoid a new field. But `Visible` controls UI visibility, while `Sold` is a lifecycle state; conflating them loses semantic meaning.

**How to avoid:**  
Add a separate `Sold` boolean. The active grid filters on both `Visible == true` (where appropriate) and `Sold == false`. The History screen queries on `Sold == true`. Let users keep `Visible` independent even for sold assets if desired.

**Warning signs:**  
- Toggle visibility accidentally moves an asset to History.  
- Undo Sell does not restore an asset because `Visible` was also toggled off.  
- Reports or summaries start treating every hidden asset as sold.

**Phase to address:**  Phase 1 — Mark as Sold / filtering.

---

### Pitfall 5: Rebuilding per-type details logic instead of reusing existing mapping

**What goes wrong:**  
The History screen shows only generic fields (name, date) or duplicates the same formatting code already in `AssetViewModel`. Future asset-type changes must be updated in two places.

**Why it happens:**  
The History screen is new, so developers write a new `SoldAssetViewModel` from scratch. Valt has 8 asset types with bespoke formatting (leverage, LTV, rental income, BTC loan snapshots, etc.).

**How to avoid:**  
Reuse the existing `AssetDTO` and `AssetViewModel` mapping. The History list can bind to the same `AssetViewModel` type used on the main tab; the details panel can use the same XAML or viewmodel helpers. The only difference is the query source (`Sold == true`).

**Warning signs:**  
- New viewmodels for sold assets duplicate `IsBasicAsset`, `IsRealEstate`, `IsLeveragedPosition`, etc.  
- Details panel is missing fields that appear on the main card.  
- Formatting strings diverge between active and sold views.

**Phase to address:**  Phase 2 — History screen / details panel.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Add `Sold` directly to `AssetEntity` without a domain method | Faster to implement | Business rules for state changes live in UI/command handlers; harder to test | Never — add `MarkAsSold` / `UndoSale` methods on the `Asset` aggregate so domain tests cover it. |
| Query filtering done in the ViewModel instead of the query layer | Quick UI fix | Every consumer of asset data must re-implement the filter; inconsistent behavior | Never — filters belong in `IAssetQueries`. |
| Skip localization for new strings | Saves time | App ships mixed-language strings; downstream users cannot use the feature | Never — Valt requires en/pt/es strings plus the Designer file. |
| Add new MCP tools but forget `ForwardServicesFromMainApp` | Tests pass | MCP server fails at runtime because dependencies are missing | Never — include MCP tool registration in the checklist. |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No confirmation on Undo Sell | User may accidentally restore an asset to active view | Show a lightweight confirmation dialog or a toast with an undo option. |
| Date Sold defaults to a fixed value silently | Users may record the wrong sale date | Default to today but show the field prominently; validate if the user leaves it empty. |
| History button hidden in a menu | Users cannot find disposed assets | Place the History button on the Assets toolbar, consistent with the BTC loan "View History" pattern. |
| Sold assets still appear in Reports | Reports feel inaccurate | Apply the same `Sold == false` filter to all report queries that include assets. |
| Per-type details are read-only but look editable | Users try to edit an archived asset and get confused | Clearly label the History details panel as read-only or disable input controls. |

## "Looks Done But Isn't" Checklist

- [ ] **Sold flag added:** Verify that `AssetEntity`, `AssetDTO`, and the domain `Asset` all expose `Sold` and `DateSold` consistently.
- [ ] **Active grid filtered:** Verify the main Assets tab excludes sold assets.
- [ ] **Totals filtered:** Verify `GetSummaryAsync` and Reports exclude sold assets.
- [ ] **History list exists:** Verify there is a way to open and view sold assets.
- [ ] **Details panel works:** Verify selecting a sold asset shows type-specific details (basic, real estate, leveraged, BTC loan, BTC lending).
- [ ] **Undo Sell works:** Verify the asset reappears in the active grid and totals update.
- [ ] **Date validation:** Verify future dates are rejected and empty dates are prompted.
- [ ] **MCP tools updated:** Verify `MarkAssetSold` and `UndoAssetSale` exist and are forwarded from the main DI container.
- [ ] **Localization:** Verify new strings are present in all three language files and the `.Designer.cs` file.
- [ ] **Backward compatibility:** Verify existing databases load without migration.
- [ ] **Documentation:** Verify `.claude/docs/assets.md` is updated with sold-history behavior.

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Missing active-view / totals filter | Phase 1 | UI test: sold asset disappears; summary value drops; reports refresh. |
| Over-engineering sale transactions | Phase 1 | Domain test: undo simply clears flag; no transaction/ledger created. |
| Backward compatibility break | Phase 1 | Database test: load existing asset document without migration. |
| Confusing `Visible` with `Sold` | Phase 1 | Unit test: toggling visibility does not affect `Sold`; history query only uses `Sold`. |
| Duplicated details formatting | Phase 2 | UI review: History details panel uses same `AssetViewModel` as main tab. |
| Missing localization | Phase 3 | Resx diff: new strings in all three language files. |
| MCP runtime failure | Phase 3 | Integration test: invoke `MarkAssetSold` and `UndoAssetSale` via MCP. |

## Sources

- Valt project context — `.planning/PROJECT.md` scope and `.claude/docs/assets.md` architecture (HIGH confidence, project context)
- My Stocks Portfolio, FinTide, TradingView, Investing.com UX patterns (LOW confidence, webfetch)
- MoneyManagerEx and Ghostfolio transaction-based sale models (LOW confidence, webfetch)
