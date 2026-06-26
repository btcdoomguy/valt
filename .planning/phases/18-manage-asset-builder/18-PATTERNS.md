# Phase 18: Manage Asset Builder - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 7
**Analogs found:** 7 / 7

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.UI/Services/IAssetFormBuilder.cs` | service interface | transform | `src/Valt.UI/Services/ITransactionDetailsBuilder.cs` | exact |
| `src/Valt.UI/Services/AssetFormBuilder.cs` | service | transform | `src/Valt.UI/Services/TransactionDetailsBuilder.cs` | exact |
| `src/Valt.UI/Services/Exceptions/AssetFormBuildException.cs` | utility | error | `src/Valt.UI/Views/Main/Modals/TransactionEditor/Exceptions/TransactionDetailsBuildException.cs` | role-match |
| `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` | test | transform | `tests/Valt.Tests/UI/Services/TransactionDetailsBuilderTests.cs` | exact |
| `src/Valt.UI/Extensions.cs` | config | registration | `src/Valt.UI/Extensions.cs` (existing `ITransactionDetailsBuilder` line) | exact |
| `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` | component | request-response/transform | `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` (self, current create/edit/load logic) | exact |
| `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs` | test | request-response | `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs` (self) | exact |

## Pattern Assignments

### `src/Valt.UI/Services/IAssetFormBuilder.cs` (service interface, transform)

**Analog:** `src/Valt.UI/Services/ITransactionDetailsBuilder.cs`

**File layout pattern** (lines 1-43):
```csharp
using System.Collections.Generic;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.UI.Services;

public sealed record TransactionFormSnapshot(...);

public sealed record TransactionFormValues(...);

public interface ITransactionDetailsBuilder
{
    TransactionDetailsDto BuildDto(TransactionFormSnapshot snapshot);

    TransactionFormValues LoadFromDto(
        TransactionDetailsDto dto,
        IReadOnlyList<AccountDTO> availableAccounts);
}
```

**Pattern notes:** Place records and interface in the same file under `Valt.UI.Services`. Snapshot carries VM form state; values record carries DTO-to-VM state. Use `sealed record` with named positional parameters. For Phase 18, add async `BuildCreateCommandAsync` and `BuildEditDetailsAsync` plus synchronous `LoadFromDto`, plus the `CreateAssetCommandEnvelope` hierarchy in the same file.

---

### `src/Valt.UI/Services/AssetFormBuilder.cs` (service, transform)

**Analog:** `src/Valt.UI/Services/TransactionDetailsBuilder.cs`

**Imports pattern** (lines 1-11):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

namespace Valt.UI.Services;
```

**Core pattern** (lines 13-78):
```csharp
public class TransactionDetailsBuilder : ITransactionDetailsBuilder
{
    public TransactionDetailsDto BuildDto(TransactionFormSnapshot snapshot)
    {
        var fromAccountType = ParseAccountType(snapshot.FromAccount.Type);
        var toAccountType = snapshot.ToAccount is not null
            ? ParseAccountType(snapshot.ToAccount.Type)
            : (AccountTypes?)null;

        if (snapshot.SelectedMode == TransactionTypes.Transfer)
        {
            // ... switch expression returning concrete DTO subtype
            return (fromAccountType, toAccountType) switch
            {
                (AccountTypes.Bitcoin, AccountTypes.Bitcoin) => new BitcoinToBitcoinTransferDto { ... },
                ...
                _ => throw new TransactionDetailsBuildException()
            };
        }

        return fromAccountType switch
        {
            AccountTypes.Bitcoin => new BitcoinTransactionDto { ... },
            AccountTypes.Fiat => new FiatTransactionDto { ... },
            _ => throw new TransactionDetailsBuildException()
        };
    }
```

**Async construction / price fetch pattern** (from `ManageAssetViewModel.cs` lines 634-645):
```csharp
var priceSource = Enum.Parse<AssetPriceSource>(SelectedPriceSource);
var currentPrice = CurrentPriceFiat.Value;

// Fetch price from provider if not Manual
if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(Symbol))
{
    var priceResult = await _priceProviderSelector!.GetPriceAsync(priceSource, Symbol, SelectedCurrency);
    if (priceResult is not null)
    {
        currentPrice = priceResult.Price;
    }
}
```

**LoadFromDto pattern** (lines 80-149):
```csharp
public TransactionFormValues LoadFromDto(
    TransactionDetailsDto dto,
    IReadOnlyList<AccountDTO> availableAccounts)
{
    switch (dto)
    {
        case FiatTransactionDto fiat:
            return new TransactionFormValues(
                SelectedMode: fiat.IsCredit ? TransactionTypes.Credit : TransactionTypes.Debt,
                ...);

        case BitcoinTransactionDto btc:
            ...

        default:
            throw new TransactionDetailsBuildException();
    }
}
```

**Error handling pattern:**
- Use a dedicated exception type (`TransactionDetailsBuildException`) for unreachable/default branches.
- Throw for null required dependencies (e.g., `snapshot.ToAccount is null` in transfer mode).

---

