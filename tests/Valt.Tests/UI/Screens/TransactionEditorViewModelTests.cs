using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel.Time;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Accounts.Services;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.Transactions;
using Valt.Infra.Settings;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.TransactionEditor;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class TransactionEditorViewModelTests : DatabaseTest
{
    private AccountQueries _accountQueries;
    private CategoryQueries _categoryQueries;
    private FixedExpenseQueries _fixedExpenseQueries;

    private TransactionEditorViewModel CreateInstance()
    {
        var transactionRepository = new TransactionRepository(_localDatabase, _priceDatabase, _domainEventPublisher);
        _accountQueries = new AccountQueries(_localDatabase, new AccountTotalsCalculator(_localDatabase, new AccountCacheService(_localDatabase, new Clock())));
        _categoryQueries = new CategoryQueries(_localDatabase);
        _fixedExpenseQueries = new FixedExpenseQueries(_localDatabase);
        var currencySettings = new CurrencySettings(_localDatabase);
        var displaySettings = new DisplaySettings(_localDatabase);

        return new TransactionEditorViewModel(
            transactionRepository,
            _accountQueries,
            _categoryQueries,
            _fixedExpenseQueries,
            null,
            null,
            currencySettings,
            displaySettings);
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

        var fiatAccount = new FiatAccountBuilder()
        {
            Id = fromFiatAccountId,
            Name = "Fiat Account",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 0
        }.Build();

        _localDatabase.GetAccounts().Insert(fiatAccount);

        var model = CreateInstance();

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromFiatAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("Fiat"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();

        var btcAccount = new BtcAccountBuilder()
            {
                Id = fromBtcAccountId,
                Name = "Test Btc",
                Value = 0
            }
            .Build();

        _localDatabase.GetAccounts().Insert(btcAccount);

        var model = CreateInstance();

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromBtcAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("Bitcoin"));
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToBtc()
    {
        var fromBtcAccountId = IdGenerator.Generate();

        var fromBtcAccount = new BtcAccountBuilder()
            {
                Id = fromBtcAccountId,
                Name = "Test Btc",
                Value = 0
            }
            .Build();

        _localDatabase.GetAccounts().Insert(fromBtcAccount);

        var toBtcAccountId = IdGenerator.Generate();

        var toBtcAccount = new BtcAccountBuilder()
            {
                Id = toBtcAccountId,
                Name = "Test Btc 2",
                Value = 0
            }
            .Build();

        _localDatabase.GetAccounts().Insert(toBtcAccount);

        var model = CreateInstance();

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromBtcAccountId);
        model.ToAccount = accounts.SingleOrDefault(x => x.Id == toBtcAccountId);

        Assert.That(model.CurrentAccountMode, Is.EqualTo("BitcoinToBitcoin"));
        Assert.That(model.FromAccountIsBtc, Is.True);
        Assert.That(model.ToAccountIsBtc, Is.True);
        Assert.That(model.ShowTransferValueField, Is.False);
    }

    [Test]
    public async Task TransactionEditorViewModel_ShouldModeBeBtcToFiat()
    {
        var fromBtcAccountId = IdGenerator.Generate();

        var fromBtcAccount = new BtcAccountBuilder()
            {
                Id = fromBtcAccountId,
                Name = "Test Btc",
                Value = 0
            }
            .Build();
        _localDatabase.GetAccounts().Insert(fromBtcAccount);

        var toFiatAccountId = IdGenerator.Generate();

        var toFiatAccount = new FiatAccountBuilder()
        {
            Id = toFiatAccountId,
            Name = "Test Fiat",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 0
        }.Build();
        _localDatabase.GetAccounts().Insert(toFiatAccount);

        var model = CreateInstance();

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromBtcAccountId);
        model.ToAccount = accounts.SingleOrDefault(x => x.Id == toFiatAccountId);

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

        var fromFiatAccount = new FiatAccountBuilder()
        {
            Id = fromFiatAccountId,
            Name = "Test Fiat",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 0
        }.Build();

        _localDatabase.GetAccounts().Insert(fromFiatAccount);

        var toBtcAccountId = IdGenerator.Generate();

        var toBtcAccount = new BtcAccountBuilder()
            {
                Id = toBtcAccountId,
                Name = "Test Btc",
                Value = 0
            }
            .Build();

        _localDatabase.GetAccounts().Insert(toBtcAccount);

        var model = CreateInstance();

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromFiatAccountId);
        model.ToAccount = accounts.SingleOrDefault(x => x.Id == toBtcAccountId);

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

        var fromFiatAccount = new FiatAccountBuilder()
        {
            Id = fromFiatAccountId,
            Name = "Test Fiat 2",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 0
        }.Build();
        _localDatabase.GetAccounts().Insert(fromFiatAccount);

        var toFiatAccountId = IdGenerator.Generate();

        var toFiatAccount = new FiatAccountBuilder()
        {
            Id = toFiatAccountId,
            Name = "Test Fiat 2",
            Icon = Icon.Empty,
            FiatCurrency = FiatCurrency.Brl,
            Value = 0
        }.Build();

        _localDatabase.GetAccounts().Insert(toFiatAccount);

        var model = CreateInstance();
        var propertiesChanged = new List<string>();
        model.PropertyChanged += (sender, args) => propertiesChanged.Add(args.PropertyName);

        var accounts = await _accountQueries.GetAccountsAsync(false);

        model.FromAccount = accounts.SingleOrDefault(x => x.Id == fromFiatAccountId);
        model.ToAccount = accounts.SingleOrDefault(x => x.Id == toFiatAccountId);

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