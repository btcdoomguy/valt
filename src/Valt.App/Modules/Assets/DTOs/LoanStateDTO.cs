namespace Valt.App.Modules.Assets.DTOs;

/// <summary>
/// DTO representing the latest recorded state of a BTC loan, including asset metadata.
/// </summary>
public record LoanStateDTO
{
    public required string AssetId { get; init; }
    public required string AssetName { get; init; }
    public required string PlatformName { get; init; }
    public required long CollateralSats { get; init; }
    public required decimal LoanAmount { get; init; }
    public required string CurrencyCode { get; init; }
    public required decimal Apr { get; init; }
    public required decimal InitialLtv { get; init; }
    public required decimal LiquidationLtv { get; init; }
    public required decimal MarginCallLtv { get; init; }
    public required decimal Fees { get; init; }
    public required DateOnly LoanStartDate { get; init; }
    public required DateOnly? RepaymentDate { get; init; }
    public required int StatusId { get; init; }
    public required decimal CurrentBtcPriceInLoanCurrency { get; init; }
    public required decimal? FixedTotalDebt { get; init; }
    public required decimal CurrentTotalDebt { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required string? Note { get; init; }
}
