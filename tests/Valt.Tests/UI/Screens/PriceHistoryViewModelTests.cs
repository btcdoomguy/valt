using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using LiteDB;
using NSubstitute;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.DataSources.Bitcoin;
using Valt.Infra.Modules.DataSources.Fiat;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Modals.PriceHistory;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class PriceHistoryViewModelTests
{
    private IPriceDatabase _priceDatabase = null!;
    private ILiteCollection<BitcoinDataEntity> _btcCollection = null!;
    private ILiteCollection<FiatDataEntity> _fiatCollection = null!;
    private IConfigurationManager _configManager = null!;
    private PriceHistoryViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _priceDatabase = Substitute.For<IPriceDatabase>();
        _btcCollection = Substitute.For<ILiteCollection<BitcoinDataEntity>>();
        _fiatCollection = Substitute.For<ILiteCollection<FiatDataEntity>>();
        _configManager = Substitute.For<IConfigurationManager>();

        _priceDatabase.GetBitcoinData().Returns(_btcCollection);
        _priceDatabase.GetFiatData().Returns(_fiatCollection);
        _configManager.GetAvailableFiatCurrencies().Returns(new List<string> { "USD", "BRL" });

        var btcRecords = new List<BitcoinDataEntity>
        {
            new() { Id = ObjectId.NewObjectId(), Date = new DateTime(2010, 7, 17), Price = 0.05m },
            new() { Id = ObjectId.NewObjectId(), Date = new DateTime(2012, 11, 28), Price = 12.35m },
            new() { Id = ObjectId.NewObjectId(), Date = new DateTime(2016, 7, 9), Price = 650.63m },
            new() { Id = ObjectId.NewObjectId(), Date = new DateTime(2020, 5, 11), Price = 8601.80m },
            new() { Id = ObjectId.NewObjectId(), Date = new DateTime(2024, 4, 20), Price = 63850.00m },
            new() { Id = ObjectId.NewObjectId(), Date = DateTime.Today.AddDays(-1), Price = 95000.00m }
        };
        _btcCollection.FindAll().Returns(btcRecords);

        var usdRecords = new List<FiatDataEntity>
        {
            new() { Id = ObjectId.NewObjectId(), Currency = "USD", Date = DateTime.Today.AddDays(-2), Price = 1.00m },
            new() { Id = ObjectId.NewObjectId(), Currency = "USD", Date = DateTime.Today.AddDays(-1), Price = 1.00m }
        };
        _fiatCollection.Find(Arg.Any<Expression<Func<FiatDataEntity, bool>>>()).Returns(usdRecords);

        _viewModel = new PriceHistoryViewModel(_priceDatabase, _configManager);
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel?.Dispose();
        _priceDatabase?.Dispose();
    }

    [Test]
    public void Initialize_ShouldSelectBtc_DefaultToLineChartMode_AndPopulateFullHistorySpiralPoints()
    {
        _viewModel.Initialize();

        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.SelectedCurrency, Is.EqualTo("BTC"));
            Assert.That(_viewModel.IsBtcSelected, Is.True);
            Assert.That(_viewModel.IsSpiralChartMode, Is.False);
            Assert.That(_viewModel.HasData, Is.True);

            // SpiralPoints includes full history from 2010 through present (6 records),
            // whereas ChartData.Values is truncated to the last 5 years (2 records: 2024 and yesterday)
            Assert.That(_viewModel.SpiralPoints, Has.Count.EqualTo(6));
            Assert.That(_viewModel.SpiralPoints[0].Date, Is.EqualTo(new DateTime(2010, 7, 17)));
            Assert.That(_viewModel.SpiralPoints[1].HalvingNumber, Is.EqualTo(1));
            Assert.That(_viewModel.SpiralPoints[2].HalvingNumber, Is.EqualTo(2));
            Assert.That(_viewModel.SpiralPoints[3].HalvingNumber, Is.EqualTo(3));
            Assert.That(_viewModel.SpiralPoints[4].HalvingNumber, Is.EqualTo(4));
            Assert.That(_viewModel.ChartData.Values.Count, Is.LessThan(_viewModel.SpiralPoints.Count));
        });
    }

    [Test]
    public void ModeToggleCommands_ShouldSwitchBetweenLineAndSpiralForBtc_AndResetWhenFiatSelected()
    {
        _viewModel.Initialize();

        _viewModel.SetSpiralChartModeCommand.Execute(null);
        Assert.That(_viewModel.IsSpiralChartMode, Is.True);

        _viewModel.SetLineChartModeCommand.Execute(null);
        Assert.That(_viewModel.IsSpiralChartMode, Is.False);

        // Re-enable spiral mode, then switch to fiat currency USD
        _viewModel.SetSpiralChartModeCommand.Execute(null);
        Assert.That(_viewModel.IsSpiralChartMode, Is.True);

        _viewModel.SelectedCurrency = "USD";

        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.IsBtcSelected, Is.False);
            Assert.That(_viewModel.IsSpiralChartMode, Is.False);
            Assert.That(_viewModel.SpiralPoints, Is.Empty);
        });

        // Attempting to enable spiral mode while fiat is selected must be ignored
        _viewModel.SetSpiralChartModeCommand.Execute(null);
        Assert.That(_viewModel.IsSpiralChartMode, Is.False);
    }

    [TestCase("en-US")]
    [TestCase("pt-BR")]
    [TestCase("es")]
    public void Localization_AllNewSpiralAndModeKeys_ShouldResolveNonEmptyInAllThreeLanguages(string cultureName)
    {
        var previousCulture = language.Culture;
        try
        {
            language.Culture = new CultureInfo(cultureName);

            Assert.Multiple(() =>
            {
                Assert.That(language.PriceHistory_Mode_LineChart, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Mode_SpiralChart, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_CycleInfo, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_HalvingMilestone, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_HalvingLegend, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_LatestPriceLegend, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_CycleTopLegend, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_CycleBottomLegend, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_CycleTopMilestone, Is.Not.Null.And.Not.Empty);
                Assert.That(language.PriceHistory_Spiral_CycleBottomMilestone, Is.Not.Null.And.Not.Empty);
            });
        }
        finally
        {
            language.Culture = previousCulture;
        }
    }
}
