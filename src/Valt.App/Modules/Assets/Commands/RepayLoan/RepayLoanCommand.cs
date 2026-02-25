using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Assets.Commands.RepayLoan;

/// <summary>
/// Command to mark a BTC loan or lending position as repaid.
/// </summary>
public record RepayLoanCommand : ICommand<Unit>
{
    /// <summary>
    /// The asset ID to mark as repaid.
    /// </summary>
    public required string AssetId { get; init; }
}
