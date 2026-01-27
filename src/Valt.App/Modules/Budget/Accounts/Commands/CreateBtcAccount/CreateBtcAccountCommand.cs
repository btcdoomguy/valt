using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;

public record CreateBtcAccountCommand : ICommand<CreateBtcAccountResult>
{
    public required string Name { get; init; }
    public string? CurrencyNickname { get; init; }
    public bool Visible { get; init; } = true;
    public required string IconId { get; init; }
    public long InitialAmountSats { get; init; }
    public string? GroupId { get; init; }
}

public record CreateBtcAccountResult(string AccountId);
