using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.DTOs;

namespace Valt.App.Modules.SpendingEvolution.Queries;

public record GetSpendingEvolutionQuery : IQuery<SpendingEvolutionDataDto>
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string[] CategoryIds { get; init; } = Array.Empty<string>();
    public bool ShowHiddenAccounts { get; init; } = false;
}
