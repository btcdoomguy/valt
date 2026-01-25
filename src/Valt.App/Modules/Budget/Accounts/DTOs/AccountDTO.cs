using System.Drawing;

namespace Valt.App.Modules.Budget.Accounts.DTOs;

public record AccountDTO(
    string Id,
    string Type,
    string Name,
    bool Visible,
    string? IconId,
    char Unicode,
    Color Color,
    string? Currency,
    bool IsBtcAccount,
    decimal? InitialAmountFiat,
    long? InitialAmountSats)
{
    public override string ToString() => Name;
}
