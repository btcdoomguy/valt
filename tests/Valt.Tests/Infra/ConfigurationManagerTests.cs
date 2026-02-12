using Valt.Infra.Modules.Configuration;

namespace Valt.Tests.Infrastructure;

[TestFixture]
public class ConfigurationManagerTests : DatabaseTest
{
    private ConfigurationManager _configurationManager = null!;

    [SetUp]
    public new Task SetUp()
    {
        base.SetUp();
        _configurationManager = new ConfigurationManager(_localDatabase);
        return Task.CompletedTask;
    }

    [Test]
    public void GetExpensesCategoryFilterExcludedIds_ReturnsEmpty_WhenNoConfig()
    {
        var result = _configurationManager.GetExpensesCategoryFilterExcludedIds();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SetAndGetExpensesCategoryFilterExcludedIds_RoundTrips()
    {
        var ids = new[] { "cat-1", "cat-2", "cat-3" };

        _configurationManager.SetExpensesCategoryFilterExcludedIds(ids);
        var result = _configurationManager.GetExpensesCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(ids));
    }

    [Test]
    public void SetExpensesCategoryFilterExcludedIds_DeduplicatesAndTrims()
    {
        var ids = new[] { " cat-1 ", "cat-2", "cat-1", " cat-2 ", "cat-3" };

        _configurationManager.SetExpensesCategoryFilterExcludedIds(ids);
        var result = _configurationManager.GetExpensesCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(new[] { "cat-1", "cat-2", "cat-3" }));
    }

    [Test]
    public void GetIncomeCategoryFilterExcludedIds_ReturnsEmpty_WhenNoConfig()
    {
        var result = _configurationManager.GetIncomeCategoryFilterExcludedIds();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SetAndGetIncomeCategoryFilterExcludedIds_RoundTrips()
    {
        var ids = new[] { "inc-1", "inc-2" };

        _configurationManager.SetIncomeCategoryFilterExcludedIds(ids);
        var result = _configurationManager.GetIncomeCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(ids));
    }

    [Test]
    public void SetExpensesCategoryFilterExcludedIds_OverwritesPrevious()
    {
        _configurationManager.SetExpensesCategoryFilterExcludedIds(new[] { "cat-1", "cat-2" });
        _configurationManager.SetExpensesCategoryFilterExcludedIds(new[] { "cat-3" });

        var result = _configurationManager.GetExpensesCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(new[] { "cat-3" }));
    }
}
