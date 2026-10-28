using System.Reflection;
using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions.Repository;

/// <summary>
/// Verifies that transactions persisted by older app versions — LiteDB documents WITHOUT the
/// "excl" BSON field — still load with IsExcluded == false (LiteDB deserializes missing fields
/// to the property default). No migration script is required for the additive exclusion flag.
/// </summary>
[TestFixture]
public class TransactionRepository_LegacyDocumentTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private AccountId _btcAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _btcAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount = new FiatAccountBuilder()
        {
            Id = _fiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var btcAccount = new BtcAccountBuilder()
        {
            Id = _btcAccountId,
            Name = "Btc Account",
            Value = 1
        }.Build();

        _localDatabase.GetAccounts().Insert(btcAccount);

        var category = new CategoryBuilder()
            .WithId(_categoryId)
            .WithName("Income")
            .WithIcon(Icon.Empty)
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    [Test]
    public async Task Legacy_Fiat_Document_Without_Excl_Field_Should_Load_With_IsExcluded_False()
    {
        var entity = InsertLegacyDocument(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "Legacy Fiat Transaction",
            TransactionDetails = new FiatDetails(_fiatAccountId, 153.32m, true),
            AutoSatAmountDetails = AutoSatAmountDetails.Pending
        }.Build());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(entity.Id.ToString()));

        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction, Is.Not.Null);
            Assert.That(restoredTransaction!.IsExcluded, Is.False);
        });
    }

    [Test]
    public async Task Legacy_Bitcoin_Document_Without_Excl_Field_Should_Load_With_IsExcluded_False()
    {
        var entity = InsertLegacyDocument(new TransactionBuilder()
        {
            CategoryId = _categoryId,
            Date = new DateOnly(2023, 1, 1),
            Name = "Legacy Bitcoin Transaction",
            TransactionDetails = new BitcoinDetails(_btcAccountId, 200000, true)
        }.Build());

        var restoredTransaction = await _transactionRepository.GetTransactionByIdAsync(
            new TransactionId(entity.Id.ToString()));

        Assert.Multiple(() =>
        {
            Assert.That(restoredTransaction, Is.Not.Null);
            Assert.That(restoredTransaction!.IsExcluded, Is.False);
        });
    }

    /// <summary>
    /// Inserts a transaction document directly into the raw LiteDB collection with the "excl"
    /// field stripped, simulating a document written before the exclusion flag existed.
    /// Uses the app's own BsonMapper so field names match production serialization.
    /// </summary>
    private Valt.Infra.Modules.Budget.Transactions.TransactionEntity InsertLegacyDocument(
        Valt.Infra.Modules.Budget.Transactions.TransactionEntity entity)
    {
        var database = GetUnderlyingDatabase();

        var document = database.Mapper.ToDocument(entity);
        document.Remove("excl");

        database.GetCollection("budget_transactions").Insert(document);

        return entity;
    }

    private LiteDatabase GetUnderlyingDatabase()
    {
        var field = typeof(Valt.Infra.DataAccess.LocalDatabase).GetField("_database",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (LiteDatabase)field!.GetValue(_localDatabase)!;
    }
}
