using System;
using System.Threading.Tasks;
using Valt.App.Modules.Assets.Commands.CreateBasicAsset;
using Valt.App.Modules.Assets.Commands.CreateBtcLending;
using Valt.App.Modules.Assets.Commands.CreateBtcLoan;
using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Assets;
using Valt.Infra.Modules.Assets.PriceProviders;
using Valt.UI.Services.Exceptions;

namespace Valt.UI.Services;

public class AssetFormBuilder : IAssetFormBuilder
{
    private readonly IAssetPriceProviderSelector _priceProviderSelector;

    public AssetFormBuilder(IAssetPriceProviderSelector priceProviderSelector)
    {
        _priceProviderSelector = priceProviderSelector;
    }

    public async Task<CreateAssetCommandEnvelope> BuildCreateCommandAsync(AssetFormSnapshot snapshot)
    {
        if (!Enum.TryParse<AssetTypes>(snapshot.SelectedAssetType, out var assetType))
            throw new AssetFormBuildException();

        switch (assetType)
        {
            case AssetTypes.Stock:
            case AssetTypes.Etf:
            case AssetTypes.Crypto:
            case AssetTypes.Commodity:
            case AssetTypes.Custom:
                var priceSource = Enum.Parse<AssetPriceSource>(snapshot.SelectedPriceSource);
                var currentPrice = snapshot.CurrentPriceFiat.Value;

                if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(snapshot.Symbol))
                {
                    var priceResult = await _priceProviderSelector.GetPriceAsync(priceSource, snapshot.Symbol, snapshot.SelectedCurrency);
                    if (priceResult is not null)
                        currentPrice = priceResult.Price;
                }

                return new BasicAssetCommandEnvelope(new CreateBasicAssetCommand
                {
                    Name = snapshot.Name,
                    AssetType = (int)assetType,
                    CurrencyCode = snapshot.SelectedCurrency,
                    Symbol = snapshot.Symbol,
                    Quantity = snapshot.Quantity,
                    CurrentPrice = currentPrice,
                    PriceSource = (int)priceSource,
                    AcquisitionDate = snapshot.AcquisitionDate.HasValue ? DateOnly.FromDateTime(snapshot.AcquisitionDate.Value) : null,
                    AcquisitionPrice = snapshot.AcquisitionPriceFiat.Value > 0 ? snapshot.AcquisitionPriceFiat.Value : null,
                    IncludeInNetWorth = snapshot.IncludeInNetWorth,
                    Visible = snapshot.Visible
                });

            case AssetTypes.RealEstate:
                return new RealEstateAssetCommandEnvelope(new CreateRealEstateAssetCommand
                {
                    Name = snapshot.Name,
                    CurrencyCode = snapshot.SelectedCurrency,
                    CurrentValue = snapshot.CurrentValueFiat.Value,
                    Address = string.IsNullOrWhiteSpace(snapshot.Address) ? null : snapshot.Address,
                    MonthlyRentalIncome = snapshot.MonthlyRentalIncomeFiat.Value > 0 ? snapshot.MonthlyRentalIncomeFiat.Value : null,
                    AcquisitionDate = snapshot.AcquisitionDate.HasValue ? DateOnly.FromDateTime(snapshot.AcquisitionDate.Value) : null,
                    AcquisitionPrice = snapshot.AcquisitionPriceFiat.Value > 0 ? snapshot.AcquisitionPriceFiat.Value : null,
                    IncludeInNetWorth = snapshot.IncludeInNetWorth,
                    Visible = snapshot.Visible
                });

            case AssetTypes.LeveragedPosition:
                var leveragedPriceSource = Enum.Parse<AssetPriceSource>(snapshot.SelectedPriceSource);
                var leveragedCurrentPrice = snapshot.CurrentPriceFiat.Value;

                if (leveragedPriceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(snapshot.Symbol))
                {
                    var priceResult = await _priceProviderSelector.GetPriceAsync(leveragedPriceSource, snapshot.Symbol, snapshot.SelectedCurrency);
                    if (priceResult is not null)
                        leveragedCurrentPrice = priceResult.Price;
                }

                return new LeveragedPositionCommandEnvelope(new CreateLeveragedPositionCommand
                {
                    Name = snapshot.Name,
                    CurrencyCode = snapshot.SelectedCurrency,
                    Symbol = snapshot.Symbol,
                    Collateral = snapshot.CollateralFiat.Value,
                    EntryPrice = snapshot.EntryPriceFiat.Value,
                    CurrentPrice = leveragedCurrentPrice,
                    Leverage = snapshot.Leverage,
                    LiquidationPrice = snapshot.LiquidationPriceFiat.Value,
                    IsLong = snapshot.IsLong,
                    PriceSource = (int)leveragedPriceSource,
                    IncludeInNetWorth = snapshot.IncludeInNetWorth,
                    Visible = snapshot.Visible,
                    InputMode = snapshot.UseExactPosition ? 1 : 0,
                    PositionSize = snapshot.UseExactPosition ? snapshot.PositionSize : null
                });

            case AssetTypes.BtcLoan:
                var btcPriceResult = await _priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", snapshot.SelectedCurrency);
                var btcPrice = btcPriceResult?.Price ?? 0m;

                return new BtcLoanCommandEnvelope(new CreateBtcLoanCommand
                {
                    Name = snapshot.Name,
                    CurrencyCode = snapshot.SelectedCurrency,
                    PlatformName = snapshot.PlatformName,
                    CollateralSats = snapshot.CollateralSats,
                    LoanAmount = snapshot.LoanAmountFiat.Value,
                    Apr = snapshot.UseFixedTotalDebt ? 0m : snapshot.AprPercentage / 100m,
                    InitialLtv = snapshot.InitialLtvPercentage,
                    LiquidationLtv = snapshot.LiquidationLtvPercentage,
                    MarginCallLtv = snapshot.MarginCallLtvPercentage,
                    Fees = snapshot.FeesFiat.Value,
                    LoanStartDate = snapshot.LoanStartDate.HasValue ? DateOnly.FromDateTime(snapshot.LoanStartDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
                    RepaymentDate = snapshot.RepaymentDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.RepaymentDateOffset.Value) : null,
                    CurrentBtcPrice = btcPrice,
                    FixedTotalDebt = snapshot.UseFixedTotalDebt ? snapshot.FixedTotalDebtFiat.Value : null,
                    IncludeInNetWorth = snapshot.IncludeInNetWorth,
                    Visible = snapshot.Visible
                });

            case AssetTypes.BtcLending:
                return new BtcLendingCommandEnvelope(new CreateBtcLendingCommand
                {
                    Name = snapshot.Name,
                    CurrencyCode = snapshot.SelectedCurrency,
                    AmountLent = snapshot.AmountLentFiat.Value,
                    Apr = snapshot.LendingAprPercentage / 100m,
                    BorrowerOrPlatformName = snapshot.BorrowerOrPlatformName,
                    LendingStartDate = snapshot.LendingStartDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.LendingStartDateOffset.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpectedRepaymentDate = snapshot.ExpectedRepaymentDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.ExpectedRepaymentDateOffset.Value) : null,
                    IncludeInNetWorth = snapshot.IncludeInNetWorth,
                    Visible = snapshot.Visible
                });

