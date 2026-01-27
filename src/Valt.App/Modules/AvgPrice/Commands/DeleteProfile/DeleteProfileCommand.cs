using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.DeleteProfile;

public record DeleteProfileCommand : ICommand<DeleteProfileResult>
{
    public required string ProfileId { get; init; }
}

public record DeleteProfileResult;
