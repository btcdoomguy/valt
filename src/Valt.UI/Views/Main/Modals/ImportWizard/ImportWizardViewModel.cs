using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Services.CsvImport;
using Valt.UI.Base;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ImportWizard;

/// <summary>
/// Wizard steps for the import process.
/// </summary>
public enum WizardStep
{
    FileSelection = 0,
    AccountMapping = 1,
    CategoryPreview = 2,
    Summary = 3,
    Progress = 4
}

/// <summary>
/// ViewModel for the Import Wizard modal that guides users through importing transactions from CSV.
/// </summary>
public partial class ImportWizardViewModel : ValtModalViewModel
{
    private readonly ICsvImportParser? _csvImportParser;
    private readonly ICsvTemplateGenerator? _csvTemplateGenerator;
    private readonly IAccountQueries? _accountQueries;
    private readonly ICategoryQueries? _categoryQueries;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(IsOnProgressStep))]
    private WizardStep _currentStep = WizardStep.FileSelection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFileSelected))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string? _selectedFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private CsvImportResult? _parseResult;

    [ObservableProperty]
    private int _validRowCount;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private ObservableCollection<string> _parseErrors = new();

    /// <summary>
    /// Returns true if a file has been selected.
    /// </summary>
    public bool IsFileSelected => !string.IsNullOrEmpty(SelectedFilePath);

    /// <summary>
    /// Returns true if the user can navigate to the previous step.
    /// </summary>
    public bool CanGoBack => CurrentStep > WizardStep.FileSelection && CurrentStep < WizardStep.Progress;

    /// <summary>
    /// Returns true if the user can navigate to the next step.
    /// </summary>
    public bool CanGoNext => CurrentStep switch
    {
        WizardStep.FileSelection => ParseResult is not null && ParseResult.Rows.Count > 0,
        WizardStep.AccountMapping => true,
        WizardStep.CategoryPreview => true,
        WizardStep.Summary => true,
        WizardStep.Progress => false,
        _ => false
    };

    /// <summary>
    /// Returns the text for the Next button based on the current step.
    /// </summary>
    public string NextButtonText => CurrentStep == WizardStep.Summary
        ? language.ImportWizard_Import
        : language.ImportWizard_Next;

    /// <summary>
    /// Returns true if we are on the Progress step (import in progress).
    /// </summary>
    public bool IsOnProgressStep => CurrentStep == WizardStep.Progress;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    public ImportWizardViewModel()
    {
        // Design-time defaults
        CurrentStep = WizardStep.FileSelection;
    }

    /// <summary>
    /// DI constructor with required services.
    /// </summary>
    public ImportWizardViewModel(
        ICsvImportParser csvImportParser,
        ICsvTemplateGenerator csvTemplateGenerator,
        IAccountQueries accountQueries,
        ICategoryQueries categoryQueries)
    {
        _csvImportParser = csvImportParser;
        _csvTemplateGenerator = csvTemplateGenerator;
        _accountQueries = accountQueries;
        _categoryQueries = categoryQueries;
    }

    /// <summary>
    /// Opens a file picker to select a CSV file and parses it.
    /// </summary>
    [RelayCommand]
    private async Task SelectFile()
    {
        if (OwnerWindow is null)
            return;

        var options = new FilePickerOpenOptions
        {
            Title = "Select CSV File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV Files")
                {
                    Patterns = ["*.csv"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            ]
        };

        var result = await OwnerWindow.StorageProvider.OpenFilePickerAsync(options);

        if (result.Count == 0)
            return;

        var filePath = result[0].Path.LocalPath;
        SelectedFilePath = filePath;

        ParseFile();
    }

    /// <summary>
    /// Downloads the CSV template file.
    /// </summary>
    [RelayCommand]
    private async Task DownloadTemplate()
    {
        if (OwnerWindow is null || _csvTemplateGenerator is null)
            return;

        var options = new FilePickerSaveOptions
        {
            Title = "Download Template",
            SuggestedFileName = "valt-import-template.csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files")
                {
                    Patterns = ["*.csv"]
                }
            ]
        };

        var result = await OwnerWindow.StorageProvider.SaveFilePickerAsync(options);

        if (result is null)
            return;

        var filePath = result.Path.LocalPath;
        var template = _csvTemplateGenerator.GenerateTemplate();
        await File.WriteAllTextAsync(filePath, template);
    }

    /// <summary>
    /// Parses the selected CSV file.
    /// </summary>
    private void ParseFile()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || _csvImportParser is null)
            return;

        try
        {
            using var stream = File.OpenRead(SelectedFilePath);
            ParseResult = _csvImportParser.Parse(stream);

            ValidRowCount = ParseResult.Rows.Count;
            ErrorCount = ParseResult.Errors.Count;

            ParseErrors.Clear();
            foreach (var error in ParseResult.Errors)
            {
                ParseErrors.Add(error);
            }
        }
        catch (Exception ex)
        {
            ParseResult = CsvImportResult.Failure(ex.Message);
            ValidRowCount = 0;
            ErrorCount = 1;
            ParseErrors.Clear();
            ParseErrors.Add(ex.Message);
        }
    }

    /// <summary>
    /// Advances to the next step or starts the import process on the Summary step.
    /// </summary>
    [RelayCommand]
    private void GoNext()
    {
        if (!CanGoNext)
            return;

        if (CurrentStep == WizardStep.Summary)
        {
            // Start import process
            CurrentStep = WizardStep.Progress;
            // Import logic will be implemented in Phase 3
        }
        else if (CurrentStep < WizardStep.Progress)
        {
            CurrentStep++;
        }
    }

    /// <summary>
    /// Returns to the previous step.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        if (!CanGoBack)
            return;

        CurrentStep--;
    }

    /// <summary>
    /// Closes the wizard modal.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }
}
