using System.Drawing;

namespace Valt.App.Modules.Budget.Accounts.DTOs;

public record AccountSummaryDTO(
    string Id,
    string Type,
    string Name,
    bool Visible,
    string? IconId,
    char Unicode,
    Color Color,
    string? Currency,
    string? CurrencyDisplayName,
    bool IsBtcAccount,
    decimal? FiatTotal,
    long? SatsTotal,
    bool HasFutureTotal,
    decimal? FutureFiatTotal,
    long? FutureSatsTotal,
    string? GroupId,
    string? GroupName);
