using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.MoveLine;

public record MoveLineCommand : ICommand<MoveLineResult>
{
    public required string ProfileId { get; init; }
    public required string LineId { get; init; }

    /// <summary>
    /// Direction: 0=Up, 1=Down
    /// </summary>
    public required int Direction { get; init; }
}

public record MoveLineResult;