            default:
                throw new AssetFormBuildException();
        }
    }

    public async Task<AssetDetailsInputDTO> BuildEditDetailsAsync(AssetFormSnapshot snapshot)
    {
        if (!Enum.TryParse<AssetTypes>(snapshot.SelectedAssetType, out var assetType))
            throw new AssetFormBuildException();

        switch (assetType)
        {
            case AssetTypes.Stock:
            case AssetTypes.Etf:
            case AssetTypes.Crypto:
            case AssetTypes.Commodity:
            case AssetTypes.Custom:
                var priceSource = Enum.Parse<AssetPriceSource>(snapshot.SelectedPriceSource);
                var currentPrice = snapshot.CurrentPriceFiat.Value;

                if (priceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(snapshot.Symbol))
                {
                    var priceResult = await _priceProviderSelector.GetPriceAsync(priceSource, snapshot.Symbol, snapshot.SelectedCurrency);
                    if (priceResult is not null)
                        currentPrice = priceResult.Price;
                }

                return new BasicAssetDetailsInputDTO
                {
                    AssetType = (int)assetType,
                    CurrencyCode = snapshot.SelectedCurrency,
                    Symbol = snapshot.Symbol,
                    Quantity = snapshot.Quantity,
                    CurrentPrice = currentPrice,
                    PriceSource = (int)priceSource,
                    AcquisitionDate = snapshot.AcquisitionDate.HasValue ? DateOnly.FromDateTime(snapshot.AcquisitionDate.Value) : null,
                    AcquisitionPrice = snapshot.AcquisitionPriceFiat.Value > 0 ? snapshot.AcquisitionPriceFiat.Value : null
                };

            case AssetTypes.RealEstate:
                return new RealEstateAssetDetailsInputDTO
                {
                    CurrencyCode = snapshot.SelectedCurrency,
                    CurrentValue = snapshot.CurrentValueFiat.Value,
                    Address = string.IsNullOrWhiteSpace(snapshot.Address) ? null : snapshot.Address,
                    MonthlyRentalIncome = snapshot.MonthlyRentalIncomeFiat.Value > 0 ? snapshot.MonthlyRentalIncomeFiat.Value : null,
                    AcquisitionDate = snapshot.AcquisitionDate.HasValue ? DateOnly.FromDateTime(snapshot.AcquisitionDate.Value) : null,
                    AcquisitionPrice = snapshot.AcquisitionPriceFiat.Value > 0 ? snapshot.AcquisitionPriceFiat.Value : null
                };

            case AssetTypes.LeveragedPosition:
                var leveragedPriceSource = Enum.Parse<AssetPriceSource>(snapshot.SelectedPriceSource);
                var leveragedCurrentPrice = snapshot.CurrentPriceFiat.Value;

                if (leveragedPriceSource != AssetPriceSource.Manual && !string.IsNullOrWhiteSpace(snapshot.Symbol))
                {
                    var priceResult = await _priceProviderSelector.GetPriceAsync(leveragedPriceSource, snapshot.Symbol, snapshot.SelectedCurrency);
                    if (priceResult is not null)
                        leveragedCurrentPrice = priceResult.Price;
                }

                return new LeveragedPositionDetailsInputDTO
                {
                    CurrencyCode = snapshot.SelectedCurrency,
                    Symbol = snapshot.Symbol,
                    Collateral = snapshot.CollateralFiat.Value,
                    EntryPrice = snapshot.EntryPriceFiat.Value,
                    CurrentPrice = leveragedCurrentPrice,
                    Leverage = snapshot.Leverage,
                    LiquidationPrice = snapshot.LiquidationPriceFiat.Value,
                    IsLong = snapshot.IsLong,
                    PriceSource = (int)leveragedPriceSource,
                    InputMode = snapshot.UseExactPosition ? 1 : 0,
                    PositionSize = snapshot.UseExactPosition ? snapshot.PositionSize : null
                };

            case AssetTypes.BtcLoan:
                var btcPriceResult = await _priceProviderSelector.GetPriceAsync(AssetPriceSource.LivePrice, "BTC", snapshot.SelectedCurrency);
                var btcPrice = btcPriceResult?.Price ?? 0m;

                return new BtcLoanDetailsInputDTO
                {
                    CurrencyCode = snapshot.SelectedCurrency,
                    PlatformName = snapshot.PlatformName,
                    CollateralSats = snapshot.CollateralSats,
                    LoanAmount = snapshot.LoanAmountFiat.Value,
                    Apr = snapshot.UseFixedTotalDebt ? 0m : snapshot.AprPercentage / 100m,
                    InitialLtv = snapshot.InitialLtvPercentage,
                    LiquidationLtv = snapshot.LiquidationLtvPercentage,
                    MarginCallLtv = snapshot.MarginCallLtvPercentage,
                    Fees = snapshot.FeesFiat.Value,
                    LoanStartDate = snapshot.LoanStartDate.HasValue ? DateOnly.FromDateTime(snapshot.LoanStartDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
                    RepaymentDate = snapshot.RepaymentDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.RepaymentDateOffset.Value) : null,
                    CurrentBtcPrice = btcPrice,
                    FixedTotalDebt = snapshot.UseFixedTotalDebt ? snapshot.FixedTotalDebtFiat.Value : null
                };

            case AssetTypes.BtcLending:
                return new BtcLendingDetailsInputDTO
                {
                    CurrencyCode = snapshot.SelectedCurrency,
                    AmountLent = snapshot.AmountLentFiat.Value,
                    Apr = snapshot.LendingAprPercentage / 100m,
                    BorrowerOrPlatformName = snapshot.BorrowerOrPlatformName,
                    LendingStartDate = snapshot.LendingStartDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.LendingStartDateOffset.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpectedRepaymentDate = snapshot.ExpectedRepaymentDateOffset.HasValue ? DateOnly.FromDateTime(snapshot.ExpectedRepaymentDateOffset.Value) : null
                };

            default:
                throw new AssetFormBuildException();
        }
    }

    public AssetFormValues LoadFromDto(AssetDTO dto)
    {
        var assetType = (AssetTypes)dto.AssetTypeId;
        var selectedAssetType = assetType.ToString();

        switch (assetType)
        {
            case AssetTypes.Stock:
            case AssetTypes.Etf:
            case AssetTypes.Crypto:
            case AssetTypes.Commodity:
            case AssetTypes.Custom:
                return new AssetFormValues(
                    Name: dto.Name,
                    SelectedAssetType: selectedAssetType,
                    SelectedCurrency: dto.CurrencyCode,
                    IncludeInNetWorth: dto.IncludeInNetWorth,
                    Visible: dto.Visible,
                    Symbol: dto.Symbol ?? string.Empty,
                    Quantity: dto.Quantity ?? 0,
                    CurrentPriceFiat: FiatValue.New(dto.CurrentPrice),
                    SelectedPriceSource: ((AssetPriceSource)(dto.PriceSourceId ?? 0)).ToString(),
                    Address: string.Empty,
                    CurrentValueFiat: FiatValue.Empty,
                    MonthlyRentalIncomeFiat: FiatValue.Empty,
                    AcquisitionDate: dto.AcquisitionDate.HasValue ? dto.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    AcquisitionPriceFiat: FiatValue.New(dto.AcquisitionPrice ?? 0),
                    IsBitcoinUnderlyingAsset: false,
                    CollateralFiat: FiatValue.Empty,
                    EntryPriceFiat: FiatValue.Empty,
                    Leverage: 1,
                    LiquidationPriceFiat: FiatValue.Empty,
                    IsLong: true,
                    UseExactPosition: false,
                    PositionSize: 0,
                    PlatformName: string.Empty,
                    CollateralSats: 0,
                    LoanAmountFiat: FiatValue.Empty,
                    AprPercentage: 0,
                    InitialLtvPercentage: 0,
                    LiquidationLtvPercentage: 0,
                    MarginCallLtvPercentage: 0,
                    FeesFiat: FiatValue.Empty,
                    LoanStartDate: null,
                    RepaymentDateOffset: null,
                    IsIndefiniteLoan: false,
                    UseFixedTotalDebt: false,
                    FixedTotalDebtFiat: FiatValue.Empty,
                    BorrowerOrPlatformName: string.Empty,
                    AmountLentFiat: FiatValue.Empty,
                    LendingAprPercentage: 0,
                    LendingStartDateOffset: null,
                    ExpectedRepaymentDateOffset: null,
                    IsIndefiniteLending: false);

            case AssetTypes.RealEstate:
                return new AssetFormValues(
                    Name: dto.Name,
                    SelectedAssetType: selectedAssetType,
                    SelectedCurrency: dto.CurrencyCode,
                    IncludeInNetWorth: dto.IncludeInNetWorth,
                    Visible: dto.Visible,
                    Symbol: string.Empty,
                    Quantity: 0,
                    CurrentPriceFiat: FiatValue.Empty,
                    SelectedPriceSource: AssetPriceSource.Manual.ToString(),
                    Address: dto.Address ?? string.Empty,
                    CurrentValueFiat: FiatValue.New(dto.CurrentValue),
                    MonthlyRentalIncomeFiat: FiatValue.New(dto.MonthlyRentalIncome ?? 0),
                    AcquisitionDate: dto.AcquisitionDate.HasValue ? dto.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    AcquisitionPriceFiat: FiatValue.New(dto.AcquisitionPrice ?? 0),
                    IsBitcoinUnderlyingAsset: false,
                    CollateralFiat: FiatValue.Empty,
                    EntryPriceFiat: FiatValue.Empty,
                    Leverage: 1,
                    LiquidationPriceFiat: FiatValue.Empty,
                    IsLong: true,
                    UseExactPosition: false,
                    PositionSize: 0,
                    PlatformName: string.Empty,
                    CollateralSats: 0,
                    LoanAmountFiat: FiatValue.Empty,
                    AprPercentage: 0,
                    InitialLtvPercentage: 0,
                    LiquidationLtvPercentage: 0,
                    MarginCallLtvPercentage: 0,
                    FeesFiat: FiatValue.Empty,
                    LoanStartDate: null,
                    RepaymentDateOffset: null,
                    IsIndefiniteLoan: false,
                    UseFixedTotalDebt: false,
                    FixedTotalDebtFiat: FiatValue.Empty,
                    BorrowerOrPlatformName: string.Empty,
                    AmountLentFiat: FiatValue.Empty,
                    LendingAprPercentage: 0,
                    LendingStartDateOffset: null,
                    ExpectedRepaymentDateOffset: null,
                    IsIndefiniteLending: false);

            case AssetTypes.LeveragedPosition:
                var symbol = dto.Symbol ?? string.Empty;
                var priceSource = (AssetPriceSource)(dto.PriceSourceId ?? 0);
                return new AssetFormValues(
                    Name: dto.Name,
                    SelectedAssetType: selectedAssetType,
                    SelectedCurrency: dto.CurrencyCode,
                    IncludeInNetWorth: dto.IncludeInNetWorth,
                    Visible: dto.Visible,
                    Symbol: symbol,
                    Quantity: 0,
                    CurrentPriceFiat: FiatValue.New(dto.CurrentPrice),
                    SelectedPriceSource: priceSource.ToString(),
                    Address: string.Empty,
                    CurrentValueFiat: FiatValue.Empty,
                    MonthlyRentalIncomeFiat: FiatValue.Empty,
                    AcquisitionDate: null,
                    AcquisitionPriceFiat: FiatValue.Empty,
                    IsBitcoinUnderlyingAsset: priceSource == AssetPriceSource.LivePrice && symbol.StartsWith("BTC", StringComparison.OrdinalIgnoreCase),
                    CollateralFiat: FiatValue.New(dto.Collateral ?? 0),
                    EntryPriceFiat: FiatValue.New(dto.EntryPrice ?? 0),
                    Leverage: dto.Leverage ?? 1,
                    LiquidationPriceFiat: FiatValue.New(dto.LiquidationPrice ?? 0),
                    IsLong: dto.IsLong ?? true,
                    UseExactPosition: (dto.InputModeId ?? 0) == 1,
                    PositionSize: dto.PositionSize ?? 0,
                    PlatformName: string.Empty,
                    CollateralSats: 0,
                    LoanAmountFiat: FiatValue.Empty,
                    AprPercentage: 0,
                    InitialLtvPercentage: 0,
                    LiquidationLtvPercentage: 0,
                    MarginCallLtvPercentage: 0,
                    FeesFiat: FiatValue.Empty,
                    LoanStartDate: null,
                    RepaymentDateOffset: null,
                    IsIndefiniteLoan: false,
                    UseFixedTotalDebt: false,
                    FixedTotalDebtFiat: FiatValue.Empty,
                    BorrowerOrPlatformName: string.Empty,
                    AmountLentFiat: FiatValue.Empty,
                    LendingAprPercentage: 0,
                    LendingStartDateOffset: null,
                    ExpectedRepaymentDateOffset: null,
                    IsIndefiniteLending: false);

            case AssetTypes.BtcLoan:
                DateTime? repaymentDateOffset = null;
                var isIndefiniteLoan = false;
                if (dto.RepaymentDate.HasValue)
                    repaymentDateOffset = dto.RepaymentDate.Value.ToDateTime(TimeOnly.MinValue);
                else
                    isIndefiniteLoan = true;

                var useFixedTotalDebt = dto.HasFixedTotalDebt && dto.FixedTotalDebt.HasValue;

                return new AssetFormValues(
                    Name: dto.Name,
                    SelectedAssetType: selectedAssetType,
                    SelectedCurrency: dto.CurrencyCode,
                    IncludeInNetWorth: dto.IncludeInNetWorth,
                    Visible: dto.Visible,
                    Symbol: string.Empty,
                    Quantity: 0,
                    CurrentPriceFiat: FiatValue.Empty,
                    SelectedPriceSource: AssetPriceSource.Manual.ToString(),
                    Address: string.Empty,
                    CurrentValueFiat: FiatValue.Empty,
                    MonthlyRentalIncomeFiat: FiatValue.Empty,
                    AcquisitionDate: null,
                    AcquisitionPriceFiat: FiatValue.Empty,
                    IsBitcoinUnderlyingAsset: false,
                    CollateralFiat: FiatValue.Empty,
                    EntryPriceFiat: FiatValue.Empty,
                    Leverage: 1,
                    LiquidationPriceFiat: FiatValue.Empty,
                    IsLong: true,
                    UseExactPosition: false,
                    PositionSize: 0,
                    PlatformName: dto.PlatformName ?? string.Empty,
                    CollateralSats: dto.CollateralSats ?? 0,
                    LoanAmountFiat: FiatValue.New(dto.LoanAmount ?? 0),
                    AprPercentage: (dto.Apr ?? 0) * 100m,
                    InitialLtvPercentage: dto.InitialLtv ?? 0,
                    LiquidationLtvPercentage: dto.LiquidationLtv ?? 0,
                    MarginCallLtvPercentage: dto.MarginCallLtv ?? 0,
                    FeesFiat: FiatValue.New(dto.Fees ?? 0),
                    LoanStartDate: dto.LoanStartDate.HasValue ? dto.LoanStartDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    RepaymentDateOffset: repaymentDateOffset,
                    IsIndefiniteLoan: isIndefiniteLoan,
                    UseFixedTotalDebt: useFixedTotalDebt,
                    FixedTotalDebtFiat: useFixedTotalDebt ? FiatValue.New(dto.FixedTotalDebt!.Value) : FiatValue.Empty,
                    BorrowerOrPlatformName: string.Empty,
                    AmountLentFiat: FiatValue.Empty,
                    LendingAprPercentage: 0,
                    LendingStartDateOffset: null,
                    ExpectedRepaymentDateOffset: null,
                    IsIndefiniteLending: false);

            case AssetTypes.BtcLending:
                DateTime? expectedRepaymentDateOffset = null;
                var isIndefiniteLending = false;
                if (dto.RepaymentDate.HasValue)
                    expectedRepaymentDateOffset = dto.RepaymentDate.Value.ToDateTime(TimeOnly.MinValue);
                else
                    isIndefiniteLending = true;

                return new AssetFormValues(
                    Name: dto.Name,
                    SelectedAssetType: selectedAssetType,
                    SelectedCurrency: dto.CurrencyCode,
                    IncludeInNetWorth: dto.IncludeInNetWorth,
                    Visible: dto.Visible,
                    Symbol: string.Empty,
                    Quantity: 0,
                    CurrentPriceFiat: FiatValue.Empty,
                    SelectedPriceSource: AssetPriceSource.Manual.ToString(),
                    Address: string.Empty,
                    CurrentValueFiat: FiatValue.Empty,
                    MonthlyRentalIncomeFiat: FiatValue.Empty,
                    AcquisitionDate: null,
                    AcquisitionPriceFiat: FiatValue.Empty,
                    IsBitcoinUnderlyingAsset: false,
                    CollateralFiat: FiatValue.Empty,
                    EntryPriceFiat: FiatValue.Empty,
                    Leverage: 1,
                    LiquidationPriceFiat: FiatValue.Empty,
                    IsLong: true,
                    UseExactPosition: false,
                    PositionSize: 0,
                    PlatformName: string.Empty,
                    CollateralSats: 0,
                    LoanAmountFiat: FiatValue.Empty,
                    AprPercentage: 0,
                    InitialLtvPercentage: 0,
                    LiquidationLtvPercentage: 0,
                    MarginCallLtvPercentage: 0,
                    FeesFiat: FiatValue.Empty,
                    LoanStartDate: null,
                    RepaymentDateOffset: null,
                    IsIndefiniteLoan: false,
                    UseFixedTotalDebt: false,
                    FixedTotalDebtFiat: FiatValue.Empty,
                    BorrowerOrPlatformName: dto.BorrowerOrPlatformName ?? string.Empty,
                    AmountLentFiat: FiatValue.New(dto.AmountLent ?? 0),
                    LendingAprPercentage: (dto.Apr ?? 0) * 100m,
                    LendingStartDateOffset: dto.LendingStartDate.HasValue ? dto.LendingStartDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    ExpectedRepaymentDateOffset: expectedRepaymentDateOffset,
                    IsIndefiniteLending: isIndefiniteLending);

            default:
                throw new AssetFormBuildException();
        }
    }
}
