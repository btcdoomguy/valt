# Phase 18: Manage Asset Builder - Research

**Researched:** 2026-06-23
**Domain:** C# / Avalonia UI / Valt application layer refactor
**Confidence:** MEDIUM

## Summary

Phase 18 extracts per-type asset command and DTO construction logic from `ManageAssetViewModel` into a dedicated `IAssetFormBuilder` / `AssetFormBuilder` service. The ViewModel currently contains ~470 lines of create/edit logic across 9 asset types (Stock, ETF, Crypto, Commodity, Custom, RealEstate, LeveragedPosition, BtcLoan, BtcLending). The refactor follows the same builder-extraction pattern established in Phase 17 (`ITransactionDetailsBuilder` / `TransactionDetailsBuilder`) [CITED: .planning/phases/17-transaction-editor-builder/17-RESEARCH.md].

The service must live in `Valt.UI` because it deals with presentation form state (selected price source strings, `FiatValue` form fields, `DateTime?` date pickers) and because `Valt.App` must not reference `Valt.UI` or Avalonia types per existing NetArchTest rules [CITED: tests/Valt.Tests/Architecture/LayerDependencyTests.cs]. The builder will construct `Create*Command` instances for creation, `AssetDetailsInputDTO` subtypes for editing, and load form values from `AssetDTO` for the edit modal. Price fetching for basic assets, leveraged positions, and BTC loans moves into the builder so the ViewModel delegates the async construction entirely.

**Primary recommendation:** Create `IAssetFormBuilder` / `AssetFormBuilder` in `src/Valt.UI/Services/`, register it as a singleton in `Valt.UI.Extensions.cs`, inject it into `ManageAssetViewModel`, and add unit tests under `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` covering all 5 create/edit command/DTO paths with focused coverage on BTC loan, leveraged position, and real estate.

<user_constraints>
## User Constraints (from CONTEXT.md)

No CONTEXT.md exists for this phase. Constraints are derived from the phase description, requirement VM-SVC-02, the milestone roadmap, and the precedent set by Phase 17 in STATE.md.

### Locked Decisions (implicit)
- Builder must follow the Phase 17 extraction pattern.
- Builder must support all 9 asset types.
- Unit tests must cover at least BTC loan, leveraged position, and real estate.
- `ManageAssetViewModel` must remain functionally identical after the refactor.

### the agent's Discretion
- Exact interface shape (single method vs per-type methods vs envelope record).
- Snapshot/values record property granularity.
- Test organization and helper methods.

### Deferred Ideas (OUT OF SCOPE)
- Splitting `ManageAssetViewModel` into per-asset-type child VMs (Phase 22, VM-CHILD-02).
- Major UI redesign or new user-facing features.
- Moving asset price providers or command handlers to different layers.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| VM-SVC-02 | `ManageAssetViewModel` delegates per-type asset command/DTO construction to a dedicated builder service | Location (`Valt.UI/Services/`), interface shape, file changes, and test strategy identified below |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Form state → create command mapping | UI | App | Driven by UI-bound form fields (`SelectedAssetType`, `CurrentPriceFiat`, `CollateralFiat`, etc.) and includes price-provider calls that are presentation concerns [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs] |
| Form state → edit details DTO mapping | UI | App | Produces polymorphic `AssetDetailsInputDTO` consumed by `EditAssetCommand` in the App layer [CITED: src/Valt.App/Modules/Assets/Commands/EditAsset/EditAssetCommand.cs] |
| AssetDTO → form state mapping | UI | App | Loading an asset for edit populates UI-bound observable properties from a read DTO [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs] |
| Asset command execution | App | UI | `Create*AssetCommand`, `EditAssetCommand`, and their handlers live in `Valt.App` [CITED: src/Valt.App/Modules/Assets/Commands] |
| Validation | UI / App | — | ViewModel uses `[Required]`/`[NotifyDataErrorInfo]`; command validators live in `Valt.App` [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs, src/Valt.App/Modules/Assets/Commands] |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 10.0.100 | Build/runtime | Project target framework [CITED: src/Valt.UI/Valt.UI.csproj] |
| Avalonia | 11.x | UI framework | Already in use; no UI changes required |
| CommunityToolkit.Mvvm | — | MVVM/source generators | Existing ViewModel base classes |
| NUnit | — | Unit testing | Existing test framework [CITED: tests/Valt.Tests/Valt.Tests.csproj] |
| NSubstitute | — | Mocking | Existing mocking framework |

