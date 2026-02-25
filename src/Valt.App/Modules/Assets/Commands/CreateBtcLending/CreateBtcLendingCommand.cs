using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.CreateBtcLending;

/// <summary>
/// Command to create a BTC/fiat lending position asset.
/// </summary>
public record CreateBtcLendingCommand : ICommand<CreateBtcLendingResult>
{
    /// <summary>
    /// Lending position name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Currency code for the lending (e.g., USD, BRL).
    /// </summary>
    public required string CurrencyCode { get; init; }

    /// <summary>
    /// Amount lent.
    /// </summary>
    public required decimal AmountLent { get; init; }

    /// <summary>
    /// Annual percentage rate earned (e.g., 0.05 for 5%).
    /// </summary>
    public required decimal Apr { get; init; }

    /// <summary>
    /// The borrower name or platform name.
    /// </summary>
    public required string BorrowerOrPlatformName { get; init; }

    /// <summary>
    /// When the lending started.
    /// </summary>
    public required DateOnly LendingStartDate { get; init; }

    /// <summary>
    /// Expected repayment date (null = indefinite).
    /// </summary>
    public DateOnly? ExpectedRepaymentDate { get; init; }

    /// <summary>
    /// Include in net worth calculation.
    /// </summary>
    public bool IncludeInNetWorth { get; init; } = true;

    /// <summary>
    /// Visible in list.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// Icon identifier (optional).
    /// </summary>
    public string? Icon { get; init; }
}

public record CreateBtcLendingResult(string AssetId);
