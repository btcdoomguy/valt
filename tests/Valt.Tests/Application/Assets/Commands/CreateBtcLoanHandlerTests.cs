using Valt.App.Modules.Assets.Commands.CreateBtcLoan;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Application.Assets.Commands;

[TestFixture]
public class CreateBtcLoanHandlerTests : DatabaseTest
{
    private CreateBtcLoanHandler _handler = null!;

    [SetUp]
    public void SetUpHandler()
    {
        _handler = new CreateBtcLoanHandler(
            _assetRepository,
            new CreateBtcLoanValidator());
    }

    private static CreateBtcLoanCommand ValidCommand() => new()
    {
        Name = "HodlHodl Loan",
        CurrencyCode = "USD",
        PlatformName = "HodlHodl",
        CollateralSats = 100_000_000,
        LoanAmount = 25_000m,
        Apr = 0.12m,
        InitialLtv = 50m,
        LiquidationLtv = 80m,
        MarginCallLtv = 70m,
        Fees = 100m,
        LoanStartDate = new DateOnly(2025, 1, 1),
        RepaymentDate = new DateOnly(2026, 1, 1),
        CurrentBtcPrice = 50_000m,
        IncludeInNetWorth = true,
        Visible = true
    };

    [Test]
    public async Task HandleAsync_WithValidCommand_CreatesAsset()
    {
        var result = await _handler.HandleAsync(ValidCommand());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.AssetId, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_WithNoRepaymentDate_CreatesAsset()
    {
        var command = ValidCommand() with { RepaymentDate = null };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEmptyName_ReturnsValidationError()
    {
        var command = ValidCommand() with { Name = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithInvalidCurrencyCode_ReturnsError()
    {
        var command = ValidCommand() with { CurrencyCode = "INVALID" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("INVALID_CURRENCY"));
        });
    }

    [Test]
    public async Task HandleAsync_WithZeroCollateral_ReturnsValidationError()
    {
        var command = ValidCommand() with { CollateralSats = 0 };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeLoanAmount_ReturnsValidationError()
    {
        var command = ValidCommand() with { LoanAmount = -1000m };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeApr_ReturnsValidationError()
    {
        var command = ValidCommand() with { Apr = -0.01m };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithLiquidationLtvLessThanMarginCall_ReturnsValidationError()
    {
        var command = ValidCommand() with { LiquidationLtv = 60m, MarginCallLtv = 70m };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithEmptyPlatformName_ReturnsValidationError()
    {
        var command = ValidCommand() with { PlatformName = "" };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithNegativeFees_ReturnsValidationError()
    {
        var command = ValidCommand() with { Fees = -50m };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithIcon_CreatesAsset()
    {
        var command = ValidCommand() with { Icon = "trending_up_E8E5_#00FF00" };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithFixedTotalDebt_CreatesAssetAndDerivesApr()
    {
        // 25000 principal, 27500 fixed total over 365 days => 10% APR
        var command = ValidCommand() with
        {
            Apr = 0m,
            Fees = 0m,
            FixedTotalDebt = 27_500m,
            LoanStartDate = new DateOnly(2025, 1, 1),
            RepaymentDate = new DateOnly(2026, 1, 1)
        };

        var result = await _handler.HandleAsync(command);

        Assert.That(result.IsSuccess, Is.True);

        var saved = await _assetRepository.GetByIdAsync(new AssetId(result.Value!.AssetId));
        var details = (BtcLoanDetails)saved!.Details;

        Assert.Multiple(() =>
        {
            Assert.That(details.HasFixedTotalDebt, Is.True);
            Assert.That(details.FixedTotalDebt, Is.EqualTo(27_500m));
            Assert.That(details.Apr, Is.EqualTo(0.1m));
            Assert.That(details.CalculateTotalDebt(), Is.EqualTo(27_500m));
        });
    }

    [Test]
    public async Task HandleAsync_WithFixedTotalDebt_IgnoresProvidedApr()
    {
        // Even if caller supplies a bogus APR, the derived APR must be used.
        var command = ValidCommand() with
        {
            Apr = 0.99m,
            Fees = 0m,
            FixedTotalDebt = 27_500m,
            LoanStartDate = new DateOnly(2025, 1, 1),
            RepaymentDate = new DateOnly(2026, 1, 1)
        };

        var result = await _handler.HandleAsync(command);
        var saved = await _assetRepository.GetByIdAsync(new AssetId(result.Value!.AssetId));
        var details = (BtcLoanDetails)saved!.Details;

        Assert.That(details.Apr, Is.EqualTo(0.1m));
    }

    [Test]
    public async Task HandleAsync_WithFixedTotalDebt_RequiresRepaymentDate()
    {
        var command = ValidCommand() with
        {
            FixedTotalDebt = 27_500m,
            RepaymentDate = null
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithFixedTotalDebt_BelowPrincipalPlusFees_Fails()
    {
        // Principal 25000 + fees 100 = 25100. Fixed debt 25000 is below.
        var command = ValidCommand() with
        {
            Fees = 100m,
            FixedTotalDebt = 25_000m
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }

    [Test]
    public async Task HandleAsync_WithFixedTotalDebt_RepaymentBeforeStart_Fails()
    {
        var command = ValidCommand() with
        {
            FixedTotalDebt = 27_500m,
            LoanStartDate = new DateOnly(2026, 1, 1),
            RepaymentDate = new DateOnly(2025, 1, 1)
        };

        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error!.Code, Is.EqualTo("VALIDATION_FAILED"));
        });
    }
}
