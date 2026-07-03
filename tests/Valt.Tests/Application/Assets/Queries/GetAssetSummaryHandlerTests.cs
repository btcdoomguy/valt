using Valt.App.Modules.Assets.Queries.GetAssetSummary;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Application.Assets.Queries;

[TestFixture]
public class GetAssetSummaryHandlerTests : DatabaseTest
{
    private GetAssetSummaryHandler _handler = null!;

    private const decimal BtcEntryPrice = 50_000m;
    private const decimal BtcCurrentPrice = 60_000m;
    private const decimal BtcCollateral = 1m;
    private const decimal BtcContractCount = 1_000m;
    private const decimal BtcContractSizeUsd = 100m;

    private const decimal FiatEntryPrice = 50_000m;
    private const decimal FiatCurrentPrice = 55_000m;
    private const decimal FiatCollateral = 1_000m;
    private const decimal FiatLeverage = 10m;

    protected override async Task SeedDatabase()
    {
        var btcCollateralPosition = AssetBuilder.AnAsset()
            .WithName("BTC Collateral Long")
            .WithDetails(new LeveragedPositionDetails(
                collateral: BtcCollateral,
                entryPrice: BtcEntryPrice,
                leverage: 1m,
                liquidationPrice: 25_000m,
                currentPrice: BtcCurrentPrice,
                currencyCode: "USD",
                symbol: "BTC",
                priceSource: AssetPriceSource.Manual,
                isLong: true,
                inputMode: LeveragedPositionInputMode.Collateral,
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: BtcContractCount,
                contractSizeUsd: BtcContractSizeUsd))
            .Build();

        var fiatCollateralPosition = AssetBuilder.AnAsset()
            .WithName("Fiat Collateral Long")
            .WithDetails(new LeveragedPositionDetails(
                collateral: FiatCollateral,
                entryPrice: FiatEntryPrice,
                leverage: FiatLeverage,
                liquidationPrice: 45_000m,
                currentPrice: FiatCurrentPrice,
                currencyCode: "USD",
                symbol: "BTC",
                priceSource: AssetPriceSource.Manual,
                isLong: true,
                inputMode: LeveragedPositionInputMode.Collateral,
                collateralAssetType: LeveragedPositionCollateralAssetType.Fiat))
            .Build();

        await _assetRepository.SaveAsync(btcCollateralPosition);
        await _assetRepository.SaveAsync(fiatCollateralPosition);
    }

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new GetAssetSummaryHandler(_assetQueries);
    }

    [Test]
    public async Task HandleAsync_LongLeveragedPositions_UsePnlOnly()
    {
        var btcDetails = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                BtcCollateral, BtcEntryPrice, 1m, 25_000m, BtcCurrentPrice, "USD",
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: BtcContractCount, contractSizeUsd: BtcContractSizeUsd))
            .Build().Details;

        var fiatDetails = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                FiatCollateral, FiatEntryPrice, FiatLeverage, 45_000m, FiatCurrentPrice, "USD"))
            .Build().Details;

        var expectedBtcPnl = btcDetails.CalculatePnL(BtcCurrentPrice);
        var expectedFiatPnl = fiatDetails.CalculatePnL(FiatCurrentPrice);
        var expectedTotalPnl = expectedBtcPnl + expectedFiatPnl;

        var expectedBtcCurrentValue = btcDetails.CalculateCurrentValue(BtcCurrentPrice);
        var expectedFiatCurrentValue = fiatDetails.CalculateCurrentValue(FiatCurrentPrice);
        var expectedTotalCurrentValue = expectedBtcCurrentValue + expectedFiatCurrentValue;

        var query = new GetAssetSummaryQuery
        {
            MainCurrencyCode = "USD",
            BtcPriceUsd = BtcCurrentPrice,
            FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m }
        };

        var result = await _handler.HandleAsync(query);

        var expectedBtcSats = (long)((expectedBtcPnl / BtcCurrentPrice) * 100_000_000m);
        var expectedFiatSats = (long)((expectedFiatPnl / BtcCurrentPrice) * 100_000_000m);
        var expectedTotalSats = expectedBtcSats + expectedFiatSats;

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalValueInMainCurrency, Is.EqualTo(expectedTotalPnl),
                "Total value should equal the combined PnL, not the combined current value");
            Assert.That(result.TotalValueInMainCurrency, Is.Not.EqualTo(expectedTotalCurrentValue),
                "Total value should not equal the combined current value (which includes BTC collateral)");
            Assert.That(result.TotalAssetsValueInMainCurrency, Is.EqualTo(expectedTotalPnl),
                "Total assets value should equal the combined PnL");
            Assert.That(result.TotalValueInSats, Is.EqualTo(expectedTotalSats),
                "Total sats should sum the per-asset PnL conversions at the BTC price");
        });
    }

    [Test]
    public async Task HandleAsync_LongBtcCollateralPosition_PnLIsLessThanCurrentValue()
    {
        var btcDetails = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                BtcCollateral, BtcEntryPrice, 1m, 25_000m, BtcCurrentPrice, "USD",
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: BtcContractCount, contractSizeUsd: BtcContractSizeUsd))
            .Build().Details;

        var expectedBtcPnl = btcDetails.CalculatePnL(BtcCurrentPrice);
        var expectedBtcCurrentValue = btcDetails.CalculateCurrentValue(BtcCurrentPrice);

        var query = new GetAssetSummaryQuery
        {
            MainCurrencyCode = "USD",
            BtcPriceUsd = BtcCurrentPrice,
            FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m }
        };

        var result = await _handler.HandleAsync(query);

        Assert.That(expectedBtcPnl, Is.LessThan(expectedBtcCurrentValue),
            "Sanity check: the BTC-collateral position's PnL must be less than its current value (which includes collateral)");
        Assert.That(result.TotalValueInMainCurrency, Is.EqualTo(expectedBtcPnl + FiatPnl()),
            "The BTC-collateral position should contribute only its PnL to the total");
    }

    [Test]
    public async Task HandleAsync_LongFiatCollateralPosition_ContinuesToUsePnlOnly()
    {
        var fiatDetails = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                FiatCollateral, FiatEntryPrice, FiatLeverage, 45_000m, FiatCurrentPrice, "USD"))
            .Build().Details;

        var expectedFiatPnl = fiatDetails.CalculatePnL(FiatCurrentPrice);

        var query = new GetAssetSummaryQuery
        {
            MainCurrencyCode = "USD",
            BtcPriceUsd = BtcCurrentPrice,
            FiatRates = new Dictionary<string, decimal> { ["USD"] = 1m }
        };

        var result = await _handler.HandleAsync(query);

        Assert.That(result.TotalValueInMainCurrency, Is.EqualTo(BtcPnl() + expectedFiatPnl),
            "The fiat-collateral position should continue to contribute only its PnL");
    }

    private decimal BtcPnl()
    {
        var details = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                BtcCollateral, BtcEntryPrice, 1m, 25_000m, BtcCurrentPrice, "USD",
                collateralAssetType: LeveragedPositionCollateralAssetType.Btc,
                contractCount: BtcContractCount, contractSizeUsd: BtcContractSizeUsd))
            .Build().Details;
        return details.CalculatePnL(BtcCurrentPrice);
    }

    private decimal FiatPnl()
    {
        var details = (LeveragedPositionDetails)AssetBuilder.AnAsset()
            .WithDetails(new LeveragedPositionDetails(
                FiatCollateral, FiatEntryPrice, FiatLeverage, 45_000m, FiatCurrentPrice, "USD"))
            .Build().Details;
        return details.CalculatePnL(FiatCurrentPrice);
    }
}
