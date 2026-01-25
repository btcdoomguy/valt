using Valt.App.Kernel;
using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.Budget.Accounts.Commands.EditAccount;

public record EditAccountCommand : ICommand<Unit>
{
    public required string AccountId { get; init; }
    public required string Name { get; init; }
    public string? CurrencyNickname { get; init; }
    public bool Visible { get; init; }
    public required string IconId { get; init; }
    public string? GroupId { get; init; }

    // Type-specific fields
    public string? Currency { get; init; }  // For Fiat accounts
    public decimal? InitialAmountFiat { get; init; }  // For Fiat accounts
    public long? InitialAmountSats { get; init; }  // For Bitcoin accounts
}
