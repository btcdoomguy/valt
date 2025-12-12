using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.Transactions;

/// <summary>
/// Tests for the Transaction aggregate root.
/// Transaction represents a financial movement in an account, with optional automatic sat amount calculation.
/// </summary>
[TestFixture]
public class TransactionTests : DatabaseTest
{
    private AccountId _fiatAccountId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task SeedDatabase()
    {
        _fiatAccountId = IdGenerator.Generate();
        _categoryId = IdGenerator.Generate();

        var fiatAccount = FiatAccountBuilder.AnAccount()
            .WithId(_fiatAccountId)
            .WithName("Fiat Account")
            .WithFiatCurrency(FiatCurrency.Brl)
            .Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var category = CategoryBuilder.ACategory()
            .WithId(_categoryId)
            .WithName("Income")
            .Build();

        _localDatabase.GetCategories().Insert(category);

        await base.SeedDatabase();
    }

    #region AutoSatAmount Tests

    [Test]
    public void Should_Reset_AutoSatAmount_When_Changing_FiatDetails_Amount()
    {
        // Arrange: Create a transaction with processed auto sat amount
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 153.32m, true))
            .WithAutoSatAmountDetails(new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.ParseSats(123456)))
            .BuildDomainObject();

        // Act: Change the transaction amount
        transaction.ChangeTransactionDetails(new FiatDetails(_fiatAccountId, 200.00m, true));

        // Assert: AutoSatAmount should be reset to Pending since amount changed
        Assert.That(transaction.AutoSatAmountDetails, Is.EqualTo(AutoSatAmountDetails.Pending));
    }

    [Test]
    public void Should_Null_AutoSatAmount_When_Changing_To_BitcoinDetails()
    {
        // Arrange: Create a transaction with fiat details and auto sat amount
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 153.32m, true))
            .WithAutoSatAmountDetails(new AutoSatAmountDetails(true, SatAmountState.Processed, BtcValue.ParseSats(123456)))
            .BuildDomainObject();

        // Act: Change to bitcoin details (auto sat amount is not applicable for BTC)
        transaction.ChangeTransactionDetails(new BitcoinDetails(_fiatAccountId, BtcValue.ParseBitcoin(100000), true));

        // Assert: AutoSatAmount should be null since BTC transactions don't need sat conversion
        Assert.That(transaction.AutoSatAmountDetails, Is.Null);
    }

    #endregion
}
