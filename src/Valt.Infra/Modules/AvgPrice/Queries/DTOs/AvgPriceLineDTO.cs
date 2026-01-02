namespace Valt.Infra.Modules.AvgPrice.Queries.DTOs;

public record AvgPriceLineDTO(
    string Id,
    DateOnly Date,
    int DisplayOrder,
    int AvgPriceLineTypeId,
    decimal Quantity,
    decimal Amount,
    string Comment,
    decimal AvgCostOfAcquisition,
    decimal TotalCost,
    decimal TotalQuantity)
{
    public decimal UnitPrice => Quantity != 0 ? Amount / Quantity : 0;
}