### Supporting
No new supporting libraries are required.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `Valt.UI` service location | `Valt.App` service location | App cannot reference UI/Avalonia; would require moving form-state records and treating `FiatValue`/date-picker state as App concepts. UI location preserves the App-layer dependency boundary. |
| Envelope record for create commands | Per-type builder methods | Envelope keeps VM switch small and mirrors Phase 17's single `BuildDto` method; per-type methods are more explicit but leave more switch logic in the VM. |
| Builder dispatches commands itself | Builder only constructs commands | Dispatching inside the builder blurs the CQRS boundary. Construction-only keeps the ViewModel responsible for orchestration and error messaging. |

**Installation:** Not applicable — no new packages.

**Version verification:** Not applicable — no new packages.

## Package Legitimacy Audit

Not applicable — this phase installs no external packages.

## Architecture Patterns

### System Architecture Diagram

```
User input in ManageAssetView.axaml
            │
            ▼
ManageAssetViewModel (form state: Name, SelectedAssetType, SelectedCurrency,
                      CurrentPriceFiat, CollateralFiat, LoanAmountFiat, ...)
            │
            │ calls BuildCreateCommandAsync(snapshot)
            │ calls BuildEditDetailsAsync(snapshot)
            ▼
        IAssetFormBuilder
            │
            │ returns Create*Command or AssetDetailsInputDTO
            ▼
    Create*AssetCommand / EditAssetCommand
            │
            ▼
        Command handler

Existing asset loaded for editing
            │
            ▼
        GetAssetQuery
            │
            ▼
    ManageAssetViewModel receives AssetDTO
            │
            │ calls LoadFromDto(dto)
            ▼
        IAssetFormBuilder
            │
            │ returns AssetFormValues record
            ▼
ManageAssetViewModel copies values to [ObservableProperty] fields
```

### Recommended Project Structure

```
src/Valt.UI/
├── Services/
│   ├── IAssetFormBuilder.cs          # interface + AssetFormSnapshot + AssetFormValues
│   ├── AssetFormBuilder.cs           # implementation
│   └── Exceptions/
│       └── AssetFormBuildException.cs # unhandled asset type / invalid form state
└── Views/Main/Modals/ManageAsset/
    └── ManageAssetViewModel.cs        # delegates to IAssetFormBuilder

tests/Valt.Tests/
└── UI/Services/
    └── AssetFormBuilderTests.cs       # unit tests for all 5 create/edit paths
```

### Pattern 1: UI Service with Interface/Implementation Pair
**What:** Define an interface and a concrete class in `Valt.UI/Services/`, register the interface in `Valt.UI.Extensions.cs`, and inject it into the ViewModel.
**When to use:** For presentation logic that is complex enough to test independently or that needs to be shared across ViewModels.
**Example:**
```csharp
// Source: src/Valt.UI/Services/ITransactionDetailsBuilder.cs [CITED]
public interface ITransactionDetailsBuilder
{
    TransactionDetailsDto BuildDto(TransactionFormSnapshot snapshot);
    TransactionFormValues LoadFromDto(TransactionDetailsDto dto, IReadOnlyList<AccountDTO> availableAccounts);
}
```

