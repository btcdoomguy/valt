using System;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.Core.Modules.AvgPrice;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.AvgPrice.Models;

public class AvgPriceLineViewModel : ObservableObject
{
    private readonly AvgPriceLineDTO _dto;

    public string Id { get; }
    public DateOnly Date { get; }
    public int DisplayOrder { get; }
    public int AvgPriceLineTypeId { get; }
    public string TypeName { get; }
    public string Quantity { get; }
    public string UnitPrice { get; }
    public string Amount { get; }
    public string Comment { get; }
    public string AvgCostOfAcquisition { get; }
    public string TotalCost { get; }
    public string TotalQuantity { get; }
    public SolidColorBrush TypeColor { get; }

    public AvgPriceLineViewModel(AvgPriceLineDTO dto, int assetPrecision, string cultureName)
    {
        _dto = dto;

        Id = dto.Id;
        Date = dto.Date;
        DisplayOrder = dto.DisplayOrder;
        AvgPriceLineTypeId = dto.AvgPriceLineTypeId;

        var culture = new CultureInfo(cultureName);
        var quantityFormat = $"N{assetPrecision}";

        Quantity = dto.Quantity.ToString(quantityFormat, culture);
        UnitPrice = dto.UnitPrice.ToString("N2", culture);
        Amount = dto.Amount.ToString("N2", culture);
        Comment = dto.Comment;
        AvgCostOfAcquisition = dto.AvgCostOfAcquisition.ToString("N2", culture);
        TotalCost = dto.TotalCost.ToString("N2", culture);
        TotalQuantity = dto.TotalQuantity.ToString(quantityFormat, culture);

        var lineType = (AvgPriceLineTypes)dto.AvgPriceLineTypeId;
        TypeName = GetTypeName(lineType);
        TypeColor = GetTypeColor(lineType);
    }

    public AvgPriceLineDTO ToDto() => _dto;

    private static string GetTypeName(AvgPriceLineTypes lineType)
    {
        return lineType switch
        {
            AvgPriceLineTypes.Buy => Lang.language.AvgPrice_LineType_Buy,
            AvgPriceLineTypes.Sell => Lang.language.AvgPrice_LineType_Sell,
            AvgPriceLineTypes.Setup => Lang.language.AvgPrice_LineType_Setup,
            _ => string.Empty
        };
    }

    private static SolidColorBrush GetTypeColor(AvgPriceLineTypes lineType)
    {
        return lineType switch
        {
            AvgPriceLineTypes.Buy => TransactionGridResources.Credit,
            AvgPriceLineTypes.Sell => TransactionGridResources.Debt,
            AvgPriceLineTypes.Setup => TransactionGridResources.Transfer,
            _ => TransactionGridResources.RegularLine
        };
    }
}
