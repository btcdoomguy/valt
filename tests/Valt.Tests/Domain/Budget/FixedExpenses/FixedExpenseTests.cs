using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Exceptions;
using Valt.Tests.Builders;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

/// <summary>
/// Tests for the FixedExpense aggregate root.
/// FixedExpense represents a recurring expense (bills, subscriptions, etc.) with fixed or ranged amounts.
/// </summary>
[TestFixture]
public class FixedExpenseTests : DatabaseTest
{
    #region Creation Tests

    [Test]
    public void Should_Create_With_FixedAmount()
    {
        // Arrange & Act: Create a fixed expense with a fixed amount range
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithFixedAmountRange(123.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            .BuildDomainObject();

        // Assert: Fixed expense should be created successfully
        Assert.That(fixedExpense.Name.Value, Is.EqualTo("Bill"));
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Should_Create_With_RangedAmount()
    {
        // Arrange & Act: Create a fixed expense with a ranged amount (min-max)
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithRangedAmountRange(123.45m, 130.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            .BuildDomainObject();

        // Assert: Fixed expense should be created with ranged amount
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Should_Create_With_Currency_Instead_Of_Account()
    {
        // Arrange & Act: Create a fixed expense with currency (no default account)
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithCurrency(FiatCurrency.Brl)
            .WithName("Bill")
            .WithFixedAmountRange(123.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            .BuildDomainObject();

        // Assert: Fixed expense should be created with currency
        Assert.That(fixedExpense.Currency, Is.EqualTo(FiatCurrency.Brl));
        Assert.That(fixedExpense.DefaultAccountId, Is.Null);
    }

    [Test]
    public void Should_Throw_If_Account_And_Currency_Are_Both_Set()
    {
        // Arrange: Try to create with both account and currency (invalid)
        var accountId = new AccountId();

        // Act & Assert: Should throw ArgumentException
        Assert.Throws<ArgumentException>(() =>
            FixedExpense.New("Bill", accountId, new CategoryId(), FiatCurrency.Brl,
                new List<FixedExpenseRange>
                {
                    FixedExpenseRange.CreateRangedAmount(
                        new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                        FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
                }, true));
    }

    #endregion

    #region Range Management Tests

    [Test]
    public void Should_Add_Range_After_Last_Record_Date()
    {
        // Arrange: Create a fixed expense with existing range and last record date
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithRangedAmountRange(123.45m, 130.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            .WithLastFixedExpenseRecordDate(new DateOnly(2025, 1, 10))
            .WithVersion(1)
            .BuildDomainObject();

        // Act: Add a new range starting after the last record date
        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(
            FiatValue.New(123.45m), FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 11), 15));

        // Assert: Should have both ranges
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Should_Throw_If_Range_Created_Before_Or_Equally_To_Latest_Record()
    {
        // Arrange: Create a fixed expense with last record date
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithRangedAmountRange(123.45m, 130.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            .WithLastFixedExpenseRecordDate(new DateOnly(2025, 1, 10))
            .WithVersion(1)
            .BuildDomainObject();

        // Act & Assert: Should throw when adding range on or before last record date
        Assert.Throws<InvalidFixedExpenseRangeException>(() =>
            fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(
                FiatValue.New(123.45m), FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 10), 15)));
    }

    [Test]
    public void Should_Erase_Unused_Range_When_Adding_New_One()
    {
        // Arrange: Create fixed expense with range and last record date
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithRangedAmountRange(123.45m, 130.45m, FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            .WithLastFixedExpenseRecordDate(new DateOnly(2025, 1, 10))
            .WithVersion(1)
            .BuildDomainObject();

        // Act: Add a range, then add another that should override the first unused one
        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(
            FiatValue.New(123.45m), FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 20), 15));

        // This should override the previous one since it wasn't used yet
        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(
            FiatValue.New(123.45m), FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 11), 15));

        // Assert: Should only have 2 ranges (original + replacement)
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Should_Keep_All_Ranges_When_All_Have_Been_Used()
    {
        // Arrange: Create fixed expense with multiple used ranges
        var fixedExpense = FixedExpenseBuilder.AFixedExpenseWithAccount(new AccountId())
            .WithName("Bill")
            .WithRanges(
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10),
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 15), 20))
            .WithLastFixedExpenseRecordDate(new DateOnly(2025, 1, 20))
            .WithVersion(1)
            .BuildDomainObject();

        // Act: Add a new range after all existing ones have been used
        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(
            FiatValue.New(123.45m), FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 30), 15));

        // Assert: All 3 ranges should be preserved
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(3));
    }

    #endregion
}
