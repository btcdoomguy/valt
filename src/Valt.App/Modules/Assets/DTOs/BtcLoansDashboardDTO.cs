namespace Valt.App.Modules.Assets.DTOs;

public record BtcLoansDashboardDTO
{
    public required bool HasActiveLoans { get; init; }
    public required int ActiveLoansCount { get; init; }

    // Core
    public required decimal CollateralPercentOfStack { get; init; }
    public required decimal DebtWeightedAvgLtv { get; init; }
    public required decimal DebtWeightedAvgApr { get; init; }
    public required decimal TotalDebtInMainCurrency { get; init; }

    // Risk
    public required decimal HighestLtv { get; init; }
    public required decimal ClosestDistanceToLiquidationLtv { get; init; }
    public required string ClosestLoanName { get; init; }
    public required decimal WorstCaseLiquidationBtcPriceUsd { get; init; }
    public required int HealthyCount { get; init; }
    public required int WarningCount { get; init; }
    public required int DangerCount { get; init; }

    // Collateral
    public required long TotalCollateralSats { get; init; }
    public required decimal TotalCollateralFiatInMainCurrency { get; init; }
    public required long FreeBtcSats { get; init; }
    public required long TotalBtcStackSats { get; init; }

    // Financial
    public required decimal TotalAccruedInterestInMainCurrency { get; init; }
    public required decimal TotalFeesPaidInMainCurrency { get; init; }

    // Time
    public required DateOnly? NextRepaymentDate { get; init; }
    public required int? DaysUntilNextRepayment { get; init; }
    public required string? NextRepaymentLoanName { get; init; }
    public required decimal AverageLoanAgeDays { get; init; }

    public static BtcLoansDashboardDTO Empty(long totalBtcStackSats) => new()
    {
        HasActiveLoans = false,
        ActiveLoansCount = 0,
        CollateralPercentOfStack = 0,
        DebtWeightedAvgLtv = 0,
        DebtWeightedAvgApr = 0,
        TotalDebtInMainCurrency = 0,
        HighestLtv = 0,
        ClosestDistanceToLiquidationLtv = 0,
        ClosestLoanName = string.Empty,
        WorstCaseLiquidationBtcPriceUsd = 0,
        HealthyCount = 0,
        WarningCount = 0,
        DangerCount = 0,
        TotalCollateralSats = 0,
        TotalCollateralFiatInMainCurrency = 0,
        FreeBtcSats = totalBtcStackSats,
        TotalBtcStackSats = totalBtcStackSats,
        TotalAccruedInterestInMainCurrency = 0,
        TotalFeesPaidInMainCurrency = 0,
        NextRepaymentDate = null,
        DaysUntilNextRepayment = null,
        NextRepaymentLoanName = null,
        AverageLoanAgeDays = 0
    };
}
