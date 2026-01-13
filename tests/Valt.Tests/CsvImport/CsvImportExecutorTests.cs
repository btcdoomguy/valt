using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Core.Modules.Budget.Transactions.Details;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Services.CsvImport;

namespace Valt.Tests.CsvImport;

/// <summary>
/// Tests for the CsvImportExecutor service.
/// Verifies account/category creation and all transaction detail types.
/// </summary>
[TestFixture]
public class CsvImportExecutorTests
{
    private IAccountRepository _accountRepository = null!;
    private ICategoryRepository _categoryRepository = null!;
    private ITransactionRepository _transactionRepository = null!;
    private IConfigurationManager _configurationManager = null!;
    private CsvImportExecutor _executor = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _categoryRepository = Substitute.For<ICategoryRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _configurationManager = Substitute.For<IConfigurationManager>();
        _executor = new CsvImportExecutor(_accountRepository, _categoryRepository, _transactionRepository, _configurationManager);
    }

    #region Account Creation Tests

    [Test]
    public async Task Should_Create_New_Fiat_Account_When_IsNew()
    {
        // Arrange
        var row = CreateCsvRow("Checking [USD]", null, 100m, "Salary", "Income");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", null, IsNew: true, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", null, IsNew: true)
        };

        FiatAccount? savedAccount = null;
        _accountRepository
            .When(x => x.SaveAccountAsync(Arg.Any<FiatAccount>()))
            .Do(x => savedAccount = x.Arg<FiatAccount>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.AccountsCreated, Is.EqualTo(1));
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<FiatAccount>());
        Assert.That(savedAccount, Is.Not.Null);
        Assert.That(savedAccount!.Name.Value, Is.EqualTo("Checking"));
        Assert.That(savedAccount.FiatCurrency, Is.EqualTo(FiatCurrency.Usd));
    }

    [Test]
    public async Task Should_Create_New_Btc_Account_When_IsNew()
    {
        // Arrange
        var row = CreateCsvRow("Wallet [btc]", null, 0.001m, "Mining Reward", "Income"); // 0.001 BTC = 100k sats
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet [btc]", null, IsNew: true, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", null, IsNew: true)
        };

        BtcAccount? savedAccount = null;
        _accountRepository
            .When(x => x.SaveAccountAsync(Arg.Any<BtcAccount>()))
            .Do(x => savedAccount = x.Arg<BtcAccount>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.AccountsCreated, Is.EqualTo(1));
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<BtcAccount>());
        Assert.That(savedAccount, Is.Not.Null);
        Assert.That(savedAccount!.Name.Value, Is.EqualTo("Wallet"));
    }

    [Test]
    public async Task Should_Use_Existing_Account_Id_When_Not_New()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, -100m, "Grocery", "Food");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", IdGenerator.Generate(), IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.AccountsCreated, Is.EqualTo(0));
        await _accountRepository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as FiatDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.FiatAccountId.Value, Is.EqualTo(existingAccountId));
    }

    #endregion

    #region Category Creation Tests

    [Test]
    public async Task Should_Create_New_Category_When_IsNew()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, -100m, "Grocery shopping", "Groceries");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Groceries", null, IsNew: true)
        };

        Category? savedCategory = null;
        _categoryRepository
            .When(x => x.SaveCategoryAsync(Arg.Any<Category>()))
            .Do(x => savedCategory = x.Arg<Category>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.CategoriesCreated, Is.EqualTo(1));
        await _categoryRepository.Received(1).SaveCategoryAsync(Arg.Any<Category>());
        Assert.That(savedCategory, Is.Not.Null);
        Assert.That(savedCategory!.Name.Value, Is.EqualTo("Groceries"));
    }

    #endregion

    #region Transaction Type Tests

    [Test]
    public async Task Should_Create_FiatDetails_Transaction_For_Single_Fiat_Account()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, -100m, "Grocery shopping", "Food");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionsCreated, Is.EqualTo(1));
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as FiatDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.Credit, Is.False);
        Assert.That(details.Amount.Value, Is.EqualTo(100m));
    }

    [Test]
    public async Task Should_Create_FiatDetails_Credit_For_Positive_Amount()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, 500m, "Salary", "Income");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as FiatDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.Credit, Is.True);
        Assert.That(details.Amount.Value, Is.EqualTo(500m));
    }

    [Test]
    public async Task Should_Create_BitcoinDetails_Transaction_For_Single_Btc_Account()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Wallet [btc]", null, 0.001m, "Mining reward", "Income"); // 0.001 BTC = 100k sats
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet [btc]", existingAccountId, IsNew: false, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as BitcoinDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.Credit, Is.True);
        Assert.That(details.Amount.Sats, Is.EqualTo(100_000)); // 0.001 BTC = 100k sats
    }

    [Test]
    public async Task Should_Create_FiatToFiat_Transfer()
    {
        // Arrange
        var checkingAccountId = IdGenerator.Generate();
        var savingsAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", "Savings [USD]", -500m, "Transfer to savings", "Transfer", 500m);
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", checkingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD"),
            new("Savings [USD]", savingsAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Transfer", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as FiatToFiatDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.FromAccountId.Value, Is.EqualTo(checkingAccountId));
        Assert.That(details.ToAccountId.Value, Is.EqualTo(savingsAccountId));
        Assert.That(details.FromAmount.Value, Is.EqualTo(500m));
    }

    [Test]
    public async Task Should_Create_FiatToBitcoin_Exchange()
    {
        // Arrange
        var checkingAccountId = IdGenerator.Generate();
        var walletAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", "Wallet [btc]", -1000m, "Buy Bitcoin", "Investment", 0.01m); // 0.01 BTC
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", checkingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD"),
            new("Wallet [btc]", walletAccountId, IsNew: false, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Investment", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as FiatToBitcoinDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.FromAccountId.Value, Is.EqualTo(checkingAccountId));
        Assert.That(details.ToAccountId.Value, Is.EqualTo(walletAccountId));
        Assert.That(details.FromAmount.Value, Is.EqualTo(1000m));
        Assert.That(details.ToAmount.Sats, Is.EqualTo(1_000_000)); // 0.01 BTC = 1M sats
    }

    [Test]
    public async Task Should_Create_BitcoinToFiat_Exchange()
    {
        // Arrange
        var walletAccountId = IdGenerator.Generate();
        var checkingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Wallet [btc]", "Checking [USD]", -0.01m, "Sell Bitcoin", "Investment", 1000m); // 0.01 BTC sold for $1000
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet [btc]", walletAccountId, IsNew: false, IsBtcAccount: true, Currency: null),
            new("Checking [USD]", checkingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Investment", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as BitcoinToFiatDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.FromAccountId.Value, Is.EqualTo(walletAccountId));
        Assert.That(details.ToAccountId.Value, Is.EqualTo(checkingAccountId));
        Assert.That(details.FromAmount.Sats, Is.EqualTo(1_000_000));
        Assert.That(details.ToAmount.Value, Is.EqualTo(1000m));
    }

    [Test]
    public async Task Should_Create_BitcoinToBitcoin_Transfer()
    {
        // Arrange
        var wallet1AccountId = IdGenerator.Generate();
        var wallet2AccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Wallet1 [btc]", "Wallet2 [btc]", -0.001m, "Move to cold storage", "Transfer"); // 0.001 BTC = 100k sats
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet1 [btc]", wallet1AccountId, IsNew: false, IsBtcAccount: true, Currency: null),
            new("Wallet2 [btc]", wallet2AccountId, IsNew: false, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Transfer", existingCategoryId, IsNew: false)
        };

        Transaction? savedTransaction = null;
        _transactionRepository
            .When(x => x.SaveTransactionAsync(Arg.Any<Transaction>()))
            .Do(x => savedTransaction = x.Arg<Transaction>());

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(savedTransaction, Is.Not.Null);
        var details = savedTransaction!.TransactionDetails as BitcoinToBitcoinDetails;
        Assert.That(details, Is.Not.Null);
        Assert.That(details!.FromAccountId.Value, Is.EqualTo(wallet1AccountId));
        Assert.That(details.ToAccountId.Value, Is.EqualTo(wallet2AccountId));
        Assert.That(details.Amount.Sats, Is.EqualTo(100_000)); // 0.001 BTC = 100k sats
    }

    #endregion

    #region Progress and Error Handling Tests

    [Test]
    public async Task Should_Report_Progress_During_Import()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, -100m, "Expense 1", "Food"),
            CreateCsvRow("Checking [USD]", null, -200m, "Expense 2", "Food"),
            CreateCsvRow("Checking [USD]", null, -300m, "Expense 3", "Food")
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        var progressReports = new List<CsvImportProgress>();
        var progress = new Progress<CsvImportProgress>(p => progressReports.Add(p));

        // Act
        var result = await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages(), progress);

        // Allow async progress reporting to complete
        await Task.Delay(50);

        // Assert
        Assert.That(result.TransactionsCreated, Is.EqualTo(3));
        Assert.That(progressReports.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(progressReports.Any(p => p.CurrentAction.Contains("Importing transaction")), Is.True);
    }

    [Test]
    public async Task Should_Continue_On_Row_Error_And_Report_Partial_Success()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, -100m, "Valid expense", "Food"),
            CreateCsvRow("Unknown [USD]", null, -200m, "Invalid expense", "Food"), // Unknown account
            CreateCsvRow("Checking [USD]", null, -300m, "Another valid expense", "Food")
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
            // Note: "Unknown [USD]" is NOT in the mapping
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        // Act
        var result = await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.TransactionsCreated, Is.EqualTo(2)); // 2 valid, 1 skipped
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Does.Contain("Unknown [USD]"));
        Assert.That(result.Success, Is.True); // Partial success since some transactions were created
    }

    [Test]
    public async Task Should_Handle_Missing_Category_In_Mapping()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, -100m, "Valid expense", "Food"),
            CreateCsvRow("Checking [USD]", null, -200m, "Unknown category expense", "UnknownCategory") // Unknown category
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
            // Note: "UnknownCategory" is NOT in the mapping
        };

        // Act
        var result = await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.TransactionsCreated, Is.EqualTo(1));
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Does.Contain("UnknownCategory"));
    }

    #endregion

    #region Fiat Currency Registration Tests

    [Test]
    public async Task Should_Add_Fiat_Currency_To_Configuration_For_New_Fiat_Account()
    {
        // Arrange
        var row = CreateCsvRow("Checking [EUR]", null, 100m, "Salary", "Income");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [EUR]", null, IsNew: true, IsBtcAccount: false, Currency: "EUR")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", null, IsNew: true)
        };

        // Act
        await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        _configurationManager.Received(1).AddFiatCurrency("EUR");
    }

    [Test]
    public async Task Should_Add_Fiat_Currency_To_Configuration_For_Existing_Fiat_Account()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [BRL]", null, -100m, "Expense", "Food");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [BRL]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "BRL")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        // Act
        await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        _configurationManager.Received(1).AddFiatCurrency("BRL");
    }

    [Test]
    public async Task Should_Add_Multiple_Fiat_Currencies_To_Configuration()
    {
        // Arrange
        var usdAccountId = IdGenerator.Generate();
        var eurAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, -100m, "Expense 1", "Food"),
            CreateCsvRow("Euro Account [EUR]", null, -50m, "Expense 2", "Food")
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", usdAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD"),
            new("Euro Account [EUR]", eurAccountId, IsNew: false, IsBtcAccount: false, Currency: "EUR")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        // Act
        await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        _configurationManager.Received(1).AddFiatCurrency("USD");
        _configurationManager.Received(1).AddFiatCurrency("EUR");
    }

    [Test]
    public async Task Should_Not_Add_Duplicate_Fiat_Currencies_To_Configuration()
    {
        // Arrange
        var account1Id = IdGenerator.Generate();
        var account2Id = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, -100m, "Expense 1", "Food"),
            CreateCsvRow("Savings [USD]", null, -50m, "Expense 2", "Food")
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", account1Id, IsNew: false, IsBtcAccount: false, Currency: "USD"),
            new("Savings [USD]", account2Id, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        // Act
        await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert - Should only be called once for USD even though there are two USD accounts
        _configurationManager.Received(1).AddFiatCurrency("USD");
    }

    [Test]
    public async Task Should_Not_Call_AddFiatCurrency_For_Btc_Accounts()
    {
        // Arrange
        var row = CreateCsvRow("Wallet [btc]", null, 0.001m, "Mining", "Income"); // 0.001 BTC
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet [btc]", null, IsNew: true, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Income", null, IsNew: true)
        };

        // Act
        await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        _configurationManager.DidNotReceive().AddFiatCurrency(Arg.Any<string>());
    }

    [Test]
    public async Task Should_Add_Fiat_Currency_For_Mixed_Btc_And_Fiat_Accounts()
    {
        // Arrange
        var fiatAccountId = IdGenerator.Generate();
        var btcAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [GBP]", null, -100m, "Expense", "Food"),
            CreateCsvRow("Wallet [btc]", null, 0.001m, "Mining", "Income") // 0.001 BTC
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [GBP]", fiatAccountId, IsNew: false, IsBtcAccount: false, Currency: "GBP"),
            new("Wallet [btc]", btcAccountId, IsNew: false, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false),
            new("Income", existingCategoryId, IsNew: false)
        };

        // Act
        await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert - Only GBP should be added, not BTC
        _configurationManager.Received(1).AddFiatCurrency("GBP");
        _configurationManager.Received(1).AddFiatCurrency(Arg.Any<string>()); // Total calls should be 1
    }

    #endregion

    #region InitialValue Tests

    [Test]
    public async Task Should_Set_Fiat_Account_InitialValue_When_Category_Is_InitialValue()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, 1000m, "InitialValue", "InitialValue");
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>(); // InitialValue doesn't need mapping

        var fiatAccount = FiatAccount.Create(
            new AccountId(existingAccountId),
            AccountName.New("Checking"),
            AccountCurrencyNickname.Empty,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.Empty,
            displayOrder: 0,
            version: 0);

        _accountRepository.GetAccountByIdAsync(Arg.Any<AccountId>()).Returns(fiatAccount);

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionsCreated, Is.EqualTo(0)); // InitialValue doesn't create a transaction
        await _transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<FiatAccount>());
        Assert.That(fiatAccount.InitialAmount.Value, Is.EqualTo(1000m));
    }

    [Test]
    public async Task Should_Set_Btc_Account_InitialValue_When_Category_Is_InitialValue()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var row = CreateCsvRow("Wallet [btc]", null, 0.001m, "InitialValue", "InitialValue"); // 0.001 BTC = 100k sats
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Wallet [btc]", existingAccountId, IsNew: false, IsBtcAccount: true, Currency: null)
        };
        var categoryMappings = new List<CsvCategoryMapping>(); // InitialValue doesn't need mapping

        var btcAccount = BtcAccount.Create(
            new AccountId(existingAccountId),
            AccountName.New("Wallet"),
            AccountCurrencyNickname.Empty,
            visible: true,
            Icon.Empty,
            BtcValue.Empty,
            displayOrder: 0,
            version: 0);

        _accountRepository.GetAccountByIdAsync(Arg.Any<AccountId>()).Returns(btcAccount);

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionsCreated, Is.EqualTo(0)); // InitialValue doesn't create a transaction
        await _transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<BtcAccount>());
        Assert.That(btcAccount.InitialAmount.Sats, Is.EqualTo(100_000)); // 0.001 BTC = 100k sats
    }

    [Test]
    public async Task Should_Handle_InitialValue_Case_Insensitively()
    {
        // Arrange
        var existingAccountId = IdGenerator.Generate();
        var row = CreateCsvRow("Checking [USD]", null, 500m, "INITIALVALUE", "initialvalue"); // Mixed case
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", existingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>();

        var fiatAccount = FiatAccount.Create(
            new AccountId(existingAccountId),
            AccountName.New("Checking"),
            AccountCurrencyNickname.Empty,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.Empty,
            displayOrder: 0,
            version: 0);

        _accountRepository.GetAccountByIdAsync(Arg.Any<AccountId>()).Returns(fiatAccount);

        // Act
        var result = await _executor.ExecuteAsync(new[] { row }, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionsCreated, Is.EqualTo(0));
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<FiatAccount>());
        Assert.That(fiatAccount.InitialAmount.Value, Is.EqualTo(500m));
    }

    [Test]
    public async Task Should_Handle_Mixed_InitialValue_And_Regular_Transactions()
    {
        // Arrange
        var checkingAccountId = IdGenerator.Generate();
        var existingCategoryId = IdGenerator.Generate();
        var rows = new[]
        {
            CreateCsvRow("Checking [USD]", null, 1000m, "InitialValue", "InitialValue", lineNumber: 2),
            CreateCsvRow("Checking [USD]", null, -50m, "Grocery", "Food", lineNumber: 3)
        };
        var accountMappings = new List<CsvAccountMapping>
        {
            new("Checking [USD]", checkingAccountId, IsNew: false, IsBtcAccount: false, Currency: "USD")
        };
        var categoryMappings = new List<CsvCategoryMapping>
        {
            new("Food", existingCategoryId, IsNew: false)
        };

        var fiatAccount = FiatAccount.Create(
            new AccountId(checkingAccountId),
            AccountName.New("Checking"),
            AccountCurrencyNickname.Empty,
            visible: true,
            Icon.Empty,
            FiatCurrency.Usd,
            FiatValue.Empty,
            displayOrder: 0,
            version: 0);

        _accountRepository.GetAccountByIdAsync(Arg.Any<AccountId>()).Returns(fiatAccount);

        // Act
        var result = await _executor.ExecuteAsync(rows, accountMappings, categoryMappings, CreateTestMessages());

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionsCreated, Is.EqualTo(1)); // Only one regular transaction
        await _transactionRepository.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
        await _accountRepository.Received(1).SaveAccountAsync(Arg.Any<FiatAccount>());
        Assert.That(fiatAccount.InitialAmount.Value, Is.EqualTo(1000m));
    }

    #endregion

    #region Helper Methods

    private static CsvImportRow CreateCsvRow(
        string accountName,
        string? toAccountName,
        decimal amount,
        string description,
        string categoryName,
        decimal? toAmount = null,
        int lineNumber = 2)
    {
        return new CsvImportRow(
            new DateOnly(2024, 1, 15),
            description,
            amount,
            accountName,
            toAccountName,
            toAmount,
            categoryName,
            lineNumber);
    }

    private static CsvImportMessages CreateTestMessages()
    {
        return new CsvImportMessages(
            CreatingAccounts: "Creating accounts...",
            CreatedAccount: "Created account: {0}",
            FailedToCreateAccount: "Failed to create account '{0}': {1}",
            CreatingCategories: "Creating categories...",
            CreatedCategory: "Created category: {0}",
            FailedToCreateCategory: "Failed to create category '{0}': {1}",
            ImportingTransaction: "Importing transaction {0} of {1}...",
            AccountNotFound: "Line {0}: Account '{1}' not found in mappings",
            ToAccountNotFound: "Line {0}: To-account '{1}' not found in mappings",
            CategoryNotFound: "Line {0}: Category '{1}' not found in mappings",
            LineError: "Line {0}: {1}",
            UnableToDetermineType: "Unable to determine transaction type for row at line {0}",
            SetInitialValue: "Set initial value for account: {0}");
    }

    #endregion
}