### Pattern 2: Immutable Form Snapshot / Values Records
**What:** The builder receives an immutable record carrying all form state and returns either a command/DTO or another immutable record carrying values to apply.
**When to use:** When the service lives in the same layer but should remain testable and free of ViewModel mutation side effects.
**Example (proposed for this phase):**
```csharp
// Proposed for this phase
public sealed record AssetFormSnapshot(
    string Name,
    string SelectedAssetType,
    string SelectedCurrency,
    bool IncludeInNetWorth,
    bool Visible,
    string Symbol,
    decimal Quantity,
    FiatValue CurrentPriceFiat,
    string SelectedPriceSource,
    string Address,
    FiatValue CurrentValueFiat,
    FiatValue MonthlyRentalIncomeFiat,
    DateTime? AcquisitionDate,
    FiatValue AcquisitionPriceFiat,
    bool IsBitcoinUnderlyingAsset,
    FiatValue CollateralFiat,
    FiatValue EntryPriceFiat,
    decimal Leverage,
    FiatValue LiquidationPriceFiat,
    bool IsLong,
    bool UseExactPosition,
    decimal PositionSize,
    string PlatformName,
    long CollateralSats,
    FiatValue LoanAmountFiat,
    decimal AprPercentage,
    decimal InitialLtvPercentage,
    decimal LiquidationLtvPercentage,
    decimal MarginCallLtvPercentage,
    FiatValue FeesFiat,
    DateTime? LoanStartDate,
    DateTime? RepaymentDateOffset,
    bool IsIndefiniteLoan,
    bool UseFixedTotalDebt,
    FiatValue FixedTotalDebtFiat,
    string BorrowerOrPlatformName,
    FiatValue AmountLentFiat,
    decimal LendingAprPercentage,
    DateTime? LendingStartDateOffset,
    DateTime? ExpectedRepaymentDateOffset,
    bool IsIndefiniteLending);

public sealed record AssetFormValues(
    string Name,
    string SelectedAssetType,
    string SelectedCurrency,
    bool IncludeInNetWorth,
    bool Visible,
    string Symbol,
    decimal Quantity,
    FiatValue CurrentPriceFiat,
    string SelectedPriceSource,
    string Address,
    FiatValue CurrentValueFiat,
    FiatValue MonthlyRentalIncomeFiat,
    DateTime? AcquisitionDate,
    FiatValue AcquisitionPriceFiat,
    bool IsBitcoinUnderlyingAsset,
    FiatValue CollateralFiat,
    FiatValue EntryPriceFiat,
    decimal Leverage,
    FiatValue LiquidationPriceFiat,
    bool IsLong,
    bool UseExactPosition,
    decimal PositionSize,
    string PlatformName,
    long CollateralSats,
    FiatValue LoanAmountFiat,
    decimal AprPercentage,
    decimal InitialLtvPercentage,
    decimal LiquidationLtvPercentage,
    decimal MarginCallLtvPercentage,
    FiatValue FeesFiat,
    DateTime? LoanStartDate,
    DateTime? RepaymentDateOffset,
    bool IsIndefiniteLoan,
    bool UseFixedTotalDebt,
    FiatValue FixedTotalDebtFiat,
    string BorrowerOrPlatformName,
    FiatValue AmountLentFiat,
    decimal LendingAprPercentage,
    DateTime? LendingStartDateOffset,
    DateTime? ExpectedRepaymentDateOffset,
    bool IsIndefiniteLending);
```

### Pattern 3: Envelope Record for Polymorphic Create Commands
**What:** Because the five create commands do not share a common base interface, return a UI-layer discriminated envelope from `BuildCreateCommandAsync` so the ViewModel can switch on a single record type.
**When to use:** When a service must return one of several concrete command/DTO types and C# does not provide a natural shared abstraction.
**Example (proposed for this phase):**
```csharp
// Proposed for this phase
public abstract record CreateAssetCommandEnvelope;
public sealed record BasicAssetCommandEnvelope(CreateBasicAssetCommand Command) : CreateAssetCommandEnvelope;
public sealed record RealEstateAssetCommandEnvelope(CreateRealEstateAssetCommand Command) : CreateAssetCommandEnvelope;
public sealed record LeveragedPositionCommandEnvelope(CreateLeveragedPositionCommand Command) : CreateAssetCommandEnvelope;
public sealed record BtcLoanCommandEnvelope(CreateBtcLoanCommand Command) : CreateAssetCommandEnvelope;
public sealed record BtcLendingCommandEnvelope(CreateBtcLendingCommand Command) : CreateAssetCommandEnvelope;
```

### Anti-Patterns to Avoid
- **Mutating the ViewModel from the builder:** The builder should not take `ManageAssetViewModel` as a parameter; that defeats testability and still couples the logic to the VM.
- **Moving the builder to `Valt.App` while referencing UI types:** `Valt.App` must not reference `Valt.UI` or Avalonia; NetArchTest enforces this [CITED: tests/Valt.Tests/Architecture/LayerDependencyTests.cs].
- **Duplicating price-fetching logic in the VM:** After extraction, the VM should not re-fetch prices before calling the builder; the builder owns price resolution for non-manual sources.
- **Losing the "set SelectedAssetType last" ordering:** `OnBindParameterAsync` sets `SelectedAssetType` only after all type-specific fields are populated so date pickers bind correctly. The builder's `LoadFromDto` can return the type, but the VM must apply it last.

## Domain Summary

The Assets module tracks 9 asset types grouped into 5 create/edit command/DTO families:

| Asset Type | Family | Create Command | Edit DTO |
|------------|--------|----------------|----------|
| Stock, ETF, Crypto, Commodity, Custom | Basic | `CreateBasicAssetCommand` | `BasicAssetDetailsInputDTO` |
| RealEstate | Real estate | `CreateRealEstateAssetCommand` | `RealEstateAssetDetailsInputDTO` |
| LeveragedPosition | Leveraged | `CreateLeveragedPositionCommand` | `LeveragedPositionDetailsInputDTO` |
| BtcLoan | BTC loan | `CreateBtcLoanCommand` | `BtcLoanDetailsInputDTO` |
| BtcLending | BTC lending | `CreateBtcLendingCommand` | `BtcLendingDetailsInputDTO` |

