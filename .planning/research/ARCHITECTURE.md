# Architecture Research: Asset Sold History

**Domain:** Personal finance desktop application (bitcoin-denominated budget/assets)
**Researched:** 2026-07-13
**Confidence:** HIGH

## Executive Summary

Asset Sold History is a state-change feature, not a new entity. It adds a `Sold` flag and a `Date Sold` to the existing `Asset` aggregate, then uses the existing CQRS + LiteDB infrastructure to filter sold assets out of active views and totals while keeping them in a dedicated History screen. The implementation reuses the existing `AssetViewModel` for per-type details, follows the same command/query/validator pattern as the loan-state feature, and requires only additive changes to the entity and DTO shapes.

The recommended architecture keeps `IsSold` and `DateSold` as first-class properties on `Asset`/`AssetEntity` (not inside the JSON details blob), so every query can filter on them without deserializing details. It also keeps `Visible` independent from `Sold`, preserving the user's visibility choice when an asset is sold and later restored.

## Standard Architecture (Existing + New)

### System Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                              Valt.UI                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐      │
│  │ AssetsView       │  │ SoldAssetHistory │  │ AssetViewModel   │      │
│  │ (active list)    │  │ modal            │  │ (details reuse)  │      │
│  └────────┬─────────┘  └────────┬─────────┘  └──────────────────┘      │
│           │                      │                                     │
│           │  GetActiveAssets     │  GetSoldAssets                      │
│           │  SellAsset           │  UndoSellAsset                      │
│           ▼                      ▼                                     │
├──────────────────────────────────────────────────────────────────────┤
│                              Valt.App                                   │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  CQRS Handlers                                                 │  │
│  │  GetActiveAssetsHandler  GetSoldAssetsHandler                 │  │
│  │  SellAssetHandler        UndoSellAssetHandler                 │  │
│  │  GetAssetSummaryHandler (excludes sold)                        │  │
│  └────────────────────────────────────────────────────────────────┘  │
│           │                      │                                     │
│           │  IAssetRepository    │  IAssetQueries                      │
│           ▼                      ▼                                     │
├──────────────────────────────────────────────────────────────────────┤
│                             Valt.Infra                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ AssetEntity  │  │ AssetQueries │  │ AssetDetails │  │ Mcp Tools  │ │
│  │ (+IsSold,    │  │ (+active/    │  │ Serializer   │  │ AssetTools │ │
│  │  DateSold)   │  │  sold filters)│  │ (unchanged)  │  │ (+sold ops)│ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │
│           │                      │                                     │
│           ▼                      ▼                                     │
├──────────────────────────────────────────────────────────────────────┤
│                             LiteDB                                    │
│                        (local + price databases)                       │
└──────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | What Changes |
|-----------|---------------|--------------|
| `Asset` (Core) | Aggregate root; owns `IsSold`/`DateSold` and mutation rules | Add properties, `Sell()`, `UndoSell()` methods |
| `AssetEntity` (Infra) | LiteDB persistence shape | Add `IsSold`/`DateSold` Bson fields |
| `AssetDetailsSerializer` (Infra) | JSON serialization of `IAssetDetails` | **No change** — sold state is not stored in details JSON |
| `IAssetQueries` / `AssetQueries` (Infra) | Read models and filtering | Add `GetSoldAssetsAsync`, `GetActiveAssetsAsync`; update `GetVisibleAsync` and `GetSummaryAsync` to exclude sold |
| `SellAssetCommand` (App) | Mark asset sold with a date | New command/handler/validator |
| `UndoSellAssetCommand` (App) | Restore asset to active view | New command/handler/validator |
| `GetSoldAssetsQuery` / `GetActiveAssetsQuery` (App) | Read sold-only or active-only assets | New query/handler pairs |
| `AssetsViewModel` (UI) | Main tab logic | Switch to `GetActiveAssetsQuery`; add History button; change Sell flow from Delete to Sell command |
| `SoldAssetHistoryViewModel` (UI) | Modal listing sold assets | New modal; reuses `AssetViewModel` for per-type details panel |
| `AssetViewModel` (UI) | Display model for individual assets | Add `IsSold`, `DateSold`, `DateSoldFormatted` |
| `AssetTools` (MCP) | AI assistant tools | Add `GetSoldAssets`, `SellAsset`, `UndoSellAsset` |

