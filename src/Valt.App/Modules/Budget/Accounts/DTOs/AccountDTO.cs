using System.Drawing;

namespace Valt.App.Modules.Budget.Accounts.DTOs;

public record AccountDTO(
    string Id,
    string Type,
    string Name,
    string CurrencyNickname,
    bool Visible,
    string? IconId,
    char Unicode,
    Color Color,
    string? Currency,
    bool IsBtcAccount,
    decimal? InitialAmountFiat,
    long? InitialAmountSats,
    string? GroupId)
{
    public override string ToString() => Name;
}
