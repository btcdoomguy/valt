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

    #region Notes Tests

    [Test]
    public void Should_Create_Transaction_With_Notes()
    {
        // Arrange & Act
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .WithNotes("This is a test note")
            .BuildDomainObject();

        // Assert
        Assert.That(transaction.Notes, Is.EqualTo("This is a test note"));
    }

    [Test]
    public void Should_Create_Transaction_Without_Notes()
    {
        // Arrange & Act
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .BuildDomainObject();

        // Assert
        Assert.That(transaction.Notes, Is.Null);
    }

    [Test]
    public void Should_Change_Notes_On_Transaction()
    {
        // Arrange
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .WithNotes("Original note")
            .BuildDomainObject();

        // Act
        transaction.ChangeNotes("Updated note");

        // Assert
        Assert.That(transaction.Notes, Is.EqualTo("Updated note"));
    }

    [Test]
    public void Should_Clear_Notes_When_Setting_To_Null()
    {
        // Arrange
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .WithNotes("Original note")
            .BuildDomainObject();

        // Act
        transaction.ChangeNotes(null);

        // Assert
        Assert.That(transaction.Notes, Is.Null);
    }

    [Test]
    public void Should_Not_Emit_Event_When_Notes_Not_Changed()
    {
        // Arrange
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .WithNotes("Same note")
            .WithVersion(1) // Set version > 0 to avoid creation event
            .BuildDomainObject();

        transaction.ClearEvents(); // Clear any existing events

        // Act
        transaction.ChangeNotes("Same note");

        // Assert
        Assert.That(transaction.Events, Is.Empty);
    }

    #endregion

    #region GroupId Tests

    [Test]
    public void Should_Create_Transaction_With_GroupId_And_IsPartOfGroup_Returns_True()
    {
        // Arrange
        var groupId = new GroupId();

        // Act
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .WithGroupId(groupId)
            .BuildDomainObject();

        // Assert
        Assert.That(transaction.GroupId, Is.EqualTo(groupId));
        Assert.That(transaction.IsPartOfGroup, Is.True);
    }

    [Test]
    public void Should_Create_Transaction_Without_GroupId_And_IsPartOfGroup_Returns_False()
    {
        // Arrange & Act
        var transaction = new TransactionBuilder()
            .WithCategoryId(_categoryId)
            .WithDate(new DateOnly(2023, 1, 1))
            .WithName("My Transaction")
            .WithTransactionDetails(new FiatDetails(_fiatAccountId, 100m, true))
            .BuildDomainObject();

        // Assert
        Assert.That(transaction.GroupId, Is.Null);
        Assert.That(transaction.IsPartOfGroup, Is.False);
    }

    #endregion
}
