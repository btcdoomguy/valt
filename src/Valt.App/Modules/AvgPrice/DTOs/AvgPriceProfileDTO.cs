using System.Drawing;

namespace Valt.App.Modules.AvgPrice.DTOs;

public record AvgPriceProfileDTO(
    string Id,
    string Name,
    string AssetName,
    int Precision,
    bool Visible,
    string? Icon,
    char Unicode,
    Color Color,
    string CurrencyCode,
    int AvgPriceCalculationMethodId)
{
    public string DisplayName => $"{AssetName} ({Name})";
}

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