## Recommended Project Structure

```
src/
├── Valt.Core/Modules/Assets/
│   ├── Asset.cs                          # Add IsSold, DateSold, Sell(), UndoSell()
│   └── Events/
│       └── AssetSoldEvent.cs             # Optional: domain event for sold/undo
├── Valt.App/Modules/Assets/
│   ├── Commands/
│   │   ├── SellAsset/
│   │   │   ├── SellAssetCommand.cs
│   │   │   ├── SellAssetHandler.cs
│   │   │   └── SellAssetValidator.cs
│   │   └── UndoSellAsset/
│   │       ├── UndoSellAssetCommand.cs
│   │       ├── UndoSellAssetHandler.cs
│   │       └── UndoSellAssetValidator.cs
│   ├── Queries/
│   │   ├── GetActiveAssets/
│   │   │   ├── GetActiveAssetsQuery.cs
│   │   │   └── GetActiveAssetsHandler.cs
│   │   ├── GetSoldAssets/
│   │   │   ├── GetSoldAssetsQuery.cs
│   │   │   └── GetSoldAssetsHandler.cs
│   │   └── GetAssetSummary/              # Update handler to exclude sold assets
│   └── DTOs/
│       └── AssetDTO.cs                   # Add IsSold, DateSold
├── Valt.Infra/Modules/Assets/
│   ├── AssetEntity.cs                    # Add IsSold, DateSold fields
│   ├── Extensions.cs                     # Map IsSold/DateSold in AsEntity/AsDomainObject
│   ├── Queries/
│   │   └── AssetQueries.cs               # Add active/sold filters
│   └── Services/
│       └── AssetPriceUpdaterJob.cs       # Skip sold assets
├── Valt.Infra/Mcp/Tools/
│   └── AssetTools.cs                     # Add sold tools, update summary
├── Valt.UI/Views/
│   ├── ApplicationModalNames.cs          # Add SoldAssetHistory
│   ├── Main/Tabs/Assets/
│   │   ├── AssetsView.axaml              # Add History toolbar button
│   │   ├── AssetsViewModel.cs            # Use GetActiveAssetsQuery, new Sell flow
│   │   └── Models/AssetViewModel.cs       # Add IsSold/DateSold display props
│   └── Main/Modals/SoldAssetHistory/
│       ├── SoldAssetHistoryView.axaml
│       ├── SoldAssetHistoryView.axaml.cs
│       └── SoldAssetHistoryViewModel.cs
└── Valt.UI/Lang/
    ├── language.resx
    ├── language.pt-BR.resx
    ├── language.es.resx
    └── language.Designer.cs
```

### Structure Rationale

- **Sold state lives on the aggregate, not in JSON details:** This lets `AssetQueries` filter by `IsSold` without deserializing `DetailsJson`, avoiding a full table scan.
- **New commands mirror existing toggle commands:** `SellAsset`/`UndoSellAsset` follow the same pattern as `SetAssetVisibility`/`SetAssetIncludeInNetWorth` — load asset, mutate, save, emit event.
- **Active/Sold queries are separate from `GetAssetsQuery`:** `GetAssetsQuery` keeps its "all tracked assets" semantics (used by MCP and the leverage simulator). The UI main view switches to `GetActiveAssetsQuery` to hide sold assets.
- **History modal reuses `AssetViewModel`:** It already contains all per-type formatting and display helpers, so the details panel can be a copy of the existing right-side panel from `AssetsView.axaml`.

## Architectural Patterns

### Pattern 1: State Flag on Aggregate Root

