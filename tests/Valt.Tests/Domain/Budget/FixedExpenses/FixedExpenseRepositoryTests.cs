using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Events;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

[TestFixture]
public class FixedExpenseRepositoryTests : DatabaseTest
{
    [Test]
    public async Task Save_Should_Store_And_Retrieve()
    {
        var fixedExpense = FixedExpense.New("Bill", null, new CategoryId(), FiatCurrency.Brl, new List<FixedExpenseRange>()
        {
            FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10),
            FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
                FixedExpensePeriods.Monthly, new DateOnly(2025, 2, 1), 15),
            FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(123.45m), FiatValue.New(130.45m)),
            FixedExpensePeriods.Monthly, new DateOnly(2025, 3, 1), 20)
        }, true);

        var repository = new FixedExpenseRepository(_localDatabase, _domainEventPublisher);

        await repository.SaveFixedExpenseAsync(fixedExpense);

        Assert.That(fixedExpense.Events, Is.Empty);
        await _domainEventPublisher.Received(1).PublishAsync(Arg.Any<FixedExpenseCreatedEvent>());

        var restoredFixedExpense = await repository.GetFixedExpenseByIdAsync(fixedExpense.Id);
        Assert.That(fixedExpense.Id, Is.EqualTo(restoredFixedExpense.Id));
        Assert.That(fixedExpense.Name, Is.EqualTo(restoredFixedExpense.Name));
        Assert.That(fixedExpense.Currency, Is.EqualTo(restoredFixedExpense.Currency));
        Assert.That(fixedExpense.DefaultAccountId, Is.EqualTo(restoredFixedExpense.DefaultAccountId));
        Assert.That(fixedExpense.Enabled, Is.EqualTo(restoredFixedExpense.Enabled));
        Assert.That(fixedExpense.Version, Is.EqualTo(restoredFixedExpense.Version));
        Assert.That(restoredFixedExpense.Ranges.Count, Is.EqualTo(3));
        
        foreach (var range in restoredFixedExpense.Ranges)
        {
            Assert.That(fixedExpense.Ranges.Any(x => x.Equals(range)));
        }
    }
}