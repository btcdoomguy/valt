using System.Drawing;

namespace Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles.Models;

public record AveragePriceProfileItem(
    string Id,
    string Name,
    string AssetName,
    char Unicode,
    Color Color)
{
    public string DisplayName => $"{AssetName} ({Name})";
}
