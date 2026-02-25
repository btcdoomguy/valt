using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLoan;

/// <summary>
/// Command to create a BTC-collateralized loan asset.
/// </summary>
public record CreateBtcLoanCommand : ICommand<CreateBtcLoanResult>
{
    /// <summary>
    /// Loan name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Currency code for the loan (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Lending platform name (e.g., "HodlHodl", "Ledn").
    /// </summary>
    public required string PlatformName { get; init; }

    /// <summary>
    /// BTC collateral in satoshis.
    /// </summary>
    public required long CollateralSats { get; init; }

    /// <summary>
    /// Borrowed fiat amount.
    /// </summary>
    public required decimal LoanAmount { get; init; }

    /// <summary>
    /// Annual percentage rate (e.g., 0.12 for 12%).
    /// </summary>
    public required decimal Apr { get; init; }

    /// <summary>
    /// Initial LTV ratio at loan origination (percentage, e.g., 50).
    /// </summary>
    public required decimal InitialLtv { get; init; }

    /// <summary>
    /// LTV ratio that triggers liquidation (percentage, e.g., 80).
    /// </summary>
    public required decimal LiquidationLtv { get; init; }

    /// <summary>
    /// LTV ratio that triggers margin call warning (percentage, e.g., 70).
    /// </summary>
    public required decimal MarginCallLtv { get; init; }

    /// <summary>
    /// Fees paid for the loan.
    /// </summary>
    public decimal Fees { get; init; }

    /// <summary>
    /// When the loan started.
    /// </summary>
    public required DateOnly LoanStartDate { get; init; }

    /// <summary>
    /// When the loan is due for repayment (null = indefinite).
    /// </summary>
    public DateOnly? RepaymentDate { get; init; }

    /// <summary>
    /// Include in net worth calculation.
    /// </summary>
    public bool IncludeInNetWorth { get; init; } = true;

    /// <summary>
    /// Visible in list.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// Current BTC price in the loan currency (for initial LTV calculation).
    /// </summary>
    public decimal CurrentBtcPrice { get; init; }

    /// <summary>
    /// Icon identifier (optional).
    /// </summary>
    public string? Icon { get; init; }
}

public record CreateBtcLoanResult(string AssetId);
