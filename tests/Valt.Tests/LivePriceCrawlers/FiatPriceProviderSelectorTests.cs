using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Tests.LivePriceCrawlers;

[TestFixture]
public class FiatPriceProviderSelectorTests
{
    private IClock _clock = null!;
    private IFiatPriceProvider _frankfurterProvider = null!;
    private IFiatPriceProvider _currencyApiProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = Substitute.For<IClock>();
        _clock.GetCurrentDateTimeUtc().Returns(DateTime.UtcNow);

        _frankfurterProvider = Substitute.For<IFiatPriceProvider>();
        _frankfurterProvider.Name.Returns("Frankfurter");
        _frankfurterProvider.SupportedCurrencies.Returns(new HashSet<FiatCurrency>
        {
            FiatCurrency.Usd, FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Gbp
        });

        _currencyApiProvider = Substitute.For<IFiatPriceProvider>();
        _currencyApiProvider.Name.Returns("CurrencyApi");
        _currencyApiProvider.SupportedCurrencies.Returns(new HashSet<FiatCurrency>
        {
            FiatCurrency.Usd, FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Gbp,
            FiatCurrency.Uyu, FiatCurrency.Pyg
        });
    }

    #region Currency Assignment Tests

    [Test]
    public void AssignCurrenciesToProviders_Should_Use_Primary_Provider_For_Supported_Currencies()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new List<FiatCurrency> { FiatCurrency.Brl, FiatCurrency.Eur };

        // Act
        var assignments = selector.AssignCurrenciesToProviders(currencies);

        // Assert
        Assert.That(assignments.Count, Is.EqualTo(1));
        Assert.That(assignments[0].Provider.Name, Is.EqualTo("Frankfurter"));
        Assert.That(assignments[0].Currencies, Does.Contain(FiatCurrency.Brl));
        Assert.That(assignments[0].Currencies, Does.Contain(FiatCurrency.Eur));
    }

    [Test]
    public void AssignCurrenciesToProviders_Should_Use_Secondary_Provider_For_Unsupported_Currencies()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new List<FiatCurrency> { FiatCurrency.Uyu, FiatCurrency.Pyg };

        // Act
        var assignments = selector.AssignCurrenciesToProviders(currencies);

        // Assert
        Assert.That(assignments.Count, Is.EqualTo(1));
        Assert.That(assignments[0].Provider.Name, Is.EqualTo("CurrencyApi"));
        Assert.That(assignments[0].Currencies, Does.Contain(FiatCurrency.Uyu));
        Assert.That(assignments[0].Currencies, Does.Contain(FiatCurrency.Pyg));
    }

    [Test]
    public void AssignCurrenciesToProviders_Should_Split_Between_Providers_When_Mixed_Currencies()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new List<FiatCurrency> { FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Uyu };

        // Act
        var assignments = selector.AssignCurrenciesToProviders(currencies);

        // Assert
        Assert.That(assignments.Count, Is.EqualTo(2));

        var frankfurterAssignment = assignments.First(a => a.Provider.Name == "Frankfurter");
        var currencyApiAssignment = assignments.First(a => a.Provider.Name == "CurrencyApi");

        Assert.That(frankfurterAssignment.Currencies, Does.Contain(FiatCurrency.Brl));
        Assert.That(frankfurterAssignment.Currencies, Does.Contain(FiatCurrency.Eur));
        Assert.That(frankfurterAssignment.Currencies, Does.Not.Contain(FiatCurrency.Uyu));

        Assert.That(currencyApiAssignment.Currencies, Does.Contain(FiatCurrency.Uyu));
        Assert.That(currencyApiAssignment.Currencies, Does.Not.Contain(FiatCurrency.Brl));
    }

    [Test]
    public void AssignCurrenciesToProviders_Should_Return_Empty_For_Empty_Input()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new List<FiatCurrency>();

        // Act
        var assignments = selector.AssignCurrenciesToProviders(currencies);

        // Assert
        Assert.That(assignments.Count, Is.EqualTo(0));
    }

    #endregion

    #region GetAsync Tests

    [Test]
    public async Task GetAsync_Should_Return_Usd_When_Empty_Currencies()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = Array.Empty<FiatCurrency>();

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items.Single().Currency, Is.EqualTo(FiatCurrency.Usd));
        Assert.That(result.UpToDate, Is.True);
    }

    [Test]
    public async Task GetAsync_Should_Call_Only_Primary_Provider_When_All_Currencies_Supported()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Eur };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m),
                new FiatUsdPrice.Item(FiatCurrency.Eur, 0.85m)
            }));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        await _frankfurterProvider.Received(1).GetAsync(Arg.Any<IEnumerable<FiatCurrency>>());
        await _currencyApiProvider.DidNotReceive().GetAsync(Arg.Any<IEnumerable<FiatCurrency>>());

        Assert.That(result.Items.Count, Is.EqualTo(3));
        Assert.That(result.UpToDate, Is.True);
    }

    [Test]
    public async Task GetAsync_Should_Call_Both_Providers_In_Parallel_When_Mixed_Currencies()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Uyu };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m)
            }));

        _currencyApiProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Uyu, 39.0m)
            }));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        await _frankfurterProvider.Received(1).GetAsync(Arg.Any<IEnumerable<FiatCurrency>>());
        await _currencyApiProvider.Received(1).GetAsync(Arg.Any<IEnumerable<FiatCurrency>>());

        Assert.That(result.Items.Count, Is.EqualTo(3)); // USD, BRL, UYU
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Brl)?.Price, Is.EqualTo(5.5m));
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Uyu)?.Price, Is.EqualTo(39.0m));
        Assert.That(result.UpToDate, Is.True);
    }

    [Test]
    public async Task GetAsync_Should_Merge_Results_From_Multiple_Providers()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Eur, FiatCurrency.Uyu, FiatCurrency.Pyg };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m),
                new FiatUsdPrice.Item(FiatCurrency.Eur, 0.85m)
            }));

        _currencyApiProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Uyu, 39.0m),
                new FiatUsdPrice.Item(FiatCurrency.Pyg, 7500m)
            }));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(5)); // USD, BRL, EUR, UYU, PYG
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Brl)?.Price, Is.EqualTo(5.5m));
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Eur)?.Price, Is.EqualTo(0.85m));
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Uyu)?.Price, Is.EqualTo(39.0m));
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Pyg)?.Price, Is.EqualTo(7500m));
    }

    [Test]
    public async Task GetAsync_Should_Set_UpToDate_False_When_Any_Provider_Fails()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Brl, FiatCurrency.Uyu };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m)
            }));

        _currencyApiProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns<FiatUsdPrice>(x => throw new ApplicationException("API Error"));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        Assert.That(result.UpToDate, Is.False);
        Assert.That(result.Items.SingleOrDefault(x => x.Currency == FiatCurrency.Brl)?.Price, Is.EqualTo(5.5m));
    }

    [Test]
    public async Task GetAsync_Should_Set_UpToDate_False_When_Provider_Returns_NotUpToDate()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Brl };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, false, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m)
            }));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        Assert.That(result.UpToDate, Is.False);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task GetAsync_Should_Not_Duplicate_Usd_In_Results()
    {
        // Arrange
        var selector = CreateSelector();
        var currencies = new[] { FiatCurrency.Usd, FiatCurrency.Brl };

        _frankfurterProvider.GetAsync(Arg.Any<IEnumerable<FiatCurrency>>())
            .Returns(new FiatUsdPrice(DateTime.UtcNow, true, new[]
            {
                new FiatUsdPrice.Item(FiatCurrency.Usd, 1),
                new FiatUsdPrice.Item(FiatCurrency.Brl, 5.5m)
            }));

        // Act
        var result = await selector.GetAsync(currencies);

        // Assert
        var usdItems = result.Items.Where(x => x.Currency == FiatCurrency.Usd).ToList();
        Assert.That(usdItems.Count, Is.EqualTo(1));
        Assert.That(usdItems[0].Price, Is.EqualTo(1));
    }

    [Test]
    public void AssignCurrenciesToProviders_Should_Not_Duplicate_Currencies_Across_Providers()
    {
        // Arrange: Both providers support BRL
        var selector = CreateSelector();
        var currencies = new List<FiatCurrency> { FiatCurrency.Brl };

        // Act
        var assignments = selector.AssignCurrenciesToProviders(currencies);

        // Assert: Only assigned to primary provider (Frankfurter)
        Assert.That(assignments.Count, Is.EqualTo(1));
        Assert.That(assignments[0].Provider.Name, Is.EqualTo("Frankfurter"));
    }

    #endregion

    private FiatPriceProviderSelector CreateSelector()
    {
        var providers = new List<IFiatPriceProvider> { _frankfurterProvider, _currencyApiProvider };
        return new FiatPriceProviderSelector(providers, _clock, new NullLogger<FiatPriceProviderSelector>());
    }
}
