using System;
using System.Threading.Tasks;
using Valt.App.Modules.Assets.Commands.CreateBasicAsset;
using Valt.App.Modules.Assets.Commands.CreateBtcLending;
using Valt.App.Modules.Assets.Commands.CreateBtcLoan;
using Valt.App.Modules.Assets.Commands.CreateLeveragedPosition;
using Valt.App.Modules.Assets.Commands.CreateRealEstateAsset;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Common;

namespace Valt.UI.Services;

/// <summary>
/// Immutable form-state snapshot used to build asset create/edit payloads.
/// </summary>
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

/// <summary>
/// Values loaded from an <see cref="AssetDTO"/> that the ViewModel applies to its form fields.
/// </summary>
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

/// <summary>
/// UI-layer discriminated envelope for the five asset create commands.
/// </summary>
public abstract record CreateAssetCommandEnvelope;

public sealed record BasicAssetCommandEnvelope(CreateBasicAssetCommand Command) : CreateAssetCommandEnvelope;

public sealed record RealEstateAssetCommandEnvelope(CreateRealEstateAssetCommand Command) : CreateAssetCommandEnvelope;

public sealed record LeveragedPositionCommandEnvelope(CreateLeveragedPositionCommand Command) : CreateAssetCommandEnvelope;

public sealed record BtcLoanCommandEnvelope(CreateBtcLoanCommand Command) : CreateAssetCommandEnvelope;

public sealed record BtcLendingCommandEnvelope(CreateBtcLendingCommand Command) : CreateAssetCommandEnvelope;

/// <summary>
/// Builds asset create/edit commands and DTOs from form state and loads form values from existing assets.
/// </summary>
public interface IAssetFormBuilder
{
    /// <summary>
    /// Builds the appropriate create command envelope from the current form snapshot.
    /// </summary>
    Task<CreateAssetCommandEnvelope> BuildCreateCommandAsync(AssetFormSnapshot snapshot);

    /// <summary>
    /// Builds the appropriate asset details DTO for editing from the current form snapshot.
    /// </summary>
    Task<AssetDetailsInputDTO> BuildEditDetailsAsync(AssetFormSnapshot snapshot);

    /// <summary>
    /// Loads form values from an existing asset DTO for the edit modal.
    /// </summary>
    AssetFormValues LoadFromDto(AssetDTO dto);
}
