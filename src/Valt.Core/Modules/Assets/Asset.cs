using Valt.Core.Common;
using Valt.Core.Kernel;
using Valt.Core.Modules.Assets.Events;

namespace Valt.Core.Modules.Assets;

public sealed class Asset : AggregateRoot<AssetId>
{
    public AssetName Name { get; private set; }
    public IAssetDetails Details { get; private set; }
    public Icon Icon { get; private set; }
    public bool IncludeInNetWorth { get; private set; }
    public bool Visible { get; private set; }
    public DateTime LastPriceUpdateAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int DisplayOrder { get; private set; }

    private Asset(
        AssetId id,
        AssetName name,
        IAssetDetails details,
        Icon icon,
        bool includeInNetWorth,
        bool visible,
        DateTime lastPriceUpdateAt,
        DateTime createdAt,
        int displayOrder,
        int version)
    {
        Id = id;
        Name = name;
        Details = details;
        Icon = icon;
        IncludeInNetWorth = includeInNetWorth;
        Visible = visible;
        LastPriceUpdateAt = lastPriceUpdateAt;
        CreatedAt = createdAt;
        DisplayOrder = displayOrder;
        Version = version;
    }

    public static Asset Create(
        AssetId id,
        AssetName name,
        IAssetDetails details,
        Icon icon,
        bool includeInNetWorth,
        bool visible,
        DateTime lastPriceUpdateAt,
        DateTime createdAt,
        int displayOrder,
        int version)
    {
        return new Asset(id, name, details, icon, includeInNetWorth, visible, lastPriceUpdateAt, createdAt, displayOrder, version);
    }

    public static Asset New(
        AssetName name,
        IAssetDetails details,
        Icon icon,
        bool includeInNetWorth = true,
        bool visible = true,
        int displayOrder = 0)
    {
        var now = DateTime.UtcNow;
        var asset = new Asset(
            new AssetId(),
            name,
            details,
            icon,
            includeInNetWorth,
            visible,
            now,
            now,
            displayOrder,
            0);

        asset.AddEvent(new AssetCreatedEvent(asset));
        return asset;
    }

    public void Edit(
        AssetName name,
        IAssetDetails details,
        Icon icon,
        bool includeInNetWorth,
        bool visible)
    {
        Name = name;
        Details = details;
        Icon = icon;
        IncludeInNetWorth = includeInNetWorth;
        Visible = visible;

        AddEvent(new AssetUpdatedEvent(this));
    }

    public void UpdatePrice(decimal newPrice)
    {
        var oldPrice = GetCurrentPrice();
        if (oldPrice == newPrice)
            return;

        Details = Details.WithUpdatedPrice(newPrice);
        LastPriceUpdateAt = DateTime.UtcNow;

        AddEvent(new AssetPriceUpdatedEvent(this, oldPrice, newPrice));
    }

    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        AddEvent(new AssetUpdatedEvent(this));
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;
        AddEvent(new AssetUpdatedEvent(this));
    }

    public void SetIncludeInNetWorth(bool include)
    {
        IncludeInNetWorth = include;
        AddEvent(new AssetUpdatedEvent(this));
    }

    public decimal GetCurrentPrice() => Details switch
    {
        Details.BasicAssetDetails basic => basic.CurrentPrice,
        Details.RealEstateAssetDetails realEstate => realEstate.CurrentValue,
        Details.LeveragedPositionDetails leveraged => leveraged.CurrentPrice,
        _ => 0
    };

    public decimal GetCurrentValue() => Details.CalculateCurrentValue(GetCurrentPrice());

    public string GetCurrencyCode() => Details switch
    {
        Details.BasicAssetDetails basic => basic.CurrencyCode,
        Details.RealEstateAssetDetails realEstate => realEstate.CurrencyCode,
        Details.LeveragedPositionDetails leveraged => leveraged.CurrencyCode,
        _ => "USD"
    };
}