### `src/Valt.UI/Services/Exceptions/AssetFormBuildException.cs` (utility, error)

**Analog:** `src/Valt.UI/Views/Main/Modals/TransactionEditor/Exceptions/TransactionDetailsBuildException.cs`

**Exception pattern** (lines 1-10):
```csharp
using System;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

public class TransactionDetailsBuildException : Exception
{
    public TransactionDetailsBuildException() : base("Error while building transaction details.")
    {
    }
}
```

**Pattern notes:** Keep the namespace under `Valt.UI.Services.Exceptions`. Provide a parameterless constructor with a descriptive message. Use this exception in builder switch default branches and invalid state checks.

---

### `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs` (test, transform)

**Analog:** `tests/Valt.Tests/UI/Services/TransactionDetailsBuilderTests.cs`

**Test fixture pattern** (lines 1-58):
```csharp
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Services;

namespace Valt.Tests.UI.Services;

[TestFixture]
public class TransactionDetailsBuilderTests
{
    private TransactionDetailsBuilder _builder = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new TransactionDetailsBuilder();
    }
```

**Build test pattern** (lines 62-86):
```csharp
[Test]
public void BuildDto_FiatDebt_ReturnsFiatTransactionDto()
{
    var accountId = "fiat-debt";
    AddFiatAccount(accountId, "Checking", FiatCurrency.Usd);
    var account = _accounts[0];

    var snapshot = new TransactionFormSnapshot(
        SelectedMode: TransactionTypes.Debt,
        FromAccount: account,
        ...);

    var dto = _builder.BuildDto(snapshot);

    Assert.That(dto, Is.TypeOf<FiatTransactionDto>());
    var fiat = (FiatTransactionDto)dto;
    Assert.That(fiat.FromAccountId, Is.EqualTo(accountId));
}
```

**Async price-provider mock pattern** (from `ManageAssetViewModelTests.cs` lines 41-43, 113-115):
```csharp
_priceProviderSelector = Substitute.For<IAssetPriceProviderSelector>();

_priceProviderSelector.GetPriceAsync(Arg.Any<AssetPriceSource>(), Arg.Any<string>(), Arg.Any<string>())
    .Returns(new AssetPriceResult(...));
```

**LoadFromDto test pattern** (lines 307-325):
```csharp
[Test]
public void LoadFromDto_FiatTransactionDebt_RoundTripsValues()
{
    var dto = new FiatTransactionDto { ... };

    var values = _builder.LoadFromDto(dto, _accounts);

    Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Debt));
    Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(150m)));
}
```

**Pattern notes:** For `AssetFormBuilder`, instantiate the builder with a mocked `IAssetPriceProviderSelector` in `SetUp`. Create helper methods to build `AssetFormSnapshot` instances per asset family. Assert on concrete command/DTO types and exact property values, especially edge cases: `UseFixedTotalDebt`, `UseExactPosition`, `AcquisitionDate` nullability, and fetched vs. manual prices.

---

### `src/Valt.UI/Extensions.cs` (config, registration)

**Analog:** `src/Valt.UI/Extensions.cs` (existing registration line 99)

**DI registration pattern** (line 99):
```csharp
services.AddSingleton<ITransactionDetailsBuilder, TransactionDetailsBuilder>();
```

**Pattern notes:** Add `services.AddSingleton<IAssetFormBuilder, AssetFormBuilder>();` alongside the other UI service registrations near line 99. The `using Valt.UI.Services;` namespace already covers both services.

---

### `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` (component, request-response/transform)

**Analog:** `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` (current code)

**Constructor injection pattern** (lines 338-357):
```csharp
public ManageAssetViewModel(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher,
    IAssetPriceProviderSelector priceProviderSelector,
    CurrencySettings currencySettings,
    IConfigurationManager configurationManager,
    ILogger<ManageAssetViewModel> logger,
    IModalFactory modalFactory)
{
    _queryDispatcher = queryDispatcher;
    _commandDispatcher = commandDispatcher;
    _priceProviderSelector = priceProviderSelector;
    ...
}
```

**Design-time constructor pattern** (lines 333-336):
```csharp
public ManageAssetViewModel()
{
    SelectedAssetType = AssetTypes.Stock.ToString();
}
```

**Pattern notes:** Add `IAssetFormBuilder? _assetFormBuilder;` as a nullable field. Add the builder as an optional/nullable constructor parameter so the parameterless design-time constructor continues to work. Default it to `null` and guard with `!` at call sites (same pattern as other injected services).

**Current create logic to extract** (lines 625-802):
```csharp
private async Task CreateNewAssetAsync(AssetTypes assetType)
{
    switch (assetType)
    {
        case AssetTypes.Stock:
        case AssetTypes.Etf:
        case AssetTypes.Crypto:
        case AssetTypes.Commodity:
        case AssetTypes.Custom:
            // price fetch + CreateBasicAssetCommand
        case AssetTypes.RealEstate:
            // CreateRealEstateAssetCommand
        case AssetTypes.LeveragedPosition:
            // price fetch + CreateLeveragedPositionCommand
        case AssetTypes.BtcLoan:
            // BTC price fetch + CreateBtcLoanCommand
        case AssetTypes.BtcLending:
            // CreateBtcLendingCommand
    }
}
```

