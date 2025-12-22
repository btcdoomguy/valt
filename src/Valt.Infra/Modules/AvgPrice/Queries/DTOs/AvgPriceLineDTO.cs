namespace Valt.Infra.Modules.AvgPrice.Queries.DTOs;

public record AvgPriceLineDTO(string Id,
    DateOnly Date,
    int DisplayOrder,
    int AvgPriceLineTypeId,
    long BtcAmount,
    decimal BtcUnitPrice,
    string Comment,
    decimal AvgCostOfAcquisition,
    decimal TotalCost,
    long TotalBtcAmount);