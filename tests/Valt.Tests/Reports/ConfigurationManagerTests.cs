using Valt.Infra.Modules.Configuration;

namespace Valt.Tests.Reports;

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
    public void GetReportsAnalyticsCategoryFilterExcludedIds_MigratesLegacyStatisticsExcludedCategories_WhenNewKeyIsEmpty()
    {
        var legacyIds = new[] { "cat-1", "cat-2", "cat-3" };
        _configurationManager.SetStatisticsExcludedCategoryIds(legacyIds);

        var result = _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(legacyIds));

        var migrated = _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds();
        Assert.That(migrated, Is.EqualTo(legacyIds));
    }

    [Test]
    public void GetReportsAnalyticsCategoryFilterExcludedIds_PrefersNewKeyValue_OverLegacyStatisticsExcludedCategories()
    {
        _configurationManager.SetReportsAnalyticsCategoryFilterExcludedIds(new[] { "new-cat" });
        _configurationManager.SetStatisticsExcludedCategoryIds(new[] { "legacy-cat" });

        var result = _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds();

        Assert.That(result, Is.EqualTo(new[] { "new-cat" }));
    }
}
