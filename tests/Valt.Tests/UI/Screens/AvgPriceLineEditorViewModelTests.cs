using NSubstitute;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.AvgPrice.Queries.DTOs;
using Valt.Tests.Builders;
using Valt.UI.Views.Main.Modals.AvgPriceLineEditor;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class AvgPriceLineEditorViewModelTests
{
    private IAvgPriceRepository _avgPriceRepository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public void SetUp()
    {
        _avgPriceRepository = Substitute.For<IAvgPriceRepository>();
    }

    private AvgPriceLineEditorViewModel CreateViewModel()
    {
        return new AvgPriceLineEditorViewModel(_avgPriceRepository);
    }

    #region Initial State Tests

    [Test]
    public void Should_Be_In_Insert_Mode_When_No_Line_Provided()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.LineId, Is.Null);
        Assert.That(viewModel.IsInsertMode, Is.True);
        Assert.That(viewModel.IsEditMode, Is.False);
    }

    [Test]
    public void Should_Default_To_Buy_Line_Type()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Buy));
        Assert.That(viewModel.IsBuy, Is.True);
        Assert.That(viewModel.IsSell, Is.False);
        Assert.That(viewModel.IsSetup, Is.False);
    }

    [Test]
    public void Should_Have_Empty_Default_Values()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.Quantity, Is.EqualTo(0));
        Assert.That(viewModel.Comment, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Line Type Selection Tests

    [Test]
    public void Should_Switch_To_Buy_When_SetBuy_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SetSellCommand.Execute(null);

        // Act
        viewModel.SetBuyCommand.Execute(null);

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Buy));
        Assert.That(viewModel.IsBuy, Is.True);
    }

    [Test]
    public void Should_Switch_To_Sell_When_SetSell_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetSellCommand.Execute(null);

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Sell));
        Assert.That(viewModel.IsSell, Is.True);
    }

    [Test]
    public void Should_Switch_To_Setup_When_SetSetup_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetSetupCommand.Execute(null);

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Setup));
        Assert.That(viewModel.IsSetup, Is.True);
    }

    [Test]
    public void Should_Show_AvgCost_Label_When_Setup_Type_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetSetupCommand.Execute(null);

        // Assert
        Assert.That(viewModel.AmountLabel, Does.Contain("Cost"));
    }

    [Test]
    public void Should_Show_Amount_Label_When_Buy_Type_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SetSetupCommand.Execute(null);

        // Act
        viewModel.SetBuyCommand.Execute(null);

        // Assert
        Assert.That(viewModel.AmountLabel, Does.Contain("Amount"));
    }

    #endregion

    #region Bind Parameter Tests - Edit Mode

    [Test]
    public async Task Should_Load_Line_Data_When_ExistingLine_Provided()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var lineId = new AvgPriceLineId();
        var existingLine = new AvgPriceLineDTO(
            lineId.Value,
            new DateOnly(2024, 6, 15),
            1,
            (int)AvgPriceLineTypes.Buy,
            1.5m,
            75000m,
            "Test comment",
            50000m,
            75000m,
            1.5m);

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = new AvgPriceProfileId().Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false,
            ExistingLine = existingLine
        };

        viewModel.Parameter = request;

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.LineId, Is.EqualTo(lineId.Value));
        Assert.That(viewModel.IsEditMode, Is.True);
        Assert.That(viewModel.IsInsertMode, Is.False);
        Assert.That(viewModel.Date!.Value.Date, Is.EqualTo(new DateTime(2024, 6, 15)));
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Buy));
        Assert.That(viewModel.Quantity, Is.EqualTo(1.5m));
        Assert.That(viewModel.Amount!.Value, Is.EqualTo(75000m));
        Assert.That(viewModel.Comment, Is.EqualTo("Test comment"));
    }

    [Test]
    public async Task Should_Load_Sell_Line_Type_Correctly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var existingLine = new AvgPriceLineDTO(
            new AvgPriceLineId().Value,
            new DateOnly(2024, 6, 15),
            1,
            (int)AvgPriceLineTypes.Sell,
            0.5m,
            60000m,
            "Sold some",
            50000m,
            25000m,
            0.5m);

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = new AvgPriceProfileId().Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false,
            ExistingLine = existingLine
        };

        viewModel.Parameter = request;

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Sell));
        Assert.That(viewModel.IsSell, Is.True);
    }

    [Test]
    public async Task Should_Load_Setup_Line_Type_Correctly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var existingLine = new AvgPriceLineDTO(
            new AvgPriceLineId().Value,
            new DateOnly(2024, 1, 1),
            1,
            (int)AvgPriceLineTypes.Setup,
            2m,
            45000m,
            "Initial setup",
            45000m,
            90000m,
            2m);

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = new AvgPriceProfileId().Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false,
            ExistingLine = existingLine
        };

        viewModel.Parameter = request;

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.LineType, Is.EqualTo(AvgPriceLineTypes.Setup));
        Assert.That(viewModel.IsSetup, Is.True);
    }

    [Test]
    public async Task Should_Set_Profile_Context_From_Request()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = profileId.Value,
            AssetName = "ETH",
            AssetPrecision = 18,
            CurrencySymbol = "R$",
            CurrencySymbolOnRight = true
        };

        viewModel.Parameter = request;

        // Act
        await viewModel.OnBindParameterAsync();

        // Assert
        Assert.That(viewModel.ProfileId, Is.EqualTo(profileId.Value));
        Assert.That(viewModel.AssetName, Is.EqualTo("ETH"));
        Assert.That(viewModel.AssetPrecision, Is.EqualTo(18));
        Assert.That(viewModel.CurrencySymbol, Is.EqualTo("R$"));
        Assert.That(viewModel.CurrencySymbolOnRight, Is.True);
    }

    #endregion

    #region Save Tests - Insert Mode

    [Test]
    public async Task Should_Add_New_Line_When_In_Insert_Mode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var profile = AvgPriceProfileBuilder.AProfile()
            .WithId(profileId)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        _avgPriceRepository.GetAvgPriceProfileByIdAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(Task.FromResult<AvgPriceProfile?>(profile));

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = profileId.Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false
        };

        viewModel.Parameter = request;
        await viewModel.OnBindParameterAsync();

        // Set form values
        viewModel.Date = new DateTime(2024, 6, 15);
        viewModel.LineType = AvgPriceLineTypes.Buy;
        viewModel.Quantity = 1m;
        viewModel.Amount = FiatValue.New(50000m);
        viewModel.Comment = "New buy";

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        await _avgPriceRepository.Received(1).SaveAvgPriceProfileAsync(Arg.Any<AvgPriceProfile>());
    }

    #endregion

    #region Save Tests - Edit Mode

    [Test]
    public async Task Should_Remove_Old_Line_And_Add_New_When_Editing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var profile = AvgPriceProfileBuilder.AProfile()
            .WithId(profileId)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        // Add an existing line to the profile
        profile.AddLine(new DateOnly(2024, 6, 10), 1, AvgPriceLineTypes.Buy, 1m, FiatValue.New(45000m), "Original");
        var existingLineId = profile.AvgPriceLines.First().Id;

        _avgPriceRepository.GetAvgPriceProfileByIdAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(Task.FromResult<AvgPriceProfile?>(profile));

        var existingLineDto = new AvgPriceLineDTO(
            existingLineId.Value,
            new DateOnly(2024, 6, 10),
            1,
            (int)AvgPriceLineTypes.Buy,
            1m,
            45000m,
            "Original",
            45000m,
            45000m,
            1m);

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = profileId.Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false,
            ExistingLine = existingLineDto
        };

        viewModel.Parameter = request;
        await viewModel.OnBindParameterAsync();

        // Update form values
        viewModel.Date = new DateTime(2024, 6, 15);
        viewModel.Quantity = 2m;
        viewModel.Amount = FiatValue.New(90000m);
        viewModel.Comment = "Updated buy";

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert
        await _avgPriceRepository.Received(1).SaveAvgPriceProfileAsync(Arg.Any<AvgPriceProfile>());
    }

    #endregion

    #region Validation Tests

    [Test]
    public async Task Should_Not_Save_When_Quantity_Is_Zero()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var profile = AvgPriceProfileBuilder.AProfile()
            .WithId(profileId)
            .WithCurrency(FiatCurrency.Usd)
            .Build();

        _avgPriceRepository.GetAvgPriceProfileByIdAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(Task.FromResult<AvgPriceProfile?>(profile));

        var request = new AvgPriceLineEditorViewModel.Request
        {
            ProfileId = profileId.Value,
            AssetName = "BTC",
            AssetPrecision = 8,
            CurrencySymbol = "$",
            CurrencySymbolOnRight = false
        };

        viewModel.Parameter = request;
        await viewModel.OnBindParameterAsync();

        // Set invalid form values
        viewModel.Quantity = 0;
        viewModel.Amount = FiatValue.New(50000m);

        // Act
        await viewModel.OkCommand.ExecuteAsync(null);

        // Assert - Should not save due to validation errors
        await _avgPriceRepository.DidNotReceive().SaveAvgPriceProfileAsync(Arg.Any<AvgPriceProfile>());
        Assert.That(viewModel.HasErrors, Is.True);
    }

    #endregion
}
