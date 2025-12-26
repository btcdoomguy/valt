using System.Drawing;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Infra.Modules.AvgPrice.Queries.DTOs;

public record AvgPriceProfileListDTO(
    string Id,
    string Name,
    string AssetName,
    bool Visible,
    string? Icon,
    char Unicode,
    Color Color,
    string CurrencyCode,
    int AvgPriceCalculationMethodId)
{
    public string DisplayName => $"{AssetName} ({Name})";
}