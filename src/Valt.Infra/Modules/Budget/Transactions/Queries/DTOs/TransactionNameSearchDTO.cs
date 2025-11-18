namespace Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

public record TransactionNameSearchDTO
{
    public string Name { get; set; } = null!;
    public string CategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public int Count { get; set; }
    public bool IsBitcoin { get; set; }
    public long? SatAmount { get; set; }
    public decimal? FiatAmount { get; set; }
}