Key domain rules that affect construction:
- Basic assets and real estate support optional `AcquisitionDate` and `AcquisitionPrice` [CITED: src/Valt.Core/Modules/Assets/Details/BasicAssetDetails.cs].
- Leveraged positions support two input modes: `Collateral` (default) and `ExactPosition`; when exact, collateral is derived from `PositionSize * EntryPrice / Leverage` [CITED: src/Valt.App/Modules/Assets/Commands/EditAsset/EditAssetHandler.cs].
- BTC loans support fixed total debt: when `UseFixedTotalDebt` is true, `Apr` is set to 0 on creation and derived in the edit handler from `LoanAmount`, `FixedTotalDebt`, `LoanStartDate`, and `RepaymentDate` [CITED: src/Valt.App/Modules/Assets/Commands/EditAsset/EditAssetHandler.cs].
- BTC loans fetch the current BTC price during both create and edit for initial LTV calculation [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs].
- BTC lending mirrors BTC loan structure but without collateral/LTV fields.

## Current State

`ManageAssetViewModel` currently owns all construction logic in three places:

1. **`OnBindParameterAsync`** (~80 lines): Loads an `AssetDTO` into observable properties, switching on `AssetTypes`.
2. **`CreateNewAssetAsync`** (~200 lines): Builds and dispatches one of five `Create*AssetCommand` types, including async price fetching for basic, leveraged, and BTC loan assets.
3. **`EditExistingAssetAsync`** (~180 lines): Builds an `AssetDetailsInputDTO` subtype and dispatches a single `EditAssetCommand`, again including async price fetching.

The ViewModel references price-provider selection (`IAssetPriceProviderSelector`), currency settings, and configuration manager, but only the price provider is needed for construction.

### Current Create Logic (to be extracted)
```csharp
// Source: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs [CITED]
private async Task CreateNewAssetAsync(AssetTypes assetType)
{
    switch (assetType)
    {
        case AssetTypes.Stock:
        case AssetTypes.Etf:
        case AssetTypes.Crypto:
        case AssetTypes.Commodity:
        case AssetTypes.Custom:
            var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
            var currentPrice = CurrentPriceFiat.Value;
            if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
            {
                var priceResult = await _priceProviderSelector!.GetPriceAsync(priceSource, Symbol, SelectedCurrency);
                if (priceResult is not null) currentPrice = priceResult.Price;
            }
            var basicResult = await _commandDispatcher!.DispatchAsync(new CreateBasicAssetCommand { ... });
            // ...
            break;
        // ... RealEstate, LeveragedPosition, BtcLoan, BtcLending
    }
}
```

### Current Edit Logic (to be extracted)
```csharp
// Source: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs [CITED]
private async Task EditExistingAssetAsync(AssetTypes assetType)
{
    AssetDetailsInputDTO details;
    switch (assetType)
    {
        case AssetTypes.Stock:
        // ... basic path with price fetch
        case AssetTypes.RealEstate:
        // ... real estate path
        case AssetTypes.LeveragedPosition:
        // ... leveraged path with price fetch
        case AssetTypes.BtcLoan:
        // ... BTC loan path with price fetch
        case AssetTypes.BtcLending:
        // ... BTC lending path
    }
    var result = await _commandDispatcher!.DispatchAsync(new EditAssetCommand { AssetId = _assetId!, Name = Name, Details = details, ... });
}
```

## Extraction Strategy

### Proposed Builder Interface
```csharp
// Proposed for this phase
public interface IAssetFormBuilder
{
    // Create path: returns a UI-layer envelope wrapping the concrete create command.
    Task<CreateAssetCommandEnvelope> BuildCreateCommandAsync(AssetFormSnapshot snapshot);

    // Edit path: returns the polymorphic AssetDetailsInputDTO consumed by EditAssetCommand.
    Task<AssetDetailsInputDTO> BuildEditDetailsAsync(AssetFormSnapshot snapshot);

    // Load path: returns form values from an AssetDTO for the edit modal.
    AssetFormValues LoadFromDto(AssetDTO dto);
}
```