**Current edit logic to extract** (lines 804-945):
```csharp
private async Task EditExistingAssetAsync(AssetTypes assetType)
{
    AssetDetailsInputDTO details;
    switch (assetType) { ... } // build subtype DTOs
    var result = await _commandDispatcher!.DispatchAsync(new EditAssetCommand { ... });
}
```

**Current load logic to extract** (lines 359-461):
```csharp
public override async Task OnBindParameterAsync()
{
    ...
    switch ((AssetTypes)assetDto.AssetTypeId)
    {
        // populate VM properties from AssetDTO
    }
    // Set asset type LAST
    SelectedAssetType = ((AssetTypes)assetDto.AssetTypeId).ToString();
}
```

**Proposed delegation pattern** (from RESEARCH.md lines 513-555):
```csharp
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

---

### `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs` (test, request-response)

**Analog:** `tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs`

**Constructor helper pattern** (lines 61-62):
```csharp
private ManageAssetViewModel CreateViewModel()
    => new(_queryDispatcher, _commandDispatcher, _priceProviderSelector, _currencySettings, _configurationManager, _logger, _modalFactory);
```

**Pattern notes:** Update `CreateViewModel()` to pass a real `AssetFormBuilder` instance with the existing `_priceProviderSelector` mock. Add `private IAssetFormBuilder _assetFormBuilder;` and initialize it in `SetUp`. Example:
```csharp
_assetFormBuilder = new AssetFormBuilder(_priceProviderSelector);
private ManageAssetViewModel CreateViewModel()
    => new(_queryDispatcher, _commandDispatcher, _priceProviderSelector, _currencySettings, _configurationManager, _logger, _modalFactory, _assetFormBuilder);
```

**DTO helper pattern** (lines 64-105):
```csharp
private static AssetDTO CreateStockAssetDto(DateOnly? acquisitionDate = null) => new()
{
    Id = "asset-1",
    Name = "AAPL",
    AssetTypeId = (int)AssetTypes.Stock,
    ...
};
```

---

## Shared Patterns

### UI Service Registration
**Source:** `src/Valt.UI/Extensions.cs` line 99
**Apply to:** `AssetFormBuilder` registration
```csharp
services.AddSingleton<ITransactionDetailsBuilder, TransactionDetailsBuilder>();
```

### Builder Exception Type
**Source:** `src/Valt.UI/Views/Main/Modals/TransactionEditor/Exceptions/TransactionDetailsBuildException.cs`
**Apply to:** `AssetFormBuilder` default branches and invalid state
```csharp
public class TransactionDetailsBuildException : Exception
{
    public TransactionDetailsBuildException() : base("Error while building transaction details.")
    {
    }
}
```

### Async Price Fetching
**Source:** `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` lines 638-645 and 740-744
**Apply to:** `AssetFormBuilder` basic, leveraged, and BTC loan create/edit paths
```csharp
var priceResult = await _priceProviderSelector!.GetPriceAsync(priceSource, Symbol, SelectedCurrency);
if (priceResult is not null)
{
    currentPrice = priceResult.Price;
}
```

### Asset Type Dispatch
**Source:** `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` lines 627-802
**Apply to:** `AssetFormBuilder` switch expressions
```csharp
var assetType = Enum.Parse<AssetTypes>(snapshot.SelectedAssetType);
switch (assetType)
{
    case AssetTypes.Stock:
    case AssetTypes.Etf:
    ...
}
```

### DTO Nullability Mapping
**Source:** `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` lines 656-657, 758-761, 836-837
**Apply to:** All create/edit mappings in `AssetFormBuilder`
```csharp
AcquisitionDate = AcquisitionDate.HasValue ? DateOnly.FromDateTime(AcquisitionDate.Value) : null,
AcquisitionPrice = AcquisitionPriceFiat.Value > 0 ? AcquisitionPriceFiat.Value : null,
```

### Load Ordering (SelectedAssetType Last)
**Source:** `src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs` lines 373-460
**Apply to:** Refactored `ManageAssetViewModel.OnBindParameterAsync`
```csharp
// Load common fields
Name = assetDto.Name;
...

// Load type-specific fields BEFORE setting SelectedAssetType
switch (...) { ... }

// Set asset type LAST
SelectedAssetType = ((AssetTypes)assetDto.AssetTypeId).ToString();
```

## No Analog Found

No files without analogs. All new and modified files have clear existing patterns to copy from.

## Metadata

**Analog search scope:** `src/Valt.UI/Services`, `src/Valt.UI/Views/Main/Modals/ManageAsset`, `src/Valt.UI/Views/Main/Modals/TransactionEditor/Exceptions`, `tests/Valt.Tests/UI/Services`, `tests/Valt.Tests/UI/Screens`, `src/Valt.App/Modules/Assets/Commands`, `src/Valt.App/Modules/Assets/DTOs`
**Files scanned:** 18
**Pattern extraction date:** 2026-06-23
