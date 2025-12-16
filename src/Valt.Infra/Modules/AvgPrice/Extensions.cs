using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;

namespace Valt.Infra.Modules.AvgPrice;

public static class Extensions
{
    public static AvgPriceProfileEntity AsEntity(this AvgPriceProfile profile)
    {
        return new AvgPriceProfileEntity()
        {
            Id = new ObjectId(profile.Id),
            Name = profile.Name,
            Visible = profile.Visible,
            AvgPriceCalculationMethod = profile.CalculationMethod,
            Currency = profile.Currency.Code,
            Version = profile.Version,
            Icon = profile.Icon.ToString()
        };
    }

    public static AvgPriceLineEntity AsEntity(this AvgPriceLine line, AvgPriceProfileId avgPriceProfileId)
    {
        return new AvgPriceLineEntity()
        {
            Id = new ObjectId(line.Id),
            AvgCostOfAcquisition = line.Totals.AvgCostOfAcquisition.Value,
            BtcAmount = line.BtcAmount.Sats,
            Comment = line.Comment,
            Date = line.Date.ToValtDateTime(),
            DisplayOrder = line.DisplayOrder,
            TotalCost = line.Totals.TotalCost,
            AvgPriceLineType = line.Type,
            BtcUnitPrice = line.BitcoinUnitPrice.Value,
            ProfileId = new ObjectId(avgPriceProfileId),
            TotalBtcAmount = line.Totals.BtcAmount.Sats
        };
    }

    public static AvgPriceProfile AsDomainObject(this AvgPriceProfileEntity entity,
        IEnumerable<AvgPriceLineEntity> lines)
    {
        return AvgPriceProfile.Create(entity.Id.ToString(),
            entity.Name,
            entity.Visible,
            entity.Icon is not null ? Icon.RestoreFromId(entity.Icon) : Icon.Empty,
            FiatCurrency.GetFromCode(entity.Currency!),
            entity.AvgPriceCalculationMethod,
            lines.Select(line => AvgPriceLine.Create(line.Id.ToString(),
                DateOnly.FromDateTime(line.Date),
                line.DisplayOrder,
                line.AvgPriceLineType,
                BtcValue.ParseSats(line.BtcAmount),
                line.BtcUnitPrice,
                line.Comment,
                new LineTotals(line.AvgCostOfAcquisition, line.TotalCost,
                    BtcValue.ParseSats(line.TotalBtcAmount)))),
            entity.Version
        );
    }
}