**What:** Add a boolean flag and an optional date to the existing aggregate; expose domain methods that mutate the flag and emit an update event.

**When to use:** When a feature only changes the "status" of an entity and does not require a new domain concept (e.g., sold vs. active).

**Trade-offs:** Simple and low-risk; avoids a separate `Sale` table or collection. Does not capture sale price or tax lots — deliberately out of scope for v0.5.

**Example:**
```csharp
public sealed class Asset : AggregateRoot<AssetId>
{
    public bool IsSold { get; private set; }
    public DateOnly? DateSold { get; private set; }

    public void Sell(DateOnly dateSold)
    {
        IsSold = true;
        DateSold = dateSold;
        AddEvent(new AssetUpdatedEvent(this));
    }

    public void UndoSell()
    {
        IsSold = false;
        DateSold = null;
        AddEvent(new AssetUpdatedEvent(this));
    }
}
```

### Pattern 2: CQRS Command Per State Transition

**What:** Each user action that changes sold state is a command with a dedicated handler, validator, and DTO.

**When to use:** This is the existing Valt.App pattern; keep using it for consistency.

**Trade-offs:** More files than a simple service method, but gives validation, DI, and MCP exposure for free.

**Example:**
```csharp
public record SellAssetCommand : ICommand
{
    public required string AssetId { get; init; }
    public required DateOnly DateSold { get; init; }
}

internal sealed class SellAssetHandler : ICommandHandler<SellAssetCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(SellAssetCommand command, CancellationToken ct)
    {
        var asset = await _assetRepository.GetByIdAsync(new AssetId(command.AssetId));
        if (asset is null) return Result<Unit>.NotFound("Asset", command.AssetId);
        if (asset.IsSold) return Result<Unit>.Failure("ALREADY_SOLD", "Asset is already sold");

        asset.Sell(command.DateSold);
        await _assetRepository.SaveAsync(asset);
        return Result<Unit>.Success(Unit.Value);
    }
}
```

### Pattern 3: Query-Driven Active vs. Sold Lists

**What:** The database returns the same `AssetDTO` shape for both active and sold assets; filtering happens at the query layer, not in the UI.

**When to use:** When the same display model is used in two contexts but the underlying filter differs.

**Trade-offs:** Keeps UI ViewModels simple; makes testing easy; ensures MCP and reports use the same filter logic.

**Example:**
```csharp
public interface IAssetQueries
{
    Task<IReadOnlyList<AssetDTO>> GetActiveAssetsAsync(); // !IsSold
    Task<IReadOnlyList<AssetDTO>> GetSoldAssetsAsync();   // IsSold
    Task<IReadOnlyList<AssetDTO>> GetVisibleAsync();      // !IsSold && Visible
    Task<AssetSummaryDTO> GetSummaryAsync(...);           // !IsSold && IncludeInNetWorth
}
```

## Data Flow

### Sell Flow (UI)

```
User right-clicks asset → Sell
    ↓
AssetsViewModel.SellAssetCommand(asset)
    ↓
TransactionEditor modal (pre-filled with sale value, date defaults to today)
    ↓
If user saves transaction:
    ↓
SellAssetCommand(AssetId, transactionDate)
    ↓
SellAssetHandler → Asset.Sell(date) → AssetRepository.SaveAsync(asset)
    ↓
LoadAssetsAsync() using GetActiveAssetsQuery
    ↓
NotifyAssetSummaryUpdated() → refresh reports/totals
```

### Undo Sell Flow (History Screen)

```
User clicks History button on Assets toolbar
    ↓
SoldAssetHistoryViewModel loads via GetSoldAssetsQuery
    ↓
User selects sold asset → details panel renders
    ↓
User clicks Undo Sell
    ↓
UndoSellAssetCommand(AssetId)
    ↓
UndoSellAssetHandler → Asset.UndoSell() → AssetRepository.SaveAsync(asset)
    ↓
Modal closes, AssetsView refreshes active list, summary updates
```