### Why an Envelope for Create Commands
The five create commands (`CreateBasicAssetCommand`, `CreateRealEstateAssetCommand`, `CreateLeveragedPositionCommand`, `CreateBtcLoanCommand`, `CreateBtcLendingCommand`) do not share a common interface and have different generic result types. Introducing a UI-layer envelope keeps the ViewModel switch small and type-safe without modifying the App-layer command contracts. This is analogous to Phase 17's single `BuildDto` returning the polymorphic `TransactionDetailsDto` [CITED: .planning/phases/17-transaction-editor-builder/17-RESEARCH.md].

### Async Construction
Price fetching for basic assets, leveraged positions, and BTC loans is asynchronous. Both `BuildCreateCommandAsync` and `BuildEditDetailsAsync` must therefore be `Task<T>`. The builder receives `IAssetPriceProviderSelector` via constructor injection.

### Load Ordering
`LoadFromDto` returns all type-specific values plus `SelectedAssetType`, but the ViewModel must apply `SelectedAssetType` last (after dates and other fields) to preserve the existing date-picker binding behavior documented in the current code [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs].

### DI Registration
```csharp
// Add to src/Valt.UI/Extensions.cs alongside other UI singleton services [CITED]
services.AddSingleton<IAssetFormBuilder, AssetFormBuilder>();
```

## File Changes

### New Files
| File | Purpose |
|------|---------|
| `src/Valt.UI/Services/IAssetFormBuilder.cs` | Interface, `AssetFormSnapshot`, `AssetFormValues`, `CreateAssetCommandEnvelope` hierarchy |
| `src/Valt.UI/Services/AssetFormBuilder.cs` | Implementation of `IAssetFormBuilder` |
| `src/Valt.UI/Services/Exceptions/AssetFormBuildException.cs` | Exception for unhandled asset types or invalid form state |
| `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` | Unit tests for builder create/edit/load paths |

### Modified Files
| File | Change |
|------|--------|
| `src/Valt.UI/Extensions.cs` | Register `IAssetFormBuilder` as singleton |
| `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` | Inject `IAssetFormBuilder`; delegate create/edit construction and load to it |
| `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs` | Update `CreateViewModel()` helper to pass a real `AssetFormBuilder` instance |

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Polymorphic create command return | `object` + runtime casts | UI-layer envelope record (`CreateAssetCommandEnvelope`) | Type-safe switch in the ViewModel; no App-layer changes required. |
| Asset type dispatch | String switches on `SelectedAssetType` | `Enum.Parse<AssetTypes>(snapshot.SelectedAssetType)` + switch expression | Mirrors existing VM parse and avoids brittle string constants. |
| Price fetching orchestration | Inline `GetPriceAsync` calls duplicated in VM and builder | Single async method in builder | Keeps price-resolution rules co-located and testable. |
| DTO subtype mapping | Custom reflection-based mapper | Explicit switch/pattern match on `AssetTypes` | The five DTO/command subtypes have different required-init semantics; a generic mapper would miss them. |
| DI registration | Manual service locator | `IServiceCollection.AddSingleton<IAssetFormBuilder, AssetFormBuilder>()` in `Valt.UI.Extensions.cs` [CITED: src/Valt.UI/Extensions.cs] | Follows the project's existing registration pattern. |

**Key insight:** The complexity here is not individual field mappings but preserving the exact behavior across 9 asset types, including async price resolution, fixed-debt APR derivation, and leveraged-position input-mode derivation. Explicit, tested mapping is safer than abstraction.

## Runtime State Inventory

This is a code-only refactor. No runtime state outside the repository needs to change.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — no database schema or persisted string/key changes | N/A |
| Live service config | None — no external service configuration affected | N/A |
| OS-registered state | None — no OS registrations reference manage-asset internals | N/A |
| Secrets/env vars | None — no secrets or environment variables affected | N/A |
| Build artifacts | Recompiled assemblies after source changes | Standard `dotnet build` |

**Nothing found in category:** All categories explicitly verified as "None."

## Common Pitfalls (Risk Areas)

### Pitfall 1: Losing Async Price Fetching During Create/Edit
**What goes wrong:** Basic assets, leveraged positions, and BTC loans fetch live/manual prices before constructing commands. If the builder does not perform this fetch, the ViewModel must still do it, leaving construction logic split between layers.
**Why it happens:** Price fetching is currently interleaved with command construction in the VM.
**How to avoid:** Make `BuildCreateCommandAsync` and `BuildEditDetailsAsync` async and inject `IAssetPriceProviderSelector` into the builder. Cover non-manual price sources in tests.
**Warning signs:** Tests pass but integration/runtime shows stale or zero prices for YahooFinance/LivePrice assets.

