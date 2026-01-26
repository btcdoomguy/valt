using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.TransactionEditor;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class TransactionEditorViewModelTests : DatabaseTest
{
    private ICommandDispatcher _commandDispatcher;
    private IQueryDispatcher _queryDispatcher;
    private List<AccountDTO> _accounts;
    private List<CategoryDTO> _categories;

    [SetUp]
    public new void SetUp()
    {
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _accounts = [];
        _categories = [];

        // Setup default returns
        _queryDispatcher.DispatchAsync(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<IReadOnlyList<AccountDTO>>(_accounts.ToList()));

        _queryDispatcher.DispatchAsync(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new CategoriesDTO(_categories.ToList())));
    }

    private TransactionEditorViewModel CreateInstance()
    {
        var currencySettings = new CurrencySettings(_localDatabase);
        var displaySettings = new DisplaySettings(_localDatabase);

        return new TransactionEditorViewModel(
            _commandDispatcher,
            _queryDispatcher,
            null!,
            null!,
            currencySettings,
            displaySettings);
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
    }

    [Test]
    public void TransactionEditorViewModel_ShouldSetUpCreditScreen()
    {
        var model = CreateInstance();

        model.SwitchToCreditCommand.Execute(null);

        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Credit));
    }

    [Test]
    public void TransactionEditorViewModel_ShouldSetUpTransferScreen()
    {
        var model = CreateInstance();

        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiat()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Fiat Account", FiatCurrency.Brl);

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("Fiat"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("Bitcoin"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var toBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(toBtcAccountId, "Test Btc 2");

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);
        model.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toBtcAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("BitcoinToBitcoin"));
        Assert.That(model.FromAccountIsBtc, Is.True);
        Assert.That(model.ToAccountIsBtc, Is.True);
        Assert.That(model.ShowTransferValueField, Is.False);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToFiat()
    {
        var fromBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(fromBtcAccountId, "Test Btc");

        var toFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(toFiatAccountId, "Test Fiat", FiatCurrency.Brl);

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromBtcAccountId);
        model.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toFiatAccountId);

        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("BitcoinToFiat"));
        Assert.That(model.FromAccountIsBtc, Is.True);
        Assert.That(model.ToAccountIsBtc, Is.False);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(model.ShowTransferValueField, Is.True);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiatToBtc()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Test Fiat", FiatCurrency.Brl);

        var toBtcAccountId = IdGenerator.Generate();
        AddBtcAccount(toBtcAccountId, "Test Btc");

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);
        model.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toBtcAccountId);

        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("FiatToBitcoin"));
        Assert.That(model.FromAccountIsBtc, Is.False);
        Assert.That(model.ToAccountIsBtc, Is.True);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(model.ShowTransferValueField, Is.True);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeFiatToFiatToSameCurrency()
    {
        var fromFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(fromFiatAccountId, "Test Fiat 1", FiatCurrency.Brl);

        var toFiatAccountId = IdGenerator.Generate();
        AddFiatAccount(toFiatAccountId, "Test Fiat 2", FiatCurrency.Brl);

        var model = CreateInstance();

        // Wait for initialization
        await Task.Delay(100);

        var propertiesChanged = new List<string>();
        model.PropertyChanged += (sender, args) => propertiesChanged.Add(args.PropertyName!);

        model.FromAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == fromFiatAccountId);
        model.ToAccount = model.AvailableAccounts.SingleOrDefault(x => x.Id == toFiatAccountId);

        model.SwitchToTransferCommand.Execute(null);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("FiatToFiat"));
        Assert.That(propertiesChanged, Contains.Item(nameof(model.FromAccountIsBtc)));
        Assert.That(propertiesChanged, Contains.Item(nameof(model.ToAccountIsBtc)));
        Assert.That(model.FromAccountIsBtc, Is.False);
        Assert.That(model.ToAccountIsBtc, Is.False);
        Assert.That(model.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(model.ShowTransferValueField, Is.False);
    }
}
