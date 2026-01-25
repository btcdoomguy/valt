using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.DeleteLine;

public record DeleteLineCommand : ICommand<DeleteLineResult>
{
    public required string ProfileId { get; init; }
    public required string LineId { get; init; }
}

public record DeleteLineResult;
