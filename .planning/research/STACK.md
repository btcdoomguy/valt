# Stack Research: Asset Sold History Feature

**Project:** Valt  
**Domain:** Personal budget / asset tracking desktop application  
**Researched:** 2026-07-13  
**Confidence:** HIGH  

## Recommended Stack

No new packages or frameworks are required for the Asset Sold History feature. The implementation is a vertical slice inside the existing .NET 10 / Avalonia 12 / LiteDB 5 / CQRS stack, reusing the controls and patterns already in the codebase.

### Core Technologies (unchanged)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET SDK | `net10.0` | Runtime and base class library | Already targeted by all projects; no runtime change needed. |
| Avalonia | `12.0.3` | Cross-platform desktop UI framework | Existing main window, tabs, and modals are built on Avalonia; the new History screen follows the same `Window` + `CustomTitleBar` pattern. |
| Avalonia.Controls.DataGrid | `12.0.0` | List/grid control for the History screen | Already referenced and used by `LoanStateHistoryView`; use the same `DataGrid` with `SelectedItem` binding for the sold-asset list. |
| Avalonia.Themes.Fluent | `12.0.3` | Built-in theme | Existing styling resources (cards, separators, brushes) should be reused for the History details panel. |
| CommunityToolkit.Mvvm | `8.4.2` | MVVM, source-generated `RelayCommand`/`ObservableProperty` | Used by every existing ViewModel; no new ViewModel base needed. |
| LiteDB | `5.0.21` | Embedded document store | `AssetEntity` is schema-less; adding `IsSold` and `DateSold` is backward-compatible with existing user databases. |
| ModelContextProtocol.AspNetCore | `1.3.0` | MCP server tool attributes | Existing `AssetTools.cs` pattern; add new `[McpServerTool]` methods without changing infrastructure. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| *None required* | — | — | All list, details, date-picker, and command patterns are covered by the core stack above. |

### Development Tools (unchanged)

| Tool | Purpose | Notes |
|------|---------|-------|
| NUnit / NSubstitute | Unit and integration tests | Extend existing `DatabaseTest` / `IntegrationTest` bases and `AssetBuilder`. |
| `dotnet build` / `dotnet test` | Build and verify | Standard commands from `CLAUDE.md` remain sufficient. |

## Installation

No new package installations are needed. The only package references that will be exercised are already in the project:

```xml
<!-- src/Valt.UI/Valt.UI.csproj -->
<PackageReference Include="Avalonia" />
<PackageReference Include="Avalonia.Controls.DataGrid" />
<PackageReference Include="CommunityToolkit.Mvvm" />

<!-- src/Valt.Infra/Valt.Infra.csproj -->
<PackageReference Include="LiteDB" />
<PackageReference Include="ModelContextProtocol.AspNetCore" />
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Reuse existing `DataGrid` + `ContentControl` details panel | Add a third-party data grid (e.g., `Avalonia.Controls.DataGrid` already covers this) | Not applicable; the built-in grid is sufficient. |
| Reuse existing `AssetUpdatedEvent` domain event | Introduce a new `AssetSoldEvent` / `AssetSaleUndoneEvent` | Only if downstream consumers need to react specifically to a sale; for now the generic update event plus messenger refresh is simpler. |
| Add optional fields to existing `AssetEntity` | Create a separate `SoldAssets` collection | Rejected: a separate collection would duplicate mapping logic and complicate the Undo action; the existing document is the natural owner of `IsSold`/`DateSold`. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Entity Framework Core or any new ORM | LiteDB is already the persistence store; switching would require a migration and redesign. | Add optional `BsonField` properties to `AssetEntity`. |
| FluentAvalonia / custom control suites | Adds theming and dependency risk for a single list+details screen. | Built-in `DataGrid`, `ContentControl`, `Button`, and `CalendarDatePicker`. |
| Sale-price / capital-gains / tax-lot libraries | Explicitly out of scope for v0.5. | Record only `DateSold`; defer price/gain tracking to a later milestone. |
| Real-time price streaming or automated sell detection | Manual sell action only per requirements. | Existing `SellAsset` command flow + a date prompt. |

## Stack Patterns by Variant

**If the user wants a compact History screen:**
- Use a `DataGrid` for the list and a `ContentControl` on the right for the details summary.
- Because the existing `AssetsView` already renders per-type details, extract (or copy) a `DataTemplate` per `AssetViewModel` type into the new view.

**If the user wants the same card-style look as the main Assets tab:**
- Reuse the `ItemsControl`/`WrapPanel` group container and `AssetViewModel` card template from `AssetsView.axaml` inside the History modal.
- Add the `DateSold` row to the card template.

**If a Date Sold prompt is needed:**
- Create a small modal window (same pattern as `UpdateLoanStateView`) containing a `CalendarDatePicker`.
- Default to `DateTime.Now`; return `DateOnly` to the caller.

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| `Avalonia` `12.0.3` | `Avalonia.Controls.DataGrid` `12.0.0` | Major versions are aligned; the existing project already uses this combination. |
| `CommunityToolkit.Mvvm` `8.4.2` | `Avalonia` `12.0.3` | Source generators are independent of Avalonia; no binding issue. |
| `LiteDB` `5.0.21` | `net10.0` | LiteDB 5.x works on modern .NET; adding nullable fields is safe. |

## Integration Points with the Existing Stack

### Domain / Entity Layer
- Add `IsSold` and `DateSold` to the `Asset` aggregate root and to `AssetEntity`.
- Map `DateSold` as `DateTime?` in `AssetEntity` and `DateOnly?` in the domain / DTOs (consistent with existing `LoanStartDate` handling).
- Add `MarkAsSold(DateOnly dateSold)` and `UndoSale()` methods on `Asset` that emit `AssetUpdatedEvent`.

### LiteDB Queries
- Add `IsSold` and `DateSold` to `AssetEntity` and to the `AsEntity` / `AsDomainObject` mapping.
- In `AssetQueries`:
  - Add `GetSoldAsync()` returning `x => x.IsSold`, ordered by `DateSold` descending.
  - Update `GetSummaryAsync` to exclude sold assets (`!x.IsSold`) from totals.
  - Keep `GetByIdAsync` unchanged so a sold asset can still be loaded by ID.

### CQRS / Application Layer
- Add `MarkAssetAsSoldCommand` and `UndoAssetSaleCommand` (or a single `SetAssetSoldStatusCommand`) under `Valt.App/Modules/Assets/Commands/`.
- Add a validator requiring `AssetId` and, for sold, a non-default `DateSold`.
- Add handler following the existing `SetAssetVisibilityHandler` / `RepayLoanHandler` pattern: load via `IAssetRepository`, mutate, save, return `Result<Unit>`.
- Add `GetSoldAssetsQuery` / handler that calls `IAssetQueries.GetSoldAsync()`.

### UI / Avalonia
- Add an `AssetSoldHistory` value to `ApplicationModalNames` and register the view in `Extensions.cs` (same pattern as `LoanStateHistory`).
- Create `AssetSoldHistoryView.axaml` and `AssetSoldHistoryViewModel`.
- Add a History button to the Assets toolbar in `AssetsView.axaml`.
- Replace the current `SellAssetCommand` delete-with-transaction behavior with mark-as-sold; still open `TransactionEditor` to record the sale, then call `MarkAssetAsSoldCommand` instead of `DeleteAssetCommand`.
- For the optional date prompt, use `CalendarDatePicker` in a small modal or inline in the sell flow.

### MCP Tools
- In `AssetTools.cs`, add `[McpServerTool]` methods such as `MarkAssetAsSold`, `UndoAssetSale`, and optionally `GetSoldAssets`.
- Follow the existing tool pattern: validate via dispatcher, publish `McpDataChangedNotification` on success.

### Localization
- Add new strings to `src/Valt.UI/Lang/language.resx`, `language.pt-BR.resx`, and `language.es.resx`, then regenerate `language.Designer.cs`.
- Example keys: `Assets_History`, `AssetHistory_Title`, `AssetHistory_DateSold`, `AssetHistory_UndoSell`, `AssetSoldConfirmation_Title`, `AssetSoldConfirmation_Message`.

### Testing
- Extend `AssetBuilder` with `WithSold(bool)` and `WithDateSold(DateOnly?)`.
- Add domain tests for `Asset.MarkAsSold` / `UndoSale`.
- Add query tests asserting sold assets are excluded from `GetAssetSummary`.
- Add MCP integration tests mirroring `AssetToolsLoanStateTests`.

## Sources

- `Directory.Packages.props` — authoritative package versions.
- `src/Valt.UI/Valt.UI.csproj`, `src/Valt.Infra/Valt.Infra.csproj`, `src/Valt.App/Valt.App.csproj` — existing package references and project boundaries.
- Existing Assets module source files (`Asset.cs`, `AssetEntity.cs`, `AssetQueries.cs`, `Extensions.cs`, `AssetTools.cs`, `AssetsView.axaml`, `AssetsViewModel.cs`, `LoanStateHistoryView.axaml`) — integration points and patterns to reuse.
- Avalonia documentation for `DataGrid`, `ContentControl`, and `CalendarDatePicker` (built-in controls, no new package required).

---
*Stack research for: Asset Sold History feature in Valt v0.5*