### Pitfall 2: Fixed-Total-Debt APR Logic Drift
**What goes wrong:** For BTC loans with `UseFixedTotalDebt`, the VM currently sets `Apr = 0m` on creation and lets the edit handler derive the APR. If the builder sets a non-zero APR or omits `FixedTotalDebt`, behavior changes.
**Why it happens:** The fixed-debt path has a special `Apr = 0` convention in the create command.
**How to avoid:** Replicate the exact conditional: `Apr = UseFixedTotalDebt ? 0m : AprPercentage / 100m` and `FixedTotalDebt = UseFixedTotalDebt ? FixedTotalDebtFiat.Value : null`.
**Warning signs:** BTC loan create/edit tests show unexpected APR values or validation failures.

### Pitfall 3: Leveraged Position Input-Mode Derivation
**What goes wrong:** The edit handler recalculates collateral when `InputMode == ExactPosition` and `PositionSize > 0` [CITED: src/Valt.App/Modules/Assets/Commands/EditAsset/EditAssetHandler.cs]. The VM currently passes `InputMode = UseExactPosition ? 1 : 0` and `PositionSize = UseExactPosition ? PositionSize : null`. Losing this mapping changes collateral values.
**Why it happens:** Input mode is stored as an int and interpreted in the handler.
**How to avoid:** Preserve `InputMode` and `PositionSize` mapping exactly; add tests for both collateral and exact-position modes.
**Warning signs:** Leveraged-position edit tests fail collateral assertions.

### Pitfall 4: Loading `SelectedAssetType` Too Early
**What goes wrong:** Avalonia `CalendarDatePicker` bindings may fail to populate if the type-specific section becomes visible before the date values are set.
**Why it happens:** The current VM deliberately sets `SelectedAssetType` last in `OnBindParameterAsync`.
**How to avoid:** `LoadFromDto` returns `SelectedAssetType` in the values record, but the VM must assign all other fields before assigning it.
**Warning signs:** Edit modal shows empty date pickers for real estate or basic assets after the refactor.

### Pitfall 5: Nullable Field Mapping Errors
**What goes wrong:** Many DTO fields are optional (e.g., `AcquisitionDate`, `AcquisitionPrice`, `MonthlyRentalIncome`, `RepaymentDate`). Coercing nulls to defaults (e.g., `0` or `DateTime.MinValue`) changes persisted data.
**Why it happens:** The VM currently uses conditional expressions like `value > 0 ? value : null` and null-checks on dates.
**How to avoid:** Preserve all nullability conditionals exactly; test with and without optional values for each asset type.
**Warning signs:** Edit an asset with no acquisition date and the saved asset now has an unexpected default date.

### Pitfall 6: Breaking the Design-Time Constructor
**What goes wrong:** Avalonia XAML designer fails to load `ManageAssetView.axaml` if the design-time constructor path requires the new service.
**Why it happens:** `ManageAssetViewModel` has a parameterless constructor used by the designer [CITED: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs].
**How to avoid:** Add the builder as an optional/nullable constructor parameter or default it only in the design-time constructor.
**Warning signs:** Designer shows "No parameterless constructor" or the view fails to render at design time.

### Pitfall 7: App-Layer Dependency Leak
**What goes wrong:** Build or architecture tests fail because `Valt.App` gains a dependency on `Valt.UI` or Avalonia.
**Why it happens:** Copying VM code naively can pull in UI namespaces if the builder is placed in the wrong layer.
**How to avoid:** Keep the builder in `Valt.UI` and ensure its public signatures use only `Valt.App`, `Valt.Core`, and `Valt.Infra` types (the price provider already lives in `Valt.Infra`).
**Warning signs:** `LayerDependencyTests.App_Should_Not_Reference_UI_Layer` fails after the refactor.

## Code Examples

Verified patterns from official sources:

### Current Basic-Asset Create Construction
```csharp
// Source: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs [CITED]
var basicResult = await _commandDispatcher!.DispatchAsync(new CreateBasicAssetCommand
{
    Name = Name,
    AssetType = (int)assetType,
    CurrencyCode = SelectedCurrency,
    Symbol = Symbol,
    Quantity = Quantity,
    CurrentPrice = currentPrice,
    PriceSource = (int)priceSource,
    AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value) : null,
    AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null,
    IncludeInNetWorth = IncludeInNetWorth,
    Visible = Visible
});
```

