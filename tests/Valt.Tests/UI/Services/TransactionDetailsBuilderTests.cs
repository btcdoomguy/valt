using System.Drawing;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.UI.Services;

namespace Valt.Tests.UI.Services;

[TestFixture]
public class TransactionDetailsBuilderTests
{
    private TransactionDetailsBuilder _builder = null!;
    private List<AccountDTO> _accounts = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new TransactionDetailsBuilder();
        _accounts = [];
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
            Color: Color.Empty,
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
            Color: Color.Empty,
            Currency: null,
            IsBtcAccount: true,
            InitialAmountFiat: null,
            InitialAmountSats: 0,
            GroupId: null));
    }

    #region BuildDto Tests

    [Test]
    public void BuildDto_FiatDebt_ReturnsFiatTransactionDto()
    {
        var accountId = "fiat-debt";
        AddFiatAccount(accountId, "Checking", FiatCurrency.Usd);
        var account = _accounts[0];

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Debt,
            FromAccount: account,
            ToAccount: null,
            FromAccountBtcValue: null,
            FromAccountFiatValue: FiatValue.New(123.45m),
            ToAccountBtcValue: null,
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<FiatTransactionDto>());
        var fiat = (FiatTransactionDto)dto;
        Assert.That(fiat.FromAccountId, Is.EqualTo(accountId));
        Assert.That(fiat.Amount, Is.EqualTo(123.45m));
        Assert.That(fiat.IsCredit, Is.False);
    }

    [Test]
    public void BuildDto_FiatCredit_ReturnsFiatTransactionDto()
    {
        var accountId = "fiat-credit";
        AddFiatAccount(accountId, "Checking", FiatCurrency.Usd);
        var account = _accounts[0];

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Credit,
            FromAccount: account,
            ToAccount: null,
            FromAccountBtcValue: null,
            FromAccountFiatValue: FiatValue.New(99.99m),
            ToAccountBtcValue: null,
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<FiatTransactionDto>());
        var fiat = (FiatTransactionDto)dto;
        Assert.That(fiat.FromAccountId, Is.EqualTo(accountId));
        Assert.That(fiat.Amount, Is.EqualTo(99.99m));
        Assert.That(fiat.IsCredit, Is.True);
    }

    [Test]
    public void BuildDto_BitcoinDebt_ReturnsBitcoinTransactionDto()
    {
        var accountId = "btc-debt";
        AddBtcAccount(accountId, "Savings");
        var account = _accounts[0];

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Debt,
            FromAccount: account,
            ToAccount: null,
            FromAccountBtcValue: BtcValue.New(500_000),
            FromAccountFiatValue: null,
            ToAccountBtcValue: null,
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<BitcoinTransactionDto>());
        var btc = (BitcoinTransactionDto)dto;
        Assert.That(btc.FromAccountId, Is.EqualTo(accountId));
        Assert.That(btc.AmountSats, Is.EqualTo(500_000));
        Assert.That(btc.IsCredit, Is.False);
    }

    [Test]
    public void BuildDto_BitcoinCredit_ReturnsBitcoinTransactionDto()
    {
        var accountId = "btc-credit";
        AddBtcAccount(accountId, "Savings");
        var account = _accounts[0];

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Credit,
            FromAccount: account,
            ToAccount: null,
            FromAccountBtcValue: BtcValue.New(1_000_000),
            FromAccountFiatValue: null,
            ToAccountBtcValue: null,
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<BitcoinTransactionDto>());
        var btc = (BitcoinTransactionDto)dto;
        Assert.That(btc.FromAccountId, Is.EqualTo(accountId));
        Assert.That(btc.AmountSats, Is.EqualTo(1_000_000));
        Assert.That(btc.IsCredit, Is.True);
    }

    [Test]
    public void BuildDto_FiatToFiatSameCurrency_SetsToAmountEqualToFromAmount()
    {
        var fromId = "from-fiat";
        var toId = "to-fiat";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddFiatAccount(toId, "Savings", FiatCurrency.Brl);

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Transfer,
            FromAccount: _accounts[0],
            ToAccount: _accounts[1],
            FromAccountBtcValue: null,
            FromAccountFiatValue: FiatValue.New(200m),
            ToAccountBtcValue: null,
            ToAccountFiatValue: FiatValue.New(250m),
            AccountsAreSameTypeAndCurrency: true);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<FiatToFiatTransferDto>());
        var transfer = (FiatToFiatTransferDto)dto;
        Assert.That(transfer.FromAccountId, Is.EqualTo(fromId));
        Assert.That(transfer.ToAccountId, Is.EqualTo(toId));
        Assert.That(transfer.FromAmount, Is.EqualTo(200m));
        Assert.That(transfer.ToAmount, Is.EqualTo(200m));
    }

    [Test]
    public void BuildDto_FiatToFiatDifferentCurrency_SetsToAmountFromToAccountValue()
    {
        var fromId = "from-fiat";
        var toId = "to-fiat";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddFiatAccount(toId, "Savings", FiatCurrency.Usd);

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Transfer,
            FromAccount: _accounts[0],
            ToAccount: _accounts[1],
            FromAccountBtcValue: null,
            FromAccountFiatValue: FiatValue.New(200m),
            ToAccountBtcValue: null,
            ToAccountFiatValue: FiatValue.New(40m),
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<FiatToFiatTransferDto>());
        var transfer = (FiatToFiatTransferDto)dto;
        Assert.That(transfer.FromAmount, Is.EqualTo(200m));
        Assert.That(transfer.ToAmount, Is.EqualTo(40m));
    }

    [Test]
    public void BuildDto_BitcoinToBitcoin_ReturnsBitcoinToBitcoinTransferDto()
    {
        var fromId = "from-btc";
        var toId = "to-btc";
        AddBtcAccount(fromId, "BTC 1");
        AddBtcAccount(toId, "BTC 2");

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Transfer,
            FromAccount: _accounts[0],
            ToAccount: _accounts[1],
            FromAccountBtcValue: BtcValue.New(100_000),
            FromAccountFiatValue: null,
            ToAccountBtcValue: BtcValue.New(100_000),
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: true);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<BitcoinToBitcoinTransferDto>());
        var transfer = (BitcoinToBitcoinTransferDto)dto;
        Assert.That(transfer.FromAccountId, Is.EqualTo(fromId));
        Assert.That(transfer.ToAccountId, Is.EqualTo(toId));
        Assert.That(transfer.AmountSats, Is.EqualTo(100_000));
    }

    [Test]
    public void BuildDto_FiatToBitcoin_ReturnsFiatToBitcoinTransferDto()
    {
        var fromId = "from-fiat";
        var toId = "to-btc";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddBtcAccount(toId, "BTC");

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Transfer,
            FromAccount: _accounts[0],
            ToAccount: _accounts[1],
            FromAccountBtcValue: null,
            FromAccountFiatValue: FiatValue.New(500m),
            ToAccountBtcValue: BtcValue.New(50_000),
            ToAccountFiatValue: null,
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<FiatToBitcoinTransferDto>());
        var transfer = (FiatToBitcoinTransferDto)dto;
        Assert.That(transfer.FromAccountId, Is.EqualTo(fromId));
        Assert.That(transfer.ToAccountId, Is.EqualTo(toId));
        Assert.That(transfer.FromFiatAmount, Is.EqualTo(500m));
        Assert.That(transfer.ToSatsAmount, Is.EqualTo(50_000));
    }

    [Test]
    public void BuildDto_BitcoinToFiat_ReturnsBitcoinToFiatTransferDto()
    {
        var fromId = "from-btc";
        var toId = "to-fiat";
        AddBtcAccount(fromId, "BTC");
        AddFiatAccount(toId, "Checking", FiatCurrency.Brl);

        var snapshot = new TransactionFormSnapshot(
            SelectedMode: TransactionTypes.Transfer,
            FromAccount: _accounts[0],
            ToAccount: _accounts[1],
            FromAccountBtcValue: BtcValue.New(75_000),
            FromAccountFiatValue: null,
            ToAccountBtcValue: null,
            ToAccountFiatValue: FiatValue.New(300m),
            AccountsAreSameTypeAndCurrency: false);

        var dto = _builder.BuildDto(snapshot);

        Assert.That(dto, Is.TypeOf<BitcoinToFiatTransferDto>());
        var transfer = (BitcoinToFiatTransferDto)dto;
        Assert.That(transfer.FromAccountId, Is.EqualTo(fromId));
        Assert.That(transfer.ToAccountId, Is.EqualTo(toId));
        Assert.That(transfer.FromSatsAmount, Is.EqualTo(75_000));
        Assert.That(transfer.ToFiatAmount, Is.EqualTo(300m));
    }

    #endregion

    #region LoadFromDto Tests

    [Test]
    public void LoadFromDto_FiatTransactionDebt_RoundTripsValues()
    {
        var accountId = "fiat-debt";
        AddFiatAccount(accountId, "Checking", FiatCurrency.Usd);

        var dto = new FiatTransactionDto
        {
            FromAccountId = accountId,
            Amount = 150m,
            IsCredit = false
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Debt));
        Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(150m)));
        Assert.That(values.ToAccount, Is.Null);
    }

    [Test]
    public void LoadFromDto_FiatTransactionCredit_RoundTripsValues()
    {
        var accountId = "fiat-credit";
        AddFiatAccount(accountId, "Checking", FiatCurrency.Usd);

        var dto = new FiatTransactionDto
        {
            FromAccountId = accountId,
            Amount = 250m,
            IsCredit = true
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Credit));
        Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(250m)));
    }

    [Test]
    public void LoadFromDto_BitcoinTransaction_RoundTripsValues()
    {
        var accountId = "btc";
        AddBtcAccount(accountId, "Savings");

        var dto = new BitcoinTransactionDto
        {
            FromAccountId = accountId,
            AmountSats = 777_777,
            IsCredit = false
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Debt));
        Assert.That(values.FromAccountBtcValue, Is.EqualTo(BtcValue.New(777_777)));
    }

    [Test]
    public void LoadFromDto_FiatToFiatSameCurrency_SetsToAmountEqualToFromAmount()
    {
        var fromId = "from-fiat";
        var toId = "to-fiat";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddFiatAccount(toId, "Savings", FiatCurrency.Brl);

        var dto = new FiatToFiatTransferDto
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            FromAmount = 300m,
            ToAmount = 300m
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(values.ToAccount, Is.EqualTo(_accounts[1]));
        Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(300m)));
        Assert.That(values.ToAccountFiatValue, Is.EqualTo(FiatValue.New(300m)));
    }

    [Test]
    public void LoadFromDto_FiatToFiatDifferentCurrency_SetsToAmountFromDto()
    {
        var fromId = "from-fiat";
        var toId = "to-fiat";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddFiatAccount(toId, "Savings", FiatCurrency.Usd);

        var dto = new FiatToFiatTransferDto
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            FromAmount = 300m,
            ToAmount = 60m
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(values.ToAccount, Is.EqualTo(_accounts[1]));
        Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(300m)));
        Assert.That(values.ToAccountFiatValue, Is.EqualTo(FiatValue.New(60m)));
    }

    [Test]
    public void LoadFromDto_BitcoinToBitcoin_RoundTripsValues()
    {
        var fromId = "from-btc";
        var toId = "to-btc";
        AddBtcAccount(fromId, "BTC 1");
        AddBtcAccount(toId, "BTC 2");

        var dto = new BitcoinToBitcoinTransferDto
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            AmountSats = 888_888
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(values.ToAccount, Is.EqualTo(_accounts[1]));
        Assert.That(values.FromAccountBtcValue, Is.EqualTo(BtcValue.New(888_888)));
        Assert.That(values.ToAccountBtcValue, Is.EqualTo(BtcValue.New(888_888)));
    }

    [Test]
    public void LoadFromDto_FiatToBitcoin_RoundTripsValues()
    {
        var fromId = "from-fiat";
        var toId = "to-btc";
        AddFiatAccount(fromId, "Checking", FiatCurrency.Brl);
        AddBtcAccount(toId, "BTC");

        var dto = new FiatToBitcoinTransferDto
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            FromFiatAmount = 900m,
            ToSatsAmount = 100_000
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(values.ToAccount, Is.EqualTo(_accounts[1]));
        Assert.That(values.FromAccountFiatValue, Is.EqualTo(FiatValue.New(900m)));
        Assert.That(values.ToAccountBtcValue, Is.EqualTo(BtcValue.New(100_000)));
    }

    [Test]
    public void LoadFromDto_BitcoinToFiat_RoundTripsValues()
    {
        var fromId = "from-btc";
        var toId = "to-fiat";
        AddBtcAccount(fromId, "BTC");
        AddFiatAccount(toId, "Checking", FiatCurrency.Brl);

        var dto = new BitcoinToFiatTransferDto
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            FromSatsAmount = 200_000,
            ToFiatAmount = 800m
        };

        var values = _builder.LoadFromDto(dto, _accounts);

        Assert.That(values.SelectedMode, Is.EqualTo(TransactionTypes.Transfer));
        Assert.That(values.ToAccount, Is.EqualTo(_accounts[1]));
        Assert.That(values.FromAccountBtcValue, Is.EqualTo(BtcValue.New(200_000)));
        Assert.That(values.ToAccountFiatValue, Is.EqualTo(FiatValue.New(800m)));
    }

    #endregion
}
