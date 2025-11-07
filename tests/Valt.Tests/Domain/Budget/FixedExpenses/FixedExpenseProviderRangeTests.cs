using Microsoft.Extensions.DependencyInjection;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.FixedExpenses;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

[TestFixture]
public class FixedExpenseProviderRangeTests : IntegrationTest
{
    private FixedExpenseId _electricityFixedExpenseId = null!;
    
    [OneTimeSetUp]
    public async Task Setup()
    {
        var repository = new FixedExpenseRepository(_localDatabase, _serviceProvider.GetRequiredService<IDomainEventPublisher>());
        
        _electricityFixedExpenseId = await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Electricity", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 10),
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 11), 15),
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 16), 27)
                },true));
    }

    [TearDown]
    public async Task ClearTables()
    {
        _localDatabase.GetFixedExpenseRecords().DeleteAll();
    }

    private async Task<FixedExpenseId> CreateFixedExpenseAsync(FixedExpenseRepository repository, FixedExpense fixedExpense)
    {
        await repository.SaveFixedExpenseAsync(fixedExpense);
        return fixedExpense.Id;
    }

    [Test]
    public async Task Should_Get_ThreeExpensesFromSameFixedExpense()
    {
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1));
        
        Assert.That(entries.Count, Is.EqualTo(3));
    }
}