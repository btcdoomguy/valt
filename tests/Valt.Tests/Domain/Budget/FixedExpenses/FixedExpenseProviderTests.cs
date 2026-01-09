using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.EventSystem;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Modules.Budget.Transactions;
using Transaction = Valt.Core.Modules.Budget.Transactions.Transaction;

namespace Valt.Tests.Domain.Budget.FixedExpenses;

[TestFixture]
public class FixedExpenseProviderTests : IntegrationTest
{
    private FixedExpenseId _iptuFixedExpenseId = null!;
    private FixedExpenseId _electricityFixedExpenseId = null!;
    
    public async Task PrepareDataType1()
    {
        var repository = _serviceProvider.GetRequiredService<IFixedExpenseRepository>(); 
        
        _iptuFixedExpenseId = await CreateFixedExpenseAsync(repository,
            FixedExpense.New("IPTU", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateFixedAmount(FiatValue.New(100m),  FixedExpensePeriods.Yearly, new DateOnly(2025, 1, 1), 15)
                }, true));
        _electricityFixedExpenseId = await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Electricity", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 30)
                },true));
        await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Gas (deactivated)", null, new CategoryId(), FiatCurrency.Brl, new List<FixedExpenseRange>()
            {
                FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                    FixedExpensePeriods.Monthly, new DateOnly(2025, 1, 1), 30)
            }, false));
    }

    [TearDown]
    public async Task ClearTables()
    {
        _localDatabase.GetFixedExpenses().DeleteAll();
        _localDatabase.GetFixedExpenseRecords().DeleteAll();
    }

    private async Task<FixedExpenseId> CreateFixedExpenseAsync(IFixedExpenseRepository repository, FixedExpense fixedExpense)
    {
        await repository.SaveFixedExpenseAsync(fixedExpense);
        return fixedExpense.Id;
    }

    [Test]
    public async Task Should_Get_TwoFixedExpenses()
    {
        await PrepareDataType1();
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 1));
        
        Assert.That(entries.Count, Is.EqualTo(2));
    }
    
    [Test]
    public async Task Should_Get_OneFixedExpenses()
    {
        await PrepareDataType1();
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 2, 1));
        
        Assert.That(entries.Count, Is.EqualTo(1));
    }
    
    [Test]
    public async Task Should_Get_NoFixedExpenses()
    {
        await PrepareDataType1();
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2024, 1, 1));
        
        Assert.That(entries.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task Should_Detect_FixedExpense_Was_Paid()
    {
        await PrepareDataType1();
        
        //adds a transaction bound to the electricity entry
        var brlAccount = FiatAccount.New("My account", true, Icon.Empty, FiatCurrency.Brl, FiatValue.New(1000m));
        var accountRepo = new AccountRepository(_localDatabase, _serviceProvider.GetRequiredService<IDomainEventPublisher>());
        
        await accountRepo.SaveAccountAsync(brlAccount);

        var transaction = Transaction.New(new DateOnly(2025, 1, 25), "Electricity", new CategoryId(),
            new FiatDetails(brlAccount.Id, FiatValue.New(170m), false), null, new TransactionFixedExpenseReference(_electricityFixedExpenseId.Value, new DateOnly(2025, 1, 30)));
        var transactionRepo = new TransactionRepository(_localDatabase, _priceDatabase, _serviceProvider.GetRequiredService<IDomainEventPublisher>());
        
        await transactionRepo.SaveTransactionAsync(transaction);
        
        //the domaineventhandlers should create the binding after saving

        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 31));
        
        var electricityFixedExpenseEntry = entries.Single(x => x.Id == _electricityFixedExpenseId.Value);
        Assert.That(electricityFixedExpenseEntry.Paid, Is.True);
    }
    
    [Test]
    public async Task Should_Detect_FixedExpense_Was_MarkedAsPaid()
    {
        await PrepareDataType1();

        var fixedExpenseRecordEntity = new FixedExpenseRecordEntity()
        {
            Id = ObjectId.NewObjectId(),
            FixedExpense = _localDatabase.GetFixedExpenses().FindById(new ObjectId(_electricityFixedExpenseId.Value)),
            ReferenceDate = new DateOnly(2025, 1, 30).ToValtDateTime(),
            FixedExpenseRecordStateId = (int) FixedExpenseRecordState.ManuallyPaid
        };
        _localDatabase.GetFixedExpenseRecords().Insert(fixedExpenseRecordEntity);
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 31));
        
        var electricityFixedExpenseEntry = entries.Single(x => x.Id == _electricityFixedExpenseId.Value);
        Assert.That(electricityFixedExpenseEntry.MarkedAsPaid, Is.True);
    }
    
    [Test]
    public async Task Should_Detect_FixedExpense_Was_Ignored()
    {
        await PrepareDataType1();

        var fixedExpenseRecordEntity = new FixedExpenseRecordEntity()
        {
            Id = ObjectId.NewObjectId(),
            FixedExpense = _localDatabase.GetFixedExpenses().FindById(_electricityFixedExpenseId.ToObjectId()),
            ReferenceDate = new DateOnly(2025, 1, 30).ToValtDateTime(),
            FixedExpenseRecordStateId = (int) FixedExpenseRecordState.Ignored
        };
        _localDatabase.GetFixedExpenseRecords().Insert(fixedExpenseRecordEntity);
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 1, 31));
        
        var electricityFixedExpenseEntry = entries.Single(x => x.Id == _electricityFixedExpenseId.Value);
        Assert.That(electricityFixedExpenseEntry.Ignored, Is.True);
    }
    
    [Test]
    public async Task Should_Get_MultipleEntriesOfWeeklyFixedExpenses()
    {
        var repository = _serviceProvider.GetRequiredService<IFixedExpenseRepository>(); 
        
        await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Nanny Weekly", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Weekly, new DateOnly(2025, 10, 8), DayOfWeek.Friday)
                },true));
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 10, 1));
        
        Assert.That(entries.Count, Is.EqualTo(4));
    }
    
    [Test]
    public async Task Should_Get_MultipleEntriesOfBiWeeklyFixedExpenses()
    {
        var repository = _serviceProvider.GetRequiredService<IFixedExpenseRepository>(); 
        
        await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Nanny BiWeekly", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Biweekly, new DateOnly(2025, 10, 8), DayOfWeek.Friday)
                },true));
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 10, 1));
        
        Assert.That(entries.Count, Is.EqualTo(2));
    }
    
    [Test]
    public async Task Should_Get_MultipleEntriesOfBiWeeklyFixedExpensesInDifferentRanges()
    {
        var repository = _serviceProvider.GetRequiredService<IFixedExpenseRepository>(); 
        
        await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Nanny Weekly", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Weekly, new DateOnly(2025, 10, 8), DayOfWeek.Friday),
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Weekly, new DateOnly(2025, 10, 25), DayOfWeek.Sunday)
                },true));
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 10, 1));
        
        Assert.That(entries.Count, Is.EqualTo(4));
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 10, 10)), Is.Not.Null);
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 10, 17)), Is.Not.Null);
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 10, 24)), Is.Not.Null);
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 10, 26)), Is.Not.Null);
    }
    
    [Test]
    public async Task Should_Get_MultipleEntriesOfBiWeeklyFixedExpensesInDifferentRangesInDistantMonth()
    {
        var repository = _serviceProvider.GetRequiredService<IFixedExpenseRepository>(); 
        
        await CreateFixedExpenseAsync(repository,
            FixedExpense.New("Nanny Weekly", null, new CategoryId(), FiatCurrency.Brl, 
                new List<FixedExpenseRange>()
                {
                    FixedExpenseRange.CreateRangedAmount(new RangedFiatValue(FiatValue.New(150m), FiatValue.New(250m)),
                        FixedExpensePeriods.Biweekly, new DateOnly(2025, 10, 8), DayOfWeek.Friday),
                },true));
        
        var provider = new FixedExpenseProvider(_localDatabase);

        var entries = await provider.GetFixedExpensesOfMonthAsync(new DateOnly(2025, 11, 1));
        
        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 11, 7)), Is.Not.Null);
        Assert.That(entries.Single(x => x.ReferenceDate == new DateOnly(2025, 11, 21)), Is.Not.Null);
    }
}