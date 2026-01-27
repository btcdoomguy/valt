using System.Drawing;
using NSubstitute;
using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.Commands.CreateProfile;
using Valt.App.Modules.AvgPrice.Commands.DeleteProfile;
using Valt.App.Modules.AvgPrice.Commands.EditProfile;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.App.Modules.AvgPrice.Queries.GetLinesOfProfile;
using Valt.App.Modules.AvgPrice.Queries.GetProfile;
using Valt.App.Modules.AvgPrice.Queries.GetProfiles;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Services;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;
using Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles.Models;

namespace Valt.Tests.UI.Screens;

[TestFixture]
public class ManageAvgPriceProfilesViewModelTests : DatabaseTest
{
    private IModalFactory _modalFactory = null!;
    private ICommandDispatcher _commandDispatcher = null!;
    private IQueryDispatcher _queryDispatcher = null!;
    private ConfigurationManager _configurationManager = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IdGenerator.Configure(new LiteDbIdProvider());
    }

    [SetUp]
    public new void SetUp()
    {
        base.SetUp();
        _modalFactory = Substitute.For<IModalFactory>();
        _commandDispatcher = Substitute.For<ICommandDispatcher>();
        _queryDispatcher = Substitute.For<IQueryDispatcher>();
        _configurationManager = new ConfigurationManager(_localDatabase);

        // Default setup: return empty list of profiles
        _queryDispatcher.DispatchAsync(Arg.Any<GetProfilesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AvgPriceProfileDTO>>(new List<AvgPriceProfileDTO>()));

        // Default setup: return empty list of lines
        _queryDispatcher.DispatchAsync(Arg.Any<GetLinesOfProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AvgPriceLineDTO>>(new List<AvgPriceLineDTO>()));

        // Default command results
        _commandDispatcher.DispatchAsync(Arg.Any<CreateProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateProfileResult>.Success(new CreateProfileResult("new-profile-id")));

        _commandDispatcher.DispatchAsync(Arg.Any<EditProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<EditProfileResult>.Success(new EditProfileResult()));

        _commandDispatcher.DispatchAsync(Arg.Any<DeleteProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeleteProfileResult>.Success(new DeleteProfileResult()));
    }

    private ManageAvgPriceProfilesViewModel CreateViewModel()
    {
        return new ManageAvgPriceProfilesViewModel(_modalFactory, _commandDispatcher, _queryDispatcher, _configurationManager);
    }

    #region State Tests - EditMode and View States

    [Test]
    public void Should_Be_In_Adding_Mode_When_No_Profile_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Null);
        Assert.That(viewModel.IsAdding, Is.True);
        Assert.That(viewModel.IsViewing, Is.False);
        Assert.That(viewModel.IsEditing, Is.False);
        Assert.That(viewModel.EditFields, Is.True);
    }

    [Test]
    public void Should_Be_In_Viewing_Mode_When_Profile_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var profile = CreateProfileItem(profileId.Value, "Test Profile");

        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(CreateProfileDTO(profileId.Value, "Test Profile")));

        // Act
        viewModel.SelectedAveragePriceProfile = profile;

        // Assert
        Assert.That(viewModel.IsAdding, Is.False);
        Assert.That(viewModel.IsViewing, Is.True);
        Assert.That(viewModel.IsEditing, Is.False);
        Assert.That(viewModel.EditFields, Is.False);
        Assert.That(viewModel.EditMode, Is.False);
    }

    [Test]
    public void Should_Switch_To_Editing_Mode_When_Edit_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var profile = CreateProfileItem(profileId.Value, "Test Profile");

        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(CreateProfileDTO(profileId.Value, "Test Profile")));

        viewModel.SelectedAveragePriceProfile = profile;

        // Act
        viewModel.EditCommand.Execute(null);

        // Assert
        Assert.That(viewModel.IsAdding, Is.False);
        Assert.That(viewModel.IsViewing, Is.False);
        Assert.That(viewModel.IsEditing, Is.True);
        Assert.That(viewModel.EditFields, Is.True);
        Assert.That(viewModel.EditMode, Is.True);
    }

    [Test]
    public void Should_Return_To_Viewing_Mode_When_Cancel_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var profile = CreateProfileItem(profileId.Value, "Test Profile");

        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(CreateProfileDTO(profileId.Value, "Test Profile")));

        viewModel.SelectedAveragePriceProfile = profile;
        viewModel.EditCommand.Execute(null);

        // Verify we're in edit mode first
        Assert.That(viewModel.IsEditing, Is.True);

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.That(viewModel.IsViewing, Is.True);
        Assert.That(viewModel.IsEditing, Is.False);
        Assert.That(viewModel.EditMode, Is.False);
    }

    [Test]
    public async Task Should_Clear_Selection_And_Return_To_Adding_Mode_When_AddNew_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var profile = CreateProfileItem(profileId.Value, "Test Profile");

        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(CreateProfileDTO(profileId.Value, "Test Profile")));

        viewModel.SelectedAveragePriceProfile = profile;

        // Verify we're in viewing mode first
        Assert.That(viewModel.IsViewing, Is.True);

        // Act
        await viewModel.AddNewCommand.ExecuteAsync(null);

        // Assert
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Null);
        Assert.That(viewModel.IsAdding, Is.True);
        Assert.That(viewModel.IsViewing, Is.False);
        Assert.That(viewModel.IsEditing, Is.False);
        Assert.That(viewModel.EditMode, Is.False);
    }

    [Test]
    public void Should_Not_Enable_Edit_Mode_When_No_Profile_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Verify no profile is selected
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Null);

        // Act
        viewModel.EditCommand.Execute(null);

        // Assert - should remain in adding mode
        Assert.That(viewModel.IsAdding, Is.True);
        Assert.That(viewModel.EditMode, Is.False);
    }

    #endregion

    #region Asset Type Tests

    [Test]
    public void Should_Be_Bitcoin_When_AssetName_Is_BTC_And_Precision_Is_8()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AssetName = "BTC";
        viewModel.Precision = 8;

        // Assert
        Assert.That(viewModel.IsBitcoin, Is.True);
        Assert.That(viewModel.IsCustomAsset, Is.False);
    }

    [Test]
    public void Should_Be_CustomAsset_When_AssetName_Is_Not_BTC()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AssetName = "ETH";
        viewModel.Precision = 18;

        // Assert
        Assert.That(viewModel.IsBitcoin, Is.False);
        Assert.That(viewModel.IsCustomAsset, Is.True);
    }

    [Test]
    public void Should_Be_CustomAsset_When_Precision_Is_Not_8()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AssetName = "BTC";
        viewModel.Precision = 6;

        // Assert
        Assert.That(viewModel.IsBitcoin, Is.False);
        Assert.That(viewModel.IsCustomAsset, Is.True);
    }

    [Test]
    public void Should_Set_Bitcoin_Defaults_When_SetBitcoin_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AssetName = "ETH";
        viewModel.Precision = 18;

        // Act
        viewModel.SetBitcoinCommand.Execute(null);

        // Assert
        Assert.That(viewModel.AssetName, Is.EqualTo("BTC"));
        Assert.That(viewModel.Precision, Is.EqualTo(8));
        Assert.That(viewModel.IsBitcoin, Is.True);
    }

    [Test]
    public void Should_Set_CustomAsset_Defaults_When_SetCustomAsset_Command_Executed()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AssetName = "BTC";
        viewModel.Precision = 8;

        // Act
        viewModel.SetCustomAssetCommand.Execute(null);

        // Assert
        Assert.That(viewModel.AssetName, Is.EqualTo(string.Empty));
        Assert.That(viewModel.Precision, Is.EqualTo(2));
        Assert.That(viewModel.IsCustomAsset, Is.True);
    }

    #endregion

    #region Adding Profile Tests

    [Test]
    public async Task Should_Save_New_Profile_When_In_Adding_Mode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Name = "New Bitcoin Profile";
        viewModel.AssetName = "BTC";
        viewModel.Precision = 8;
        viewModel.Currency = "USD";
        viewModel.Visible = true;
        viewModel.Icon = new Icon("material", "btc", '\ue0a0', Color.Orange);
        viewModel.SelectedStrategy = viewModel.AvailableStrategies.First();

        // Verify we're in adding mode
        Assert.That(viewModel.IsAdding, Is.True);
        Assert.That(viewModel.Id, Is.Null);

        // Act
        await viewModel.SaveChangesCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<CreateProfileCommand>(cmd =>
                cmd.Name == "New Bitcoin Profile" &&
                cmd.AssetName == "BTC" &&
                cmd.Precision == 8 &&
                cmd.CurrencyCode == "USD" &&
                cmd.Visible == true),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Editing Profile Tests

    [Test]
    public async Task Should_Load_Profile_Data_When_Profile_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var expectedName = "My Bitcoin Profile";
        var expectedAssetName = "BTC";
        var expectedPrecision = 8;

        var profileDto = CreateProfileDTO(profileId.Value, expectedName, expectedAssetName, expectedPrecision);
        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(profileDto));

        var profile = CreateProfileItem(profileId.Value, expectedName, expectedAssetName);

        // Act
        viewModel.SelectedAveragePriceProfile = profile;

        // Wait for async load
        await Task.Delay(100);

        // Assert
        Assert.That(viewModel.Id, Is.EqualTo(profileId.Value));
        Assert.That(viewModel.Name, Is.EqualTo(expectedName));
        Assert.That(viewModel.AssetName, Is.EqualTo(expectedAssetName));
        Assert.That(viewModel.Precision, Is.EqualTo(expectedPrecision));
    }

    [Test]
    public async Task Should_Update_Existing_Profile_When_In_Editing_Mode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();
        var originalName = "Original Name";

        var profileDto = CreateProfileDTO(profileId.Value, originalName);
        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(profileDto));

        var profile = CreateProfileItem(profileId.Value, originalName);
        viewModel.SelectedAveragePriceProfile = profile;

        // Wait for async load
        await Task.Delay(100);

        // Enter edit mode
        viewModel.EditCommand.Execute(null);
        Assert.That(viewModel.IsEditing, Is.True);

        // Act - Change name and save
        viewModel.Name = "Updated Name";
        viewModel.Icon = new Icon("material", "btc", '\ue0a0', Color.Orange);
        await viewModel.SaveChangesCommand.ExecuteAsync(null);

        // Assert
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<EditProfileCommand>(cmd =>
                cmd.ProfileId == profileId.Value &&
                cmd.Name == "Updated Name"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Deleting Profile Tests

    [Test]
    public async Task Should_Not_Delete_When_No_Profile_Selected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Verify no profile is selected
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Null);

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert - Command dispatcher should not be called for delete
        await _commandDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<DeleteProfileCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_Delete_Profile_Without_Lines_Without_Confirmation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var profileDto = CreateProfileDTO(profileId.Value, "Test Profile");
        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(profileDto));

        // No lines for this profile (default setup returns empty list)

        var profile = CreateProfileItem(profileId.Value, "Test Profile");
        viewModel.SelectedAveragePriceProfile = profile;

        // Wait for async load
        await Task.Delay(100);

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert - Should delete without confirmation dialog (no lines)
        await _commandDispatcher.Received(1).DispatchAsync(
            Arg.Is<DeleteProfileCommand>(cmd => cmd.ProfileId == profileId.Value),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Should_Clear_Selection_After_Successful_Delete()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profileId = new AvgPriceProfileId();

        var profileDto = CreateProfileDTO(profileId.Value, "Test Profile");
        _queryDispatcher.DispatchAsync(Arg.Any<GetProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AvgPriceProfileDTO?>(profileDto));

        // No lines for this profile (default setup returns empty list)

        var profile = CreateProfileItem(profileId.Value, "Test Profile");
        viewModel.SelectedAveragePriceProfile = profile;

        // Wait for async load
        await Task.Delay(100);

        // Verify profile is selected
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Not.Null);

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert - Selection should be cleared after delete
        Assert.That(viewModel.SelectedAveragePriceProfile, Is.Null);
        Assert.That(viewModel.IsAdding, Is.True);
    }

    #endregion

    #region Helper Methods

    private static AveragePriceProfileItem CreateProfileItem(
        string id,
        string name,
        string assetName = "BTC")
    {
        return new AveragePriceProfileItem(
            id,
            name,
            assetName,
            Icon.Empty.Unicode,
            Icon.Empty.Color);
    }

    private static AvgPriceProfileDTO CreateProfileDTO(
        string id,
        string name,
        string assetName = "BTC",
        int precision = 8,
        bool visible = true,
        string currencyCode = "USD")
    {
        return new AvgPriceProfileDTO(
            id,
            name,
            assetName,
            precision,
            visible,
            Icon.Empty.ToString(),
            Icon.Empty.Unicode,
            Icon.Empty.Color,
            currencyCode,
            (int)AvgPriceCalculationMethod.BrazilianRule);
    }

    #endregion
}