### Query Filtering

```
GetActiveAssetsAsync:
  _localDatabase.GetAssets().Find(x => !x.IsSold)

GetSoldAssetsAsync:
  _localDatabase.GetAssets().Find(x => x.IsSold)
    .OrderByDescending(x => x.DateSold)

GetSummaryAsync:
  _localDatabase.GetAssets().Find(x => x.IncludeInNetWorth && !x.IsSold)
```

### Key Data Flows

1. **Sold assets are never deleted:** They remain in the database with `IsSold=true` and `DateSold` set, so they can be restored and historically referenced.
2. **Totals and reports exclude sold assets:** `AssetQueries.GetSummaryAsync`, `GetVisibleAsync`, and the new `GetActiveAssetsAsync` all filter `!IsSold`.
3. **Price updater skips sold assets:** The `AssetPriceUpdaterJob` should not waste API calls on assets that are no longer held.
4. **MCP sees all assets but can also query sold-only:** `AssetTools.GetAssets` continues to return all tracked assets (including sold, with the new flag); `GetSoldAssets` returns the history list.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 0–10k assets | Monolithic LiteDB approach is fine. Sold/active filtering is in-memory after a single collection read. |
| 10k+ assets | Add a LiteDB index on `IsSold` and `DateSold` if query latency becomes noticeable. Consider splitting sold assets to a separate collection only if the history list grows very large. |
| 100k+ assets | LiteDB is no longer the right tool; this would require a persistence migration, which is explicitly out of scope. |

### Scaling Priorities

1. **First bottleneck:** `AssetQueries.GetSummaryAsync` reads all assets and deserializes details. Excluding sold assets reduces the set but does not change the algorithm. Keep an eye on this if the user accumulates hundreds of sold assets.
2. **Second bottleneck:** `AssetPriceUpdaterJob` will skip sold assets, so the API call count drops automatically over time.

## Anti-Patterns

### Anti-Pattern 1: Store Sold State Inside DetailsJson

**What people do:** Add `IsSold` to `BasicAssetDetailsDto` or another details DTO.

**Why it's wrong:** Every query would need to deserialize the JSON details just to know whether an asset is sold. This breaks the aggregate boundary and makes filtering expensive.

**Do this instead:** Put `IsSold` and `DateSold` on `AssetEntity` and `Asset` directly, alongside `Visible` and `IncludeInNetWorth`.

### Anti-Pattern 2: Reuse the `Visible` Flag for "Sold"

**What people do:** Set `Visible=false` on sell and try to remember that it means sold.

**Why it's wrong:** It conflates two concepts. A user might want a sold asset to remain visible in a history screen, and an active asset might be hidden via `Visible`. It also makes undo brittle because the previous visibility state is lost.

**Do this instead:** Keep `IsSold` independent. The main view filters `!IsSold && Visible` (or just `!IsSold` for the full active list), and the history view filters `IsSold`.

### Anti-Pattern 3: Delete the Asset on Sell

**What people do:** Keep the existing `SellAssetCommand` flow that calls `DeleteAssetCommand` after the transaction is saved.

**Why it's wrong:** The asset is gone, so there is no history to display and no undo. This directly contradicts the v0.5 goal.

**Do this instead:** Replace the delete step with `SellAssetCommand`. Existing transactions created by prior sales will still reference the now-kept asset; that is acceptable because the asset is merely marked as sold.

### Anti-Pattern 4: Change `GetAssetsQuery` to Exclude Sold Everywhere

**What people do:** Modify `GetAssetsQuery` to return only active assets, breaking MCP and the leverage simulator.

**Why it's wrong:** `GetAssetsQuery` has "all tracked assets" semantics and is used by MCP and other tools. Changing it silently changes API behavior.

**Do this instead:** Add `GetActiveAssetsQuery` for the main UI and leave `GetAssetsQuery` returning all assets. MCP can keep `GetAssets` and add `GetSoldAssets`.

