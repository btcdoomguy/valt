using Avalonia;
using Avalonia.Controls;
using NSubstitute;
using Valt.UI.Base;
using Valt.UI.Services;
using Valt.UI.Views;

namespace Valt.Tests.UI.Services;

[TestFixture]
public class ModalLauncherTests
{
    private IModalFactory _modalFactory = null!;
    private TestableModalLauncher _launcher = null!;
    private Window _owner = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        AppBuilder.Configure<global::Avalonia.Application>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
    }

    [SetUp]
    public void SetUp()
    {
        _modalFactory = Substitute.For<IModalFactory>();
        _launcher = new TestableModalLauncher(_modalFactory);
        _owner = new Window();
    }

    [Test]
    public async Task ShowAsync_Should_Delegate_To_Factory_And_Show_Dialog()
    {
        // Arrange
        var modal = CreateFakeModal();
        _modalFactory.CreateAsync(ApplicationModalNames.About, _owner, null)
            .Returns(Task.FromResult<ValtBaseWindow>(modal));

        // Act
        await _launcher.ShowAsync(ApplicationModalNames.About, _owner);

        // Assert
        _ = _modalFactory.Received(1).CreateAsync(ApplicationModalNames.About, _owner, null);
        Assert.That(_launcher.LastDialogShown, Is.SameAs(modal));
    }

    [Test]
    public async Task ShowAsync_TResult_Should_Return_Typed_Result()
    {
        // Arrange
        var modal = CreateFakeModal();
        var expectedResult = new TestResult { Value = 42 };
        _modalFactory.CreateAsync(ApplicationModalNames.InputPassword, _owner, null)
            .Returns(Task.FromResult<ValtBaseWindow>(modal));
        _launcher.SetNextResult(expectedResult);

        // Act
        var result = await _launcher.ShowAsync<TestResult>(ApplicationModalNames.InputPassword, _owner);

        // Assert
        Assert.That(result, Is.SameAs(expectedResult));
        Assert.That(_launcher.LastDialogShown, Is.SameAs(modal));
    }

    [Test]
    public async Task ShowAsync_TViewModel_TResult_Should_Invoke_Configure_On_DataContext()
    {
        // Arrange
        var viewModel = new TestViewModel { Value = "configured" };
        var modal = CreateFakeModal(viewModel);
        _modalFactory.CreateAsync(ApplicationModalNames.ManageCategories, _owner, null)
            .Returns(Task.FromResult<ValtBaseWindow>(modal));

        string? capturedValue = null;

        // Act
        await _launcher.ShowAsync<TestViewModel, object?>(
            ApplicationModalNames.ManageCategories,
            _owner,
            vm => capturedValue = vm.Value);

        // Assert
        Assert.That(capturedValue, Is.EqualTo(viewModel.Value));
        Assert.That(_launcher.LastDialogShown, Is.SameAs(modal));
    }

    [Test]
    public void ShowAsync_With_Null_Modal_Should_Throw_InvalidOperationException()
    {
        // Arrange
        _modalFactory.CreateAsync(ApplicationModalNames.Tips, _owner, null)
            .Returns(Task.FromResult<ValtBaseWindow>(null!));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _launcher.ShowAsync(ApplicationModalNames.Tips, _owner));

        Assert.That(ex!.Message, Does.Contain(ApplicationModalNames.Tips.ToString()));
    }

    private static ValtBaseWindow CreateFakeModal(object? dataContext = null)
    {
        var modal = new TestWindow();
        modal.DataContext = dataContext;
        return modal;
    }

    private class TestWindow : ValtBaseWindow
    {
    }

    private class TestableModalLauncher : ModalLauncher
    {
        private object? _nextResult;

        public TestableModalLauncher(IModalFactory modalFactory) : base(modalFactory)
        {
        }

        public ValtBaseWindow? LastDialogShown { get; private set; }

        public void SetNextResult(object? result) => _nextResult = result;

        protected override Task ShowDialogAsync(ValtBaseWindow dialog, Window owner)
        {
            LastDialogShown = dialog;
            return Task.CompletedTask;
        }

        protected override Task<TResult?> ShowDialogAsync<TResult>(ValtBaseWindow dialog, Window owner)
            where TResult : default
        {
            LastDialogShown = dialog;
            return Task.FromResult((TResult?)_nextResult);
        }
    }

    private class TestViewModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class TestResult
    {
        public int Value { get; set; }
    }
}
