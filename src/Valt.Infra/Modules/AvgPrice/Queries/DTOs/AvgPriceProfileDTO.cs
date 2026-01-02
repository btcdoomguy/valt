using System.Drawing;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Infra.Modules.AvgPrice.Queries.DTOs;

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