## Integration Points

### UI Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `AssetsViewModel` ↔ `SoldAssetHistoryViewModel` | Modal launcher (`IModalFactory`) | History modal opens as a dialog; Undo Sell triggers a command and closes the modal. |
| `AssetsViewModel` ↔ `TransactionEditor` | Modal launcher | Sell flow still pre-fills the transaction editor; the saved transaction date becomes `DateSold`. |
| `AssetsViewModel` ↔ `AssetSummaryUpdatedMessage` | `WeakReferenceMessenger` | Sell/Undo must publish `AssetSummaryUpdatedMessage` to refresh reports and totals. |

### Back-End Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `AssetQueries` ↔ `AssetEntity` | LiteDB collection query | New `IsSold`/`DateSold` fields are queryable at the entity level. |
| `SellAssetHandler` ↔ `IAssetRepository` | Repository save | No new repository methods needed; reuse `SaveAsync`. |
| `AssetPriceUpdaterJob` ↔ `AssetEntity` | Background job read | Add `!IsSold` to the update filter. |

### External Services

No new external services. The price updater skips sold assets, so API usage decreases slightly.

## Build Order

Recommended order, with dependencies shown:

1. **Domain + Persistence** (no UI blockers)
   - `Asset.cs` — add `IsSold`, `DateSold`, `Sell()`, `UndoSell()`
   - `AssetEntity.cs` — add `IsSold`, `DateSold` fields
   - `Extensions.cs` — map new fields in `AsEntity`/`AsDomainObject`
   - Update `AssetBuilder` with `WithSold()` / `WithDateSold()`

2. **CQRS Commands** (depends on domain)
   - `SellAssetCommand` / `Handler` / `Validator`
   - `UndoSellAssetCommand` / `Handler` / `Validator`
   - Unit tests

3. **CQRS Queries + Infra Filtering** (depends on persistence)
   - Add `GetActiveAssetsAsync` and `GetSoldAssetsAsync` to `IAssetQueries`
   - Implement filters in `AssetQueries`
   - Add `GetActiveAssetsQuery` / `Handler` and `GetSoldAssetsQuery` / `Handler`
   - Update `GetVisibleAsync` to exclude sold
   - Update `GetSummaryAsync` to exclude sold
   - Update `AssetPriceUpdaterJob.ShouldUpdatePrice` to skip sold
   - Unit tests

4. **AssetDTO + ViewModel Display** (depends on queries)
   - Add `IsSold`/`DateSold` to `AssetDTO`
   - Add display properties to `AssetViewModel`

5. **UI Main View Changes** (depends on commands/queries)
   - `AssetsViewModel` switch to `GetActiveAssetsQuery`
   - Change `SellAssetCommand` from Delete to `SellAssetCommand`
   - Add History button and `OpenSoldAssetHistoryCommand`
   - Update `AssetsView.axaml` toolbar
   - Add localization strings

6. **Sold Asset History Modal** (depends on AssetViewModel and queries)
   - `SoldAssetHistoryViewModel` + `View`
   - `ApplicationModalNames.SoldAssetHistory`
   - Register view in `Valt.UI/Extensions.cs`
   - Add localization strings

7. **MCP Tools** (depends on CQRS)
   - `SellAsset` / `UndoSellAsset` / `GetSoldAssets` in `AssetTools`
   - Update tool descriptions to mention sold state

8. **Documentation + End-to-End Verification**
   - Update `.claude/docs/assets.md`
   - Verify sell, undo, query filtering, totals, and price updater behavior

## Files New vs. Modified

### New Files

