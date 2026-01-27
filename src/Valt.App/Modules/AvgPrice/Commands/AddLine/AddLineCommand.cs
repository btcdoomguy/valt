using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.AddLine;

public record AddLineCommand : ICommand<AddLineResult>
{
    public required string ProfileId { get; init; }
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Line type: 0=Buy, 1=Sell, 2=Setup
    /// </summary>
    public required int LineTypeId { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal Amount { get; init; }
    public string? Comment { get; init; }
}

public record AddLineResult(string LineId);