### Current BTC-Loan Edit Construction
```csharp
// Source: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs [CITED]
details = new BtcLoanDetailsInputDTO
{
    CurrencyCode = SelectedCurrency,
    PlatformName = PlatformName,
    CollateralSats = CollateralSats,
    LoanAmount = LoanAmountFiat.Value,
    Apr = UseFixedTotalDebt ? 0m : AprPercentage / 100m,
    InitialLtv = InitialLtvPercentage,
    LiquidationLtv = LiquidationLtvPercentage,
    MarginCallLtv = MarginCallLtvPercentage,
    Fees = FeesFiat.Value,
    LoanStartDate = LoanStartDate.HasValue ? DateOnly.FromDateTime(LoanStartDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
    RepaymentDate = RepaymentDateOffset.HasValue ? DateOnly.FromDateTime(RepaymentDateOffset.Value) : null,
    CurrentBtcPrice = editBtcPrice,
    FixedTotalDebt = UseFixedTotalDebt ? FixedTotalDebtFiat.Value : null
};
```

### Proposed Builder Create Usage in ViewModel
```csharp
// Proposed for this phase
var envelope = await _assetFormBuilder!.BuildCreateCommandAsync(snapshot);
var result = envelope switch
{
    BasicAssetCommandEnvelope basic => await _commandDispatcher!.DispatchAsync(basic.Command),
    RealEstateAssetCommandEnvelope realEstate => await _commandDispatcher!.DispatchAsync(realEstate.Command),
    LeveragedPositionCommandEnvelope leveraged => await _commandDispatcher!.DispatchAsync(leveraged.Command),
    BtcLoanCommandEnvelope btcLoan => await _commandDispatcher!.DispatchAsync(btcLoan.Command),
    BtcLendingCommandEnvelope btcLending => await _commandDispatcher!.DispatchAsync(btcLending.Command),
    _ => throw new AssetFormBuildException()
};
```

### Proposed Builder Edit Usage in ViewModel
```csharp
// Proposed for this phase
var details = await _assetFormBuilder!.BuildEditDetailsAsync(snapshot);
var result = await _commandDispatcher!.DispatchAsync(new EditAssetCommand
{
    AssetId = _assetId!,
    Name = Name,
    Details = details,
    IncludeInNetWorth = IncludeInNetWorth,
    Visible = Visible
});
```

### Proposed Load Usage in ViewModel
```csharp
// Proposed for this phase
var values = _assetFormBuilder!.LoadFromDto(assetDto);

// Apply common fields
Name = values.Name;
SelectedCurrency = values.SelectedCurrency;
IncludeInNetWorth = values.IncludeInNetWorth;
Visible = values.Visible;

// Apply type-specific fields (all except SelectedAssetType)
// ...

// Set asset type LAST
SelectedAssetType = values.SelectedAssetType;
```

## State of the Art

No new framework or library is being introduced. The refactor continues the v0.4 ViewModel simplification effort (VM-SVC-01 through VM-SVC-04) [CITED: .planning/ROADMAP.md].

**Deprecated/outdated:** None.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | The builder should live in `Valt.UI` rather than `Valt.App` | Standard Stack / Architecture Patterns | If the team prefers App-layer placement, form-state records must avoid UI/Avalonia types and the price provider abstraction must be acceptable in App. |
| A2 | A UI-layer `CreateAssetCommandEnvelope` record is acceptable to model polymorphic create commands | Extraction Strategy | If the team dislikes envelope records, the interface can be split into per-type methods, leaving a larger switch in the VM. |
| A3 | All 9 asset types can be covered by 5 create commands / 5 edit DTOs | Domain Summary | Confirmed by code inspection; 5 basic types share `CreateBasicAssetCommand` and `BasicAssetDetailsInputDTO`. |
| A4 | The existing `IAssetPriceProviderSelector` can be injected into the UI-layer builder | Extraction Strategy | The interface lives in `Valt.Infra`, which `Valt.UI` already references. |

## Open Questions

1. **Should the builder expose per-type create methods instead of a single envelope-returning method?**
   - What we know: The envelope keeps the VM switch small and mirrors Phase 17's polymorphic `BuildDto`.
   - What's unclear: Whether the planner prefers explicit per-type methods to avoid introducing a new envelope record.
   - Recommendation: Use the envelope pattern; it is concise, type-safe, and localizes all per-type construction in the builder.

2. **Should the builder also construct the `EditAssetCommand` wrapper, or only the `AssetDetailsInputDTO`?**
   - What we know: The edit command always has the same shape except for `Details`.
   - What's unclear: Whether the builder should return a full `EditAssetCommand` or just the DTO.
   - Recommendation: Return only `AssetDetailsInputDTO`; the VM already has `Name`, `IncludeInNetWorth`, `Visible`, and `_assetId` and should own the wrapper command for consistency with the create path.

