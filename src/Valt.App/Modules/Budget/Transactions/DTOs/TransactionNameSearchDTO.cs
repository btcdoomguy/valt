namespace Valt.App.Modules.Budget.Transactions.DTOs;

public record TransactionNameSearchDTO
{
    public string Name { get; init; } = null!;
    public string CategoryId { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public int Count { get; init; }
    public bool IsBitcoin { get; init; }
    public long? SatAmount { get; init; }
    public decimal? FiatAmount { get; init; }
}