| File | Purpose |
|------|---------|
| `Valt.App/Modules/Assets/Commands/SellAsset/*` | Mark asset sold |
| `Valt.App/Modules/Assets/Commands/UndoSellAsset/*` | Restore sold asset |
| `Valt.App/Modules/Assets/Queries/GetActiveAssets/*` | Active assets for main view |
| `Valt.App/Modules/Assets/Queries/GetSoldAssets/*` | Sold assets for history |
| `Valt.UI/Views/Main/Modals/SoldAssetHistory/*` | History modal UI |
| `Valt.Core/Modules/Assets/Events/AssetSoldEvent.cs` (optional) | Explicit domain event if desired |

### Modified Files

| File | Change |
|------|--------|
| `Valt.Core/Modules/Assets/Asset.cs` | Add `IsSold`, `DateSold`, `Sell()`, `UndoSell()` |
| `Valt.Infra/Modules/Assets/AssetEntity.cs` | Add `IsSold`, `DateSold` fields |
| `Valt.Infra/Modules/Assets/Extensions.cs` | Map new fields |
| `Valt.Infra/Modules/Assets/Queries/AssetQueries.cs` | Add filters, update summary |
| `Valt.App/Modules/Assets/Contracts/IAssetQueries.cs` | Add new query methods |
| `Valt.App/Modules/Assets/DTOs/AssetDTO.cs` | Add `IsSold`, `DateSold` |
| `Valt.App/Modules/Assets/Queries/GetAssetSummary/GetAssetSummaryHandler.cs` | Use updated summary query |
| `Valt.App/Modules/Assets/Queries/GetVisibleAssets/GetVisibleAssetsHandler.cs` | Exclude sold |
| `Valt.Infra/Modules/Assets/Services/AssetPriceUpdaterJob.cs` | Skip sold assets |
| `Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` | Use active query, new Sell flow, History command |
| `Valt.UI/Views/Main/Tabs/Assets/AssetsView.axaml` | Add History button |
| `Valt.UI/Views/Main/Tabs/Assets/Models/AssetViewModel.cs` | Add display properties |
| `Valt.UI/Views/ApplicationModalNames.cs` | Add `SoldAssetHistory` |
| `Valt.UI/Extensions.cs` | Register new modal view |
| `Valt.UI/Lang/language.*` | Add strings |
| `Valt.Infra/Mcp/Tools/AssetTools.cs` | Add sold tools |
| `tests/Valt.Tests/Builders/AssetBuilder.cs` | Add `WithSold` / `WithDateSold` |
| `.claude/docs/assets.md` | Document behavior |

## Backward Compatibility Notes

- **No migration required.** Existing `AssetEntity` records in LiteDB will have `IsSold` default to `false` and `DateSold` default to `null` when read. `Extensions.AsDomainObject` should use `entity.IsSold` and parse `entity.DateSold` as nullable, defaulting to `false`/`null` if the fields are missing.
- **Sold assets from prior versions do not exist.** Before v0.5, selling deleted the asset. Those records are gone, so the new History screen will start empty for existing users.
- **Existing transactions from prior sales remain.** They are ordinary transactions not tied to an asset, so they do not break.

## Sources

- `Valt.Core/Modules/Assets/Asset.cs` — aggregate structure and mutation patterns
- `Valt.Infra/Modules/Assets/AssetEntity.cs` and `Extensions.cs` — persistence mapping
- `Valt.Infra/Modules/Assets/Queries/AssetQueries.cs` — query filtering and summary logic
- `Valt.App/Modules/Assets/Commands/SetAssetVisibility/` — template for simple toggle commands
- `Valt.App/Modules/Assets/Commands/AddLoanStateUpdate/` — template for date-bearing mutation commands
- `Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` and `AssetsView.axaml` — existing sell flow and UI layout
- `Valt.UI/Views/Main/Modals/LoanStateHistory/LoanStateHistoryViewModel.cs` and `.axaml` — modal history pattern
- `Valt.Infra/Mcp/Tools/AssetTools.cs` — existing MCP tool patterns
- `.planning/PROJECT.md` — v0.5 scope and constraints

---
*Architecture research for: Asset Sold History feature integration into Valt v0.5*
*Researched: 2026-07-13*
