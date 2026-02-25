namespace Valt.Infra.Modules.Reports.MaxBtcStack;

public record MaxBtcStackData(DateOnly Date, long MaxStackInSats, long CurrentStackInSats)
{
    public decimal DeclineFromMaxPercent =>
        MaxStackInSats == 0
            ? 0
            : Math.Round((Math.Round((decimal)CurrentStackInSats / MaxStackInSats - 1, 4) * 100), 2);

    public bool HasAccountsWithoutTransactions { get; init; }
}
