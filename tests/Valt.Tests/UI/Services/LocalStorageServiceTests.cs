using System.ComponentModel;
using Valt.UI.Services.LocalStorage;

namespace Valt.Tests.UI.Services;

[TestFixture]
public class LocalStorageServiceTests
{
    private string _testFilePath = null!;
    private ILocalStorageService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"valt_test_{Guid.NewGuid()}.json");
        _service = new LocalStorageService(_testFilePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    #region DataGridSettings Tests

    [Test]
    public void LoadDataGridSettings_WhenNoFileExists_ReturnsDefaultSettings()
    {
        // Act
        var settings = _service.LoadDataGridSettings();

        // Assert
        Assert.That(settings.ColumnWidths, Is.Empty);
        Assert.That(settings.ColumnOrder, Is.Empty);
        Assert.That(settings.OrderedColumn, Is.Null);
        Assert.That(settings.SortDirection, Is.Null);
    }

    [Test]
    public async Task SaveDataGridSettingsAsync_ThenLoad_ReturnsSavedSettings()
    {
        // Arrange
        var settings = new DataGridSettings
        {
            OrderedColumn = "Date",
            SortDirection = ListSortDirection.Descending,
            ColumnWidths = new Dictionary<string, double> { { "Date", 150.5 }, { "Name", 200.0 } },
            ColumnOrder = new List<string> { "Date", "Name", "Amount" }
        };

        // Act
        await _service.SaveDataGridSettingsAsync(settings);

        // Reload to ensure persistence
        var freshService = new LocalStorageService(_testFilePath);
        var loadedSettings = freshService.LoadDataGridSettings();

        // Assert
        Assert.That(loadedSettings.OrderedColumn, Is.EqualTo("Date"));
        Assert.That(loadedSettings.SortDirection, Is.EqualTo(ListSortDirection.Descending));
        Assert.That(loadedSettings.ColumnWidths["Date"], Is.EqualTo(150.5));
        Assert.That(loadedSettings.ColumnWidths["Name"], Is.EqualTo(200.0));
        Assert.That(loadedSettings.ColumnOrder, Has.Count.EqualTo(3));
        Assert.That(loadedSettings.ColumnOrder, Contains.Item("Date"));
        Assert.That(loadedSettings.ColumnOrder, Contains.Item("Name"));
        Assert.That(loadedSettings.ColumnOrder, Contains.Item("Amount"));
    }

    [Test]
    public async Task SaveDataGridSettingsAsync_PersistsColumnWidths()
    {
        // Arrange
        var settings = new DataGridSettings
        {
            ColumnWidths = new Dictionary<string, double>
            {
                { "Column1", 100.0 },
                { "Column2", 200.5 },
                { "Column3", 150.25 }
            }
        };

        // Act
        await _service.SaveDataGridSettingsAsync(settings);
        var freshService = new LocalStorageService(_testFilePath);
        var loadedSettings = freshService.LoadDataGridSettings();

        // Assert
        Assert.That(loadedSettings.ColumnWidths, Has.Count.EqualTo(3));
        Assert.That(loadedSettings.ColumnWidths["Column1"], Is.EqualTo(100.0));
        Assert.That(loadedSettings.ColumnWidths["Column2"], Is.EqualTo(200.5));
        Assert.That(loadedSettings.ColumnWidths["Column3"], Is.EqualTo(150.25));
    }

    [Test]
    public async Task SaveDataGridSettingsAsync_PersistsColumnOrder()
    {
        // Arrange
        var settings = new DataGridSettings
        {
            ColumnOrder = new List<string> { "First", "Second", "Third" }
        };

        // Act
        await _service.SaveDataGridSettingsAsync(settings);
        var freshService = new LocalStorageService(_testFilePath);
        var loadedSettings = freshService.LoadDataGridSettings();

        // Assert
        Assert.That(loadedSettings.ColumnOrder, Has.Count.EqualTo(3));
        Assert.That(loadedSettings.ColumnOrder[0], Is.EqualTo("First"));
        Assert.That(loadedSettings.ColumnOrder[1], Is.EqualTo("Second"));
        Assert.That(loadedSettings.ColumnOrder[2], Is.EqualTo("Third"));
    }

    [Test]
    public async Task SaveDataGridSettingsAsync_PersistsSortState()
    {
        // Arrange
        var settings = new DataGridSettings
        {
            OrderedColumn = "TestColumn",
            SortDirection = ListSortDirection.Ascending
        };

        // Act
        await _service.SaveDataGridSettingsAsync(settings);
        var freshService = new LocalStorageService(_testFilePath);
        var loadedSettings = freshService.LoadDataGridSettings();

        // Assert
        Assert.That(loadedSettings.OrderedColumn, Is.EqualTo("TestColumn"));
        Assert.That(loadedSettings.SortDirection, Is.EqualTo(ListSortDirection.Ascending));
    }

    #endregion

    #region Culture Tests

    [Test]
    public void LoadCulture_WhenNoFileExists_ReturnsCurrentCulture()
    {
        // Act
        var culture = _service.LoadCulture();

        // Assert
        Assert.That(culture, Is.Not.Empty);
    }

    [Test]
    public async Task ChangeCultureAsync_ThenLoad_ReturnsSavedCulture()
    {
        // Arrange
        var expectedCulture = "pt-BR";

        // Act
        await _service.ChangeCultureAsync(expectedCulture);
        var freshService = new LocalStorageService(_testFilePath);
        var loadedCulture = freshService.LoadCulture();

        // Assert
        Assert.That(loadedCulture, Is.EqualTo(expectedCulture));
    }

    #endregion

    #region Recent Files Tests

    [Test]
    public void LoadRecentFiles_WhenNoFileExists_ReturnsEmptyList()
    {
        // Act
        var files = _service.LoadRecentFiles();

        // Assert
        Assert.That(files, Is.Empty);
    }

    [Test]
    public async Task ChangeRecentFilesAsync_ThenLoad_ReturnsSavedFiles()
    {
        // Arrange
        var files = new List<string> { "/path/to/file1.db", "/path/to/file2.db" };

        // Act
        await _service.ChangeRecentFilesAsync(files);
        var freshService = new LocalStorageService(_testFilePath);
        var loadedFiles = freshService.LoadRecentFiles();

        // Assert
        Assert.That(loadedFiles, Has.Count.EqualTo(2));
        Assert.That(loadedFiles, Contains.Item("/path/to/file1.db"));
        Assert.That(loadedFiles, Contains.Item("/path/to/file2.db"));
    }

    [Test]
    public async Task ChangeRecentFilesAsync_WithEmptyList_ClearsFiles()
    {
        // Arrange
        await _service.ChangeRecentFilesAsync(new List<string> { "/path/to/file.db" });

        // Act
        await _service.ChangeRecentFilesAsync(new List<string>());
        var freshService = new LocalStorageService(_testFilePath);
        var loadedFiles = freshService.LoadRecentFiles();

        // Assert
        Assert.That(loadedFiles, Is.Empty);
    }

    #endregion
}