3. **How should `AssetFormSnapshot` handle the large number of form fields?**
   - What we know: The VM has ~35 form fields across all asset types.
   - What's unclear: Whether to use one large record or split into per-family records.
   - Recommendation: One flat record is simplest for the VM call site and mirrors Phase 17's single `TransactionFormSnapshot`; unused fields default naturally for per-type tests.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build/test | ✓ | 10.0.100 | — |
| Avalonia designer | XAML validation | ✓ | (via packages) | Manual runtime check |
| NUnit test runner | Unit tests | ✓ | (via packages) | — |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** None.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit 4.x (via NUnit package) |
| Config file | None — uses convention |
| Quick run command | `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| VM-SVC-02 | Builder builds `CreateBasicAssetCommand` for Stock/ETF/Crypto/Commodity/Custom | Unit | `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"` | ❌ Wave 0 |
| VM-SVC-02 | Builder fetches price for non-manual basic asset | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateRealEstateAssetCommand` | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateLeveragedPositionCommand` (collateral mode) | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateLeveragedPositionCommand` (exact position mode) | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder fetches price for non-manual leveraged position | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateBtcLoanCommand` with fixed total debt | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateBtcLoanCommand` with APR and fetches BTC price | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `CreateBtcLendingCommand` | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `BasicAssetDetailsInputDTO` for edit | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `RealEstateAssetDetailsInputDTO` for edit | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `LeveragedPositionDetailsInputDTO` for edit | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `BtcLoanDetailsInputDTO` for edit with fixed total debt | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder builds `BtcLendingDetailsInputDTO` for edit | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | Builder loads form values from `AssetDTO` for all 9 asset types | Unit | Same filter | ❌ Wave 0 |
| VM-SVC-02 | `ManageAssetViewModel` still dispatches correct commands after refactor | Unit/Integration | Existing `ManageAssetViewModelTests` + builder tests | ✅ Existing |
| VM-SVC-02 | Layer dependency tests still pass | Architecture | `dotnet test --filter "FullyQualifiedName~LayerDependencyTests"` | ✅ Existing |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` — covers create/edit/load paths
- [ ] `src/Valt.UI/Services/IAssetFormBuilder.cs` — interface and records
- [ ] `src/Valt.UI/Services/AssetFormBuilder.cs` — implementation
- [ ] `src/Valt.UI/Services/Exceptions/AssetFormBuildException.cs` — exception type
- [ ] `src/Valt.UI/Extensions.cs` — DI registration

*(If no gaps: "None — existing test infrastructure covers all phase requirements")*

## Security Domain

This refactor does not introduce new security surface area. The builder operates on in-memory form values and DTOs already validated by the ViewModel and command handlers.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | N/A |
| V3 Session Management | No | N/A |
| V4 Access Control | No | N/A |
| V5 Input Validation | No | Validation remains in ViewModel/command handlers; builder receives validated values |
| V6 Cryptography | No | N/A |

### Known Threat Patterns for the Stack

No new threats introduced by this phase.

## Sources

### Primary (HIGH confidence)
- `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` — current create/edit/load logic
- `src/Valt.App/Modules/Assets/DTOs/AssetDetailsInputDTO.cs` — edit DTO subtypes
- `src/Valt.App/Modules/Assets/Commands/*/*Command.cs` — create command shapes
- `src/Valt.App/Modules/Assets/DTOs/AssetDTO.cs` — read DTO shape
- `.planning/phases/17-transaction-editor-builder/17-RESEARCH.md` — Phase 17 precedent
- `src/Valt.UI/Services/ITransactionDetailsBuilder.cs` and `TransactionDetailsBuilder.cs` — implemented Phase 17 pattern

### Secondary (MEDIUM confidence)
- `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs` — existing ViewModel test pattern
- `tests/Valt.Tests/Architecture/LayerDependencyTests.cs` — layer dependency constraints
- `src/Valt.App/Modules/Assets/Commands/EditAsset/EditAssetHandler.cs` — handler-side derivation rules for leveraged positions and BTC loans

### Tertiary (LOW confidence)
- None.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, existing .NET 10 / Avalonia / NUnit stack [CITED: project files]
- Architecture: MEDIUM — builder location and shape are recommended based on Phase 17 precedent and codebase patterns; final envelope-vs-per-method decision is discretion
- Pitfalls: MEDIUM — derived from direct code reading and the Phase 17 refactor experience

**Research date:** 2026-06-23
**Valid until:** 2026-07-23 (stable internal refactor)
