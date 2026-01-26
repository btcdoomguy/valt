using System.ComponentModel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.DataAccess;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class TransactionListViewModelDataGridTests : DatabaseTest
{
    private ILocalStorageService _localStorageService = null!;
    private IModalFactory _modalFactory = null!;
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private ITransactionTermService _transactionTermService = null!;
    private FilterState _filterState = null!;
    private IClock _clock = null!;
    private ILogger<TransactionListViewModel> _vmLogger = null!;

    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        _localStorageService = Substitute.For<ILocalStorageService>();
        _localStorageService.LoadDataGridSettings().Returns(new DataGridSettings());

        _modalFactory = Substitute.For<IModalFactory>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _transactionTermService = Substitute.For<ITransactionTermService>();
        _filterState = new FilterState();
        _clock = Substitute.For<IClock>();
        _vmLogger = Substitute.For<ILogger<TransactionListViewModel>>();
    }

    private TransactionListViewModel CreateViewModel()
    {
        var currencySettings = new CurrencySettings(_localDatabase);
        var ratesState = new RatesState();
        var liveRateState = new LiveRateState(
            currencySettings,
            _localDatabase,
            _priceDatabase,
            Substitute.For<ILocalHistoricalPriceProvider>(),
            ratesState,
            Substitute.For<ILogger<LiveRateState>>());

        return new TransactionListViewModel(
            _modalFactory,
            _commandDispatcher,
            _queryDispatcher,
            _transactionTermService,
            liveRateState,
            _localDatabase,
            currencySettings,
            _filterState,
            _clock,
            _localStorageService,
            _vmLogger);
    }

    #region UpdateSortState Tests

    [Test]
    public void UpdateSortState_WhenNewColumn_SetsAscendingDirection()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.UpdateSortState("Date");

        // Assert
        Assert.That(vm.OrderedColumn, Is.EqualTo("Date"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    [Test]
    public void UpdateSortState_WhenSameColumn_TogglesDirectionToDescending()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateSortState("Date"); // First call sets Ascending

        // Act
        vm.UpdateSortState("Date"); // Second call should toggle

        // Assert
        Assert.That(vm.OrderedColumn, Is.EqualTo("Date"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Descending));
    }

    [Test]
    public void UpdateSortState_WhenSameColumnThreeTimes_TogglesBackToAscending()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateSortState("Date"); // Ascending
        vm.UpdateSortState("Date"); // Descending

        // Act
        vm.UpdateSortState("Date"); // Back to Ascending

        // Assert
        Assert.That(vm.OrderedColumn, Is.EqualTo("Date"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    [Test]
    public void UpdateSortState_WhenDifferentColumn_ResetsToAscending()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateSortState("Date");
        vm.UpdateSortState("Date"); // Now Descending

        // Act
        vm.UpdateSortState("Name"); // Switch to different column

        // Assert
        Assert.That(vm.OrderedColumn, Is.EqualTo("Name"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    [Test]
    public void UpdateSortState_WhenNullColumn_SetsNullWithAscending()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.UpdateSortState(null);

        // Assert
        Assert.That(vm.OrderedColumn, Is.Null);
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    #endregion

    #region GetDataGridSettings Tests

    [Test]
    public void GetDataGridSettings_ReturnsSettingsFromStorageService()
    {
        // Arrange
        var expectedSettings = new DataGridSettings
        {
            OrderedColumn = "Amount",
            SortDirection = ListSortDirection.Descending,
            ColumnWidths = new Dictionary<string, double> { { "Amount", 120.5 } },
            ColumnOrder = new List<string> { "Date", "Amount" }
        };
        _localStorageService.LoadDataGridSettings().Returns(expectedSettings);
        var vm = CreateViewModel();

        // Act
        var settings = vm.GetDataGridSettings();

        // Assert
        Assert.That(settings.OrderedColumn, Is.EqualTo("Amount"));
        Assert.That(settings.SortDirection, Is.EqualTo(ListSortDirection.Descending));
        Assert.That(vm.OrderedColumn, Is.EqualTo("Amount"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Descending));
    }

    [Test]
    public void GetDataGridSettings_UpdatesViewModelStateFromSettings()
    {
        // Arrange
        var expectedSettings = new DataGridSettings
        {
            OrderedColumn = "Category",
            SortDirection = ListSortDirection.Ascending
        };
        _localStorageService.LoadDataGridSettings().Returns(expectedSettings);
        var vm = CreateViewModel();

        // Act
        vm.GetDataGridSettings();

        // Assert
        Assert.That(vm.OrderedColumn, Is.EqualTo("Category"));
        Assert.That(vm.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    #endregion

    #region SaveDataGridSettings Tests

    [Test]
    public void SaveDataGridSettings_CallsStorageServiceWithCorrectData()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateSortState("Date");

        var columns = new List<DataGridColumnInfo>
        {
            new() { Tag = "Date", Width = 150, DisplayIndex = 0 },
            new() { Tag = "Name", Width = 200, DisplayIndex = 1 },
            new() { Tag = "Amount", Width = 100, DisplayIndex = 2 }
        };

        // Act
        vm.SaveDataGridSettings(columns);

        // Assert
        _localStorageService.Received(1).SaveDataGridSettingsAsync(
            Arg.Is<DataGridSettings>(s =>
                s.OrderedColumn == "Date" &&
                s.SortDirection == ListSortDirection.Ascending &&
                s.ColumnWidths.ContainsKey("Date") &&
                s.ColumnWidths["Date"] == 150 &&
                s.ColumnOrder.Count == 3 &&
                s.ColumnOrder[0] == "Date"));
    }

    [Test]
    public void SaveDataGridSettings_PreservesColumnOrder()
    {
        // Arrange
        var vm = CreateViewModel();

        var columns = new List<DataGridColumnInfo>
        {
            new() { Tag = "Amount", Width = 100, DisplayIndex = 2 },
            new() { Tag = "Date", Width = 150, DisplayIndex = 0 },
            new() { Tag = "Name", Width = 200, DisplayIndex = 1 }
        };

        // Act
        vm.SaveDataGridSettings(columns);

        // Assert
        _localStorageService.Received(1).SaveDataGridSettingsAsync(
            Arg.Is<DataGridSettings>(s =>
                s.ColumnOrder[0] == "Date" &&
                s.ColumnOrder[1] == "Name" &&
                s.ColumnOrder[2] == "Amount"));
    }

    [Test]
    public void SaveDataGridSettings_IncludesCurrentSortState()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.UpdateSortState("Amount");
        vm.UpdateSortState("Amount"); // Toggle to Descending

        var columns = new List<DataGridColumnInfo>
        {
            new() { Tag = "Amount", Width = 100, DisplayIndex = 0 }
        };

        // Act
        vm.SaveDataGridSettings(columns);

        // Assert
        _localStorageService.Received(1).SaveDataGridSettingsAsync(
            Arg.Is<DataGridSettings>(s =>
                s.OrderedColumn == "Amount" &&
                s.SortDirection == ListSortDirection.Descending));
    }

    #endregion
}
