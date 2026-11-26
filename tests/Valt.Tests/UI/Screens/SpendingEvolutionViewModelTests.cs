using System.Drawing;
using LiteDB;
using NSubstitute;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Common;
using Valt.Infra.Modules.Configuration;
using Valt.Tests.Builders;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Modals.SpendingEvolution;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class SpendingEvolutionViewModelTests
{
    private IQueryDispatcher _queryDispatcher = null!;
    private IConfigurationManager _configurationManager = null!;
    private FakeClock _clock = null!;

    private string _accountAId = null!;
    private string _accountBId = null!;
    private string _categoryAId = null!;
    private string _categoryBId = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _configurationManager = Substitute.For<IConfigurationManager>();
        _clock = new FakeClock(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _accountAId = ObjectId.NewObjectId().ToString();
        _accountBId = ObjectId.NewObjectId().ToString();
        _categoryAId = ObjectId.NewObjectId().ToString();
        _categoryBId = ObjectId.NewObjectId().ToString();

        _configurationManager.GetSpendingEvolutionCategoryFilterExcludedIds().Returns([]);
        _configurationManager.GetSpendingEvolutionAccountFilterExcludedIds().Returns([]);

        _queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AccountDTO>>(
            [
                CreateAccountDto(_accountAId, "Checking"),
                CreateAccountDto(_accountBId, "Savings")
            ]));

        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CategoriesDTO(
            [
                CreateCategoryDto(_categoryAId, "Food"),
                CreateCategoryDto(_categoryBId, "Housing")
            ])));

        _queryDispatcher.DispatchAsync(Arg.Any<GetSpendingEvolutionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(200m, 300m, 400_000L, 600_000L)));
    }

    [Test]
    public async Task OnBindParameterAsync_LoadsAccountsAndCategories_AppliesSavedExclusions_AndRefreshesChart()
    {
        _configurationManager.GetSpendingEvolutionCategoryFilterExcludedIds().Returns([_categoryBId]);
        _configurationManager.GetSpendingEvolutionAccountFilterExcludedIds().Returns([_accountBId]);

        GetSpendingEvolutionQuery? capturedQuery = null;
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => capturedQuery = q),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(200m, 300m, 400_000L, 600_000L)));

        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);

        await sut.OnBindParameterAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sut.AvailableAccounts.Select(a => a.Id), Is.EquivalentTo(new[] { _accountAId, _accountBId }));
            Assert.That(sut.SelectedAccounts.Select(a => a.Id), Is.EquivalentTo(new[] { _accountAId }));
            Assert.That(sut.AvailableCategories.Select(c => c.Id), Is.EquivalentTo(new[] { _categoryAId, _categoryBId }));
            Assert.That(sut.SelectedCategories.Select(c => c.Id), Is.EquivalentTo(new[] { _categoryAId }));

            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery!.AccountIds, Is.EquivalentTo(new[] { _accountAId }));
            Assert.That(capturedQuery.CategoryIds, Is.EquivalentTo(new[] { _categoryAId }));
            Assert.That(capturedQuery.From, Is.EqualTo(new DateOnly(2023, 6, 1)));
            Assert.That(capturedQuery.To, Is.EqualTo(new DateOnly(2025, 6, 15)));

            Assert.That(sut.ChartData.FiatValues, Has.Count.EqualTo(2));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(200.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(300.0));
            Assert.That(sut.FiatIncreasePercent, Is.EqualTo(50m));
            Assert.That(sut.BtcIncreasePercent, Is.EqualTo(50m));
            Assert.That(sut.FiatIncreasePercentText, Does.StartWith("+"));
        }
    }

    [Test]
    public async Task OnBindParameterAsync_WithPreSelectedCategoryId_SelectsOnlyThatCategory()
    {
        GetSpendingEvolutionQuery? capturedQuery = null;
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => capturedQuery = q),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(100m, 150m, 100_000L, 150_000L)));

        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock)
        {
            Parameter = _categoryBId
        };

        await sut.OnBindParameterAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sut.SelectedCategories.Select(c => c.Id), Is.EqualTo(new[] { _categoryBId }));
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery!.CategoryIds, Is.EqualTo(new[] { _categoryBId }));
        }
    }

    [Test]
    public async Task ChangingSelectedCategories_DispatchesQueryWithUpdatedCategories_AndUpdatesChartData()
    {
        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        var reloaded = new TaskCompletionSource<GetSpendingEvolutionQuery>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => reloaded.TrySetResult(q)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(500m, 400m, 900_000L, 450_000L)));

        var housingItem = sut.SelectedCategories.Single(c => c.Id == _categoryBId);
        sut.SelectedCategories.Remove(housingItem);

        var capturedQuery = await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedQuery.CategoryIds, Is.EqualTo(new[] { _categoryAId }));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(500.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(400.0));
            Assert.That(sut.FiatIncreasePercent, Is.EqualTo(-20m));
            Assert.That(sut.BtcIncreasePercent, Is.EqualTo(-50m));
        }
    }

    [Test]
    public async Task ChangingSelectedAccounts_DispatchesQueryWithUpdatedAccounts_AndUpdatesChartData()
    {
        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        var reloaded = new TaskCompletionSource<GetSpendingEvolutionQuery>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => reloaded.TrySetResult(q)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(100m, 250m, 200_000L, 300_000L)));

        var savingsItem = sut.SelectedAccounts.Single(a => a.Id == _accountBId);
        sut.SelectedAccounts.Remove(savingsItem);

        var capturedQuery = await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedQuery.AccountIds, Is.EqualTo(new[] { _accountAId }));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(100.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(250.0));
            Assert.That(sut.FiatIncreasePercent, Is.EqualTo(150m));
            Assert.That(sut.BtcIncreasePercent, Is.EqualTo(50m));
        }
    }

    [Test]
    public async Task UnselectingAllCategories_OrAllAccounts_ClearsChartDataWithoutQueryingAll()
    {
        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        _queryDispatcher.ClearReceivedCalls();

        sut.SelectedCategories.Clear();
        await sut.LoadDataCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            await _queryDispatcher.DidNotReceive().DispatchAsync(
                Arg.Any<GetSpendingEvolutionQuery>(),
                Arg.Any<CancellationToken>());
            Assert.That(sut.CurrentData, Is.Not.Null);
            Assert.That(sut.CurrentData!.Months, Is.Empty);
            Assert.That(sut.ChartData.FiatValues, Is.Empty);
            Assert.That(sut.ChartData.BtcValues, Is.Empty);
            Assert.That(sut.FiatIncreasePercentText, Is.EqualTo(language.SpendingEvolution_NA));
            Assert.That(sut.BtcIncreasePercentText, Is.EqualTo(language.SpendingEvolution_NA));
        }

        // Restore categories, then unselect all accounts
        sut.SelectedCategories.AddRange(sut.AvailableCategories);
        sut.SelectedAccounts.Clear();
        _queryDispatcher.ClearReceivedCalls();

        await sut.LoadDataCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            await _queryDispatcher.DidNotReceive().DispatchAsync(
                Arg.Any<GetSpendingEvolutionQuery>(),
                Arg.Any<CancellationToken>());
            Assert.That(sut.CurrentData!.Months, Is.Empty);
            Assert.That(sut.ChartData.FiatValues, Is.Empty);
            Assert.That(sut.FiatIncreasePercentText, Is.EqualTo(language.SpendingEvolution_NA));
            Assert.That(sut.BtcIncreasePercentText, Is.EqualTo(language.SpendingEvolution_NA));
        }
    }

    [Test]
    public async Task ChangingSelectedTimeRangeOption_DispatchesQueryWithUpdatedFromDate_AndUpdatesChartData()
    {
        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        var reloaded = new TaskCompletionSource<GetSpendingEvolutionQuery>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => reloaded.TrySetResult(q)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(400m, 600m, 800_000L, 1_200_000L)));

        sut.SelectedTimeRangeOption = sut.TimeRangeOptions.Single(x => x.Months == 12);

        var capturedQuery = await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedQuery.From, Is.EqualTo(new DateOnly(2024, 6, 1)));
            Assert.That(capturedQuery.To, Is.EqualTo(new DateOnly(2025, 6, 15)));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(400.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(600.0));
        }
    }

    [Test]
    public async Task LoadFilterCommand_RestoresSavedDefaults_AndReloadsChart()
    {
        _configurationManager.GetSpendingEvolutionCategoryFilterExcludedIds().Returns([_categoryAId]);
        _configurationManager.GetSpendingEvolutionAccountFilterExcludedIds().Returns([_accountAId]);

        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        // Mutate selections away from defaults
        sut.SelectedCategories.Clear();
        sut.SelectedCategories.Add(sut.AvailableCategories.Single(c => c.Id == _categoryAId));
        sut.SelectedAccounts.Clear();
        sut.SelectedAccounts.Add(sut.AvailableAccounts.Single(a => a.Id == _accountAId));

        GetSpendingEvolutionQuery? capturedQuery = null;
        _queryDispatcher.DispatchAsync(
                Arg.Do<GetSpendingEvolutionQuery>(q => capturedQuery = q),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSampleEvolutionData(700m, 770m, 500_000L, 550_000L)));

        await sut.LoadFilterCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sut.SelectedCategories.Select(c => c.Id), Is.EqualTo(new[] { _categoryBId }));
            Assert.That(sut.SelectedAccounts.Select(a => a.Id), Is.EqualTo(new[] { _accountBId }));
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery!.CategoryIds, Is.EqualTo(new[] { _categoryBId }));
            Assert.That(capturedQuery.AccountIds, Is.EqualTo(new[] { _accountBId }));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(700.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(770.0));
        }
    }

    [Test]
    public async Task FilterChangeDuringActiveLoad_QueuesAndExecutesReloadWithLatestFilters()
    {
        using var sut = new SpendingEvolutionViewModel(_queryDispatcher, _configurationManager, _clock);
        await sut.OnBindParameterAsync();

        var firstLoadGate = new TaskCompletionSource<SpendingEvolutionDataDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatchedQueries = new List<GetSpendingEvolutionQuery>();

        _queryDispatcher.DispatchAsync(Arg.Any<GetSpendingEvolutionQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var q = callInfo.Arg<GetSpendingEvolutionQuery>();
                dispatchedQueries.Add(q);
                if (dispatchedQueries.Count == 1)
                {
                    firstLoadStarted.TrySetResult();
                    return firstLoadGate.Task;
                }

                return Task.FromResult(CreateSampleEvolutionData(900m, 990m, 300_000L, 330_000L));
            });

        // Start first load
        var activeLoadTask = sut.LoadDataCommand.ExecuteAsync(null);
        await firstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // While first load is in flight, change selected categories and trigger LoadDataCommand again
        sut.SelectedCategories.Remove(sut.SelectedCategories.Single(c => c.Id == _categoryBId));
        await sut.LoadDataCommand.ExecuteAsync(null);

        // Complete the first in-flight load
        firstLoadGate.SetResult(CreateSampleEvolutionData(100m, 200m, 100_000L, 200_000L));
        await activeLoadTask.WaitAsync(TimeSpan.FromSeconds(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dispatchedQueries, Has.Count.EqualTo(2));
            Assert.That(dispatchedQueries[0].CategoryIds, Is.EquivalentTo(new[] { _categoryAId, _categoryBId }));
            Assert.That(dispatchedQueries[1].CategoryIds, Is.EqualTo(new[] { _categoryAId }));
            Assert.That(sut.ChartData.FiatValues[0].Y, Is.EqualTo(900.0));
            Assert.That(sut.ChartData.FiatValues[1].Y, Is.EqualTo(990.0));
        }
    }

    private static AccountDTO CreateAccountDto(string id, string name) =>
        new(
            Id: id,
            Type: "Fiat",
            Name: name,
            CurrencyNickname: "USD",
            Visible: true,
            IconId: null,
            Unicode: Icon.Empty.Unicode,
            Color: Color.White,
            Currency: "USD",
            IsBtcAccount: false,
            InitialAmountFiat: 0m,
            InitialAmountSats: null,
            GroupId: null);

    private static CategoryDTO CreateCategoryDto(string id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            SimpleName = name,
            Unicode = Icon.Empty.Unicode,
            Color = Color.White
        };

    private static SpendingEvolutionDataDto CreateSampleEvolutionData(
        decimal firstFiat,
        decimal secondFiat,
        long firstSats,
        long secondSats) =>
        new()
        {
            PrimaryCurrency = "USD",
            HasMissingPriceInSats = false,
            Months =
            [
                new SpendingEvolutionMonthDto
                {
                    Month = new DateOnly(2025, 1, 1),
                    FiatTotal = firstFiat,
                    SatsTotal = firstSats,
                    TransactionCount = 1
                },
                new SpendingEvolutionMonthDto
                {
                    Month = new DateOnly(2025, 2, 1),
                    FiatTotal = secondFiat,
                    SatsTotal = secondSats,
                    TransactionCount = 1
                }
            ]
        };
}
