using System.Drawing;

namespace Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

public record AccountDTO(
    string Id,
    string Type,
    string Name,
    bool Visible,
    string? Icon,
    char Unicode,
    Color Color,
    string? Currency,
    bool IsBtcAccount)
{
    public override string ToString() => Name;
}