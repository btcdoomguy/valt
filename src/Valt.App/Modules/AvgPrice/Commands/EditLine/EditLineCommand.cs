using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.EditLine;

public record EditLineCommand : ICommand<EditLineResult>
{
    public required string ProfileId { get; init; }
    public required string LineId { get; init; }
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Line type: 0=Buy, 1=Sell, 2=Setup
    /// </summary>
    public required int LineTypeId { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal Amount { get; init; }
    public string? Comment { get; init; }
}

public record EditLineResult;
