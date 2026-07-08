using Microsoft.Extensions.Logging;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Modals.TransactionEditor.ChildViewModels;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class TransactionEditorViewModelTests : DatabaseTest
{
    private ICommandDispatcher _commandDispatcher;
    private IQueryDispatcher _queryDispatcher;
    private IFireAndForgetTaskRunner _runner;
    private ILogger<TransactionEditorViewModel> _logger;
    private ILoggerFactory _loggerFactory;
    private List<AccountDTO> _accounts;
    private List<CategoryDTO> _categories;

    [SetUp]
    public new void SetUp()
    {
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _runner = Substitute.For<IFireAndForgetTaskRunner>();
        _logger = Substitute.For<ILogger<TransactionEditorViewModel>>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _accounts = [];
        _categories = [];

        // Setup default returns
        _queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<IReadOnlyList<AccountDTO>>(_accounts.ToList()));

        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new CategoriesDTO(_categories.ToList())));
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
    }

    private TransactionEditorViewModel CreateInstance()
    {
        var currencySettings = new CurrencySettings(_localDatabase, null!);
        var displaySettings = new DisplaySettings(_localDatabase, null!);
        var lastTransactionDateState = new LastTransactionDateState();

        return new TransactionEditorViewModel(
            _commandDispatcher,
            _queryDispatcher,
            null!,
            null!,
            currencySettings,
            displaySettings,
            lastTransactionDateState,
            _runner,
            _logger,
            _loggerFactory,
            new TransactionDetailsBuilder());
    }

    private void AddFiatAccount(string id, string name, FiatCurrency currency)
    {
        _accounts.Add(new AccountDTO(
            Id: id,
            Type: nameof(AccountTypes.Fiat),
            Name: name,
            CurrencyNickname: "",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: System.Drawing.Color.Empty,
            Currency: currency.Code,
            IsBtcAccount: false,
            InitialAmountFiat: 0,
            InitialAmountSats: null,
            GroupId: null));
    }

    private void AddBtcAccount(string id, string name)
    {
        _accounts.Add(new AccountDTO(
            Id: id,
            Type: nameof(AccountTypes.Bitcoin),
            Name: name,
            CurrencyNickname: "",
            Visible: true,
            IconId: null,
            Unicode: '\0',
            Color: System.Drawing.Color.Empty,
            Currency: null,
            IsBtcAccount: true,
            InitialAmountFiat: null,
            InitialAmountSats: 0,
            GroupId: null));
    }

    [Test]
    public void TransactionEditorViewModel_ShouldSetUpDebtScreen()
    {
        var model = CreateInstance();

        model.SwitchToDebtCommand.Execute(null);

        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Debt));
        Assert.That(model.ActiveChildViewModel, Is.InstanceOf<DebtTransactionEditorViewModel>());
    }

    [Test]
    public void TransactionEditorViewModel_ShouldSetUpCreditScreen()
    {
        var model = CreateInstance();

        model.SwitchToCreditCommand.Execute(null);

        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Credit));
        Assert.That(model.ActiveChildViewModel, Is.InstanceOf<CreditTransactionEditorViewModel>());
    }

    [Test]
    public void TransactionEditorViewModel_ShouldSetUpTransferScreen()
    {
        var model = CreateInstance();

        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(model.ActiveChildViewModel, Is.InstanceOf<TransferTransactionEditorViewModel>());
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiat()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Fiat Account", FiatCurrency.Brl);

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.SwitchToTransferCommand.Execute(null);
        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);

        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("Fiat"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.SwitchToTransferCommand.Execute(null);
        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);

        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("Bitcoin"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var toBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(toBtcAccountId, "Test Btc 2");

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.SwitchToTransferCommand.Execute(null);
        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);
        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        transfer.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toBtcAccountId);

        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("BitcoinToBitcoin"));
        Assert.That(transfer.FromAccountIsBtc, Is.True);
        Assert.That(transfer.ToAccountIsBtc, Is.True);
        Assert.That(transfer.ShowTransferValueField, Is.False);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToFiat()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var toFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(toFiatAccountId, "Test Fiat", FiatCurrency.Brl);

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);

        model.SwitchToTransferCommand.Execute(null);

        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        transfer.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toFiatAccountId);

        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("BitcoinToFiat"));
        Assert.That(transfer.FromAccountIsBtc, Is.True);
        Assert.That(transfer.ToAccountIsBtc, Is.False);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(transfer.ShowTransferValueField, Is.True);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiatToBtc()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Test Fiat", FiatCurrency.Brl);

        var toBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(toBtcAccountId, "Test Btc");

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);

        model.SwitchToTransferCommand.Execute(null);

        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        transfer.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toBtcAccountId);

        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("FiatToBitcoin"));
        Assert.That(transfer.FromAccountIsBtc, Is.False);
        Assert.That(transfer.ToAccountIsBtc, Is.True);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(transfer.ShowTransferValueField, Is.True);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiatToFiatToSameCurrency()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Test Fiat 1", FiatCurrency.Brl);

        var toFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(toFiatAccountId, "Test Fiat 2", FiatCurrency.Brl);

        var model = CreateInstance();

        await model.OnBindParameterAsync();

        model.ActiveChildViewModel!.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);
        model.SwitchToTransferCommand.Execute(null);
        var transfer = (TransferTransactionEditorViewModel)model.ActiveChildViewModel;
        transfer.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toFiatAccountId);

        Assert.That(transfer.CurrentAccountMode, Is.EqualTo("FiatToFiat"));
        Assert.That(transfer.FromAccountIsBtc, Is.False);
        Assert.That(transfer.ToAccountIsBtc, Is.False);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(transfer.ShowTransferValueField, Is.False);
    }

    [Test]
    public async Task TransactionEditorViewModel_OnBindParameterAsync_ShouldSetActiveChild()
    {
        var model = CreateInstance();

        await model.OnBindParameterAsync();

        Assert.That(model.ActiveChildViewModel, Is.Not.Null);
        Assert.That(model.ActiveChildViewModel, Is.InstanceOf<DebtTransactionEditorViewModel>());
    }

    [Test]
    public void TransactionEditorViewModel_OkButtonLabel_ShouldBeSaveTransactionForDebt()
    {
        var model = CreateInstance();
        model.SwitchToDebtCommand.Execute(null);

        Assert.That(model.OkButtonLabel, Is.EqualTo(language.TransactionEditor_SaveTransaction));
    }

    [Test]
    public void TransactionEditorViewModel_OkButtonLabel_ShouldBeCreateTransferForTransferAdd()
    {
        var model = CreateInstance();
        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.OkButtonLabel, Is.EqualTo(language.TransactionEditor_CreateTransfer));
    }
}
