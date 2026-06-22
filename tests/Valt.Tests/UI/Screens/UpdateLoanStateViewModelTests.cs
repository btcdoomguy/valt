using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.Commands.AddLoanStateUpdate;
using Valt.App.Modules.Assets.DTOs;
using Valt.App.Modules.Assets.Queries.GetAsset;
using Valt.App.Modules.Assets.Queries.GetLatestLoanState;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Infra.Kernel;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.UpdateLoanState;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class UpdateLoanStateViewModelTests
{
    private IQueryDispatcher _queryDispatcher;
    private ICommandDispatcher _commandDispatcher;
    private IModalFactory _modalFactory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => IdGenerator.Configure(new LiteDbIdProvider());

    [SetUp]
    public void SetUp()
    {
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _modalFactory = Substitute.For<IModalFactory>();
    }

    private UpdateLoanStateViewModel CreateViewModel()
        => new(_queryDispatcher, _commandDispatcher, _modalFactory);

    private static LoanStateDTO CreateLatestLoanState(string assetId = "asset-1") => new()
    {
        AssetId = assetId,
        AssetName = "HodlHodl Loan",
        PlatformName = "HodlHodl",
        CollateralSats = 5_000_000,
        LoanAmount = 100_000m,
        CurrencyCode = "USD",
        Apr = 0.12m,
        InitialLtv = 0.50m,
        LiquidationLtv = 0.90m,
        MarginCallLtv = 0.75m,
        Fees = 500m,
        LoanStartDate = new DateOnly(2024, 1, 1),
        RepaymentDate = new DateOnly(2025, 1, 1),
        StatusId = 0,
        CurrentBtcPriceInLoanCurrency = 100_000m,
        FixedTotalDebt = null,
        TotalBorrowed = 100_000m,
        InterestAccruedUntilDate = 4_500m,
        EffectiveDate = new DateOnly(2024, 6, 1),
        Note = "Snapshot note"
    };

    #region Prefill Tests

    [Test]
    public async Task Should_Prefill_From_LatestLoanState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.AssetId, Is.EqualTo("asset-1"));
        Assert.That(viewModel.PlatformName, Is.EqualTo("HodlHodl"));
        Assert.That(viewModel.CurrencyCode, Is.EqualTo("USD"));
        Assert.That(viewModel.CurrencySymbol, Is.EqualTo("$"));
        Assert.That(viewModel.SymbolOnRight, Is.False);
        Assert.That(viewModel.EffectiveDate!.Value.Date, Is.EqualTo(DateTime.Today.Date));
        Assert.That(viewModel.CurrentTotalDebtFormatted, Is.EqualTo(CurrencyDisplay.FormatFiat(105_000m, "USD")));
        Assert.That(viewModel.TotalBorrowed.Value, Is.EqualTo(100_000m));
        Assert.That(viewModel.InterestAccruedUntilDate.Value, Is.EqualTo(4_500m));
        Assert.That(viewModel.CollateralSats, Is.EqualTo(5_000_000));
        Assert.That(viewModel.AprPercentage, Is.EqualTo(12m));
        Assert.That(viewModel.Fees.Value, Is.EqualTo(500m));
        Assert.That(viewModel.Note, Is.EqualTo("Snapshot note"));
        Assert.That(viewModel.LoanAmount, Is.EqualTo(100_000m));
        Assert.That(viewModel.InitialLtv, Is.EqualTo(0.50m));
        Assert.That(viewModel.MarginCallLtv, Is.EqualTo(0.75m));
        Assert.That(viewModel.LiquidationLtv, Is.EqualTo(0.90m));
        Assert.That(viewModel.LoanStartDate!.Value.Date, Is.EqualTo(new DateTime(2024, 1, 1).Date));
        Assert.That(viewModel.RepaymentDate!.Value.Date, Is.EqualTo(new DateTime(2025, 1, 1).Date));
    }

    [Test]
    public async Task Should_Fallback_To_AssetDto_When_No_Latest_LoanState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var asset = new AssetDTO
        {
            Id = "asset-1",
            Name = "Fallback Loan",
            AssetTypeId = 6,
            AssetTypeName = "BTC Loan",
            Icon = "\xE8F5",
            IncludeInNetWorth = true,
            Visible = true,
            LastPriceUpdateAt = DateTime.Now,
            CreatedAt = DateTime.Now,
            DisplayOrder = 1,
            CurrentPrice = 100_000m,
            CurrentValue = -100_000m,
            CurrencyCode = "USD",
            PlatformName = "Fallback Platform",
            CollateralSats = 1_000_000,
            LoanAmount = 50_000m,
            Apr = 0.10m,
            Fees = 300m,
            LoanStartDate = new DateOnly(2024, 3, 1),
            RepaymentDate = null,
            TotalDebt = 50_300m,
            TotalBorrowed = 50_000m
        };

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns((LoanStateDTO?)null);
        _queryDispatcher.DispatchAsync(Arg.Any<GetAssetQuery>(), Arg.Any<CancellationToken>())
            .Returns(asset);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.PlatformName, Is.EqualTo("Fallback Platform"));
        Assert.That(viewModel.TotalBorrowed.Value, Is.EqualTo(50_000m));
        Assert.That(viewModel.InterestAccruedUntilDate.Value, Is.EqualTo(0m));
        Assert.That(viewModel.CollateralSats, Is.EqualTo(1_000_000));
        Assert.That(viewModel.AprPercentage, Is.EqualTo(10m));
        Assert.That(viewModel.Fees.Value, Is.EqualTo(300m));
        Assert.That(viewModel.LoanAmount, Is.EqualTo(50_000m));
        Assert.That(viewModel.RepaymentDate, Is.Null);
    }

    [Test]
    public async Task Should_Default_EffectiveDate_To_Today()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();
        latest = latest with { EffectiveDate = new DateOnly(2024, 1, 1) };

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.EffectiveDate!.Value.Date, Is.EqualTo(DateTime.Today.Date));
    }

    #endregion

    #region Save Tests

    [Test]
    public async Task Should_Dispatch_AddLoanStateUpdateCommand_When_Valid()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);
        _commandDispatcher.DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        viewModel.EffectiveDate = new DateTime(2024, 12, 1);
        viewModel.TotalBorrowed = FiatValue.New(105_000m);
        viewModel.InterestAccruedUntilDate = FiatValue.New(4_500m);
        viewModel.CollateralSats = 6_000_000;
        viewModel.AprPercentage = 15m;
        viewModel.Fees = FiatValue.New(600m);
        viewModel.Note = "Updated note";

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<AddLoanStateUpdateCommand>(c =>
                c.AssetId == "asset-1" &&
                c.EffectiveDate == new DateOnly(2024, 12, 1) &&
                c.TotalBorrowed == 105_000m &&
                c.InterestAccruedUntilDate == 4_500m &&
                c.CollateralSats == 6_000_000 &&
                c.Apr == 0.15m &&
                c.Fees == 600m &&
                c.Note == "Updated note"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_CloseDialog_With_Response_When_Command_Succeeds()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);
        _commandDispatcher.DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        object? receivedResponse = null;
        viewModel.CloseDialog = response => receivedResponse = response;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(receivedResponse, Is.Not.Null);
        Assert.That(receivedResponse, Is.InstanceOf<UpdateLoanStateViewModel.Response>());
        Assert.That(((UpdateLoanStateViewModel.Response)receivedResponse!).Ok, Is.True);
    }

    [Test]
    public async Task Should_Not_CloseDialog_When_Command_Fails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);
        _commandDispatcher.DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Failure(new Error("VALIDATION_FAILED", "Date must be after latest snapshot")));

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        var closeDialogCalled = false;
        viewModel.CloseDialog = _ => closeDialogCalled = true;

        // Act
        try
        {
            await viewModel.OkCommand.ExecuteAsync(null);
        }
        catch (NullReferenceException)
        {
            // Expected when GetWindow is null and ShowErrorAsync is invoked in test environment
        }

        // Assert
        Assert.That(closeDialogCalled, Is.False);
    }

    #endregion

    #region Validation Tests

    [Test]
    public async Task Should_Not_Dispatch_When_EffectiveDate_Is_Missing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        viewModel.EffectiveDate = null;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.True);
        await _commandDispatcher.DidNotReceive().DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_Not_Dispatch_When_CollateralSats_Is_Zero()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        viewModel.CollateralSats = 0;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.True);
        await _commandDispatcher.DidNotReceive().DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_Not_Dispatch_When_AprPercentage_Is_Negative()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var latest = CreateLatestLoanState();

        _queryDispatcher.DispatchAsync(Arg.Any<GetLatestLoanStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(latest);

        viewModel.Parameter = new UpdateLoanStateViewModel.Request { AssetId = "asset-1" };
        await viewModel.OnBindParameterAsync();

        viewModel.AprPercentage = -1m;

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.HasErrors, Is.True);
        await _commandDispatcher.DidNotReceive().DispatchAsync(Arg.Any<AddLoanStateUpdateCommand>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
