using LiteDB;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.Time;

namespace Valt.Tests.Infrastructure.DataAccess;

[TestFixture]
public class LocalDatabaseIndexTests
{
    private MemoryStream _stream = null!;
    private LocalDatabase _database = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _stream = new MemoryStream();
        _database = new LocalDatabase(new Clock());
        _database.OpenInMemoryDatabase(_stream);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _database.Dispose();
        _stream.Dispose();
    }

    private async Task<List<string>> GetIndexExpressionsAsync(string collectionName)
    {
        return await _database.ExecuteWithLockAsync(db =>
            db.GetCollection("$indexes")
              .Find($"collection = '{collectionName}'")
              .Select(d => d["expression"].AsString)
              .ToList());
    }

    [Test]
    public async Task AvgPriceLine_Indexes_Are_Ensured()
    {
        _database.GetAvgPriceLines();
        var indexes = await GetIndexExpressionsAsync("avgprice_line");

        Assert.That(indexes, Does.Contain("$.profId"));
        Assert.That(indexes, Does.Contain("$.dt"));
        Assert.That(indexes, Does.Contain("$.ord"));
    }

    [Test]
    public async Task Accounts_Indexes_Are_Ensured()
    {
        _database.GetAccounts();
        var indexes = await GetIndexExpressionsAsync("budget_accounts");

        Assert.That(indexes, Does.Contain("$.visi"));
        Assert.That(indexes, Does.Contain("$.grp"));
    }

    [Test]
    public async Task TransactionTerms_Indexes_Are_Ensured()
    {
        _database.GetTransactionTerms();
        var indexes = await GetIndexExpressionsAsync("transaction_terms");

        Assert.That(indexes, Does.Contain("$.name"));
        Assert.That(indexes, Does.Contain("$.count"));
        Assert.That(indexes, Does.Contain("$.catId"));
    }

    [Test]
    public async Task FixedExpenseRecords_Indexes_Are_Ensured()
    {
        _database.GetFixedExpenseRecords();
        var indexes = await GetIndexExpressionsAsync("budget_fixedexpenserecords");

        Assert.That(indexes, Does.Contain("$.dt"));
        Assert.That(indexes, Does.Contain("$.Transaction"));
        Assert.That(indexes, Does.Contain("$.FixedExpense"));
    }

    [Test]
    public async Task Transactions_Indexes_Are_Ensured()
    {
        _database.GetTransactions();
        var indexes = await GetIndexExpressionsAsync("budget_transactions");

        Assert.That(indexes, Does.Contain("$.date"));
        Assert.That(indexes, Does.Contain("$.oAccId"));
        Assert.That(indexes, Does.Contain("$.tAccId"));
        Assert.That(indexes, Does.Contain("$.catId"));
        Assert.That(indexes, Does.Contain("$.sState"));
        Assert.That(indexes, Does.Contain("$.type"));
        Assert.That(indexes, Does.Contain("$.gId"));
    }

    [Test]
    public async Task Configuration_Indexes_Are_Ensured()
    {
        _database.GetConfiguration();
        var indexes = await GetIndexExpressionsAsync("system_config");

        Assert.That(indexes, Does.Contain("$.key"));
    }

    [Test]
    public async Task Settings_Indexes_Are_Ensured()
    {
        _database.GetSettings();
        var indexes = await GetIndexExpressionsAsync("system_settings");

        Assert.That(indexes, Does.Contain("$.prop"));
    }

    [Test]
    public async Task Goals_Indexes_Are_Ensured()
    {
        _database.GetGoals();
        var indexes = await GetIndexExpressionsAsync("goals");

        Assert.That(indexes, Does.Contain("$.refDate"));
        Assert.That(indexes, Does.Contain("$.upToDate"));
    }

    [Test]
    public async Task Assets_Indexes_Are_Ensured()
    {
        _database.GetAssets();
        var indexes = await GetIndexExpressionsAsync("assets");

        Assert.That(indexes, Does.Contain("$.visible"));
        Assert.That(indexes, Does.Contain("$.includeInNetWorth"));
        Assert.That(indexes, Does.Contain("$.displayOrder"));
        Assert.That(indexes, Does.Contain("$.groupId"));
    }
}
