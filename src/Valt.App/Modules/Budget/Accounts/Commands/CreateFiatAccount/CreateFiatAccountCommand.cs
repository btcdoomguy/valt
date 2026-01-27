using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateFiatAccount;

public record CreateFiatAccountCommand : ICommand<CreateFiatAccountResult>
{
    public required string Name { get; init; }
    public string? CurrencyNickname { get; init; }
    public bool Visible { get; init; } = true;
    public required string IconId { get; init; }
    public required string Currency { get; init; }
    public decimal InitialAmount { get; init; }
    public string? GroupId { get; init; }
}

public record CreateFiatAccountResult(string AccountId);
