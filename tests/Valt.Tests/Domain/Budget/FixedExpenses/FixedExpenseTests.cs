using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Exceptions;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

[TestFixture]
public class FixedExpenseTests : DatabaseTest
{
    [Test]
    public void Should_Create_With_FixedAmount()
    {
        _ = FixedExpense.New("Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            },
            true);

        Assert.Pass();
    }

    [Test]
    public void Should_Create_With_RangedAmount()
    {
        _ = FixedExpense.New("Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            },
            true);
    }

    [Test]
    public void Should_Create_With_Currency()
    {
        _ = FixedExpense.New("Bill", null, new CategoryId(), FiatCurrency.Brl,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            }, true);

        Assert.Pass();
    }

    [Test]
    public void Should_Throw_IfAccountAndCurrencyAreSet()
    {
        Assert.Throws<ArgumentException>(() => FixedExpense.New("Bill", new AccountId(), new CategoryId(),
            FiatCurrency.Brl, new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 12, 30), 10)
            }, true));
    }

    [Test]
    public void Should_Add_Range()
    {
        var fixedExpense = FixedExpense.Create(new FixedExpenseId(), "Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            },
            lastFixedExpenseRecordDate: new DateOnly(2025, 1, 10),
            true, 1);

        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
            FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 11), 15));
        
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Should_Throw_IfRangeCreatedBeforeOrEquallyLatestFixedExpenseRecord()
    {
        var fixedExpense = FixedExpense.Create(new FixedExpenseId(), "Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            },
            lastFixedExpenseRecordDate: new DateOnly(2025, 1, 10),
            true, 1);

        Assert.Throws<InvalidFixedExpenseRangeException>(() =>
            fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
                FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 10), 15)));
    }
    
    [Test]
    public void Should_Erase_Unused_Range()
    {
        var fixedExpense = FixedExpense.Create(new FixedExpenseId(), "Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10)
            },
            lastFixedExpenseRecordDate: new DateOnly(2025, 1, 10),
            true, 1);

        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
            FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 20), 15));
        
        //should override the previous one since it wasn't used
        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
            FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 11), 15));
        
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(2));
    }
    
      
    [Test]
    public void Should_Keep_All_Ranges()
    {
        var fixedExpense = FixedExpense.Create(new FixedExpenseId(), "Bill", new AccountId(), new CategoryId(), null,
            new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10),
                FixedExpenseRange.CreateRangedAmount(
                    new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 15), 20)
            },
            lastFixedExpenseRecordDate: new DateOnly(2025, 1, 20),
            true, 1);

        fixedExpense.AddRange(FixedExpenseRange.CreateFixedAmount(FiatValue.New(123.45m),
            FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 30), 15));
        
        Assert.That(fixedExpense.Ranges.Count(), Is.EqualTo(3));
    }
}