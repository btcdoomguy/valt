using System.Drawing;

namespace Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

public record AccountSummaryDTO(
    string Id,
    string Type,
    string Name,
    bool Visible,
    string? Icon,
    char Unicode,
    Color Color,
    string? Currency,
    bool IsBtcAccount,
    decimal? FiatTotal,
    long? SatsTotal,
    bool HasFutureTotal,
    decimal? FutureFiatTotal,
    long? FutureSatsTotal);