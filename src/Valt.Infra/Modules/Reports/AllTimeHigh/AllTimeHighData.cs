using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.AllTimeHigh;

public record AllTimeHighData(DateOnly Date, FiatCurrency Currency, FiatValue Value, decimal DeclineFromAth)
{
    public bool HasAccountsWithoutTransactions { get; init; }
}