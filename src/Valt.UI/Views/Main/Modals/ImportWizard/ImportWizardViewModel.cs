using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Budget.Accounts.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Services.CsvImport;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Modals.ImportWizard.Models;

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
    private readonly ICsvImportExecutor? _csvImportExecutor;
    private readonly BackgroundJobManager? _backgroundJobManager;

    #region Step Navigation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(IsOnProgressStep))]
    [NotifyPropertyChangedFor(nameof(IsImportComplete))]
    private WizardStep _currentStep = WizardStep.FileSelection;

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
    /// Returns true if the import has completed.
    /// </summary>
    public bool IsImportComplete => CurrentStep == WizardStep.Progress && ImportProgress >= 100;

    #endregion

    #region Step 1 - File Selection

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

    #endregion

    #region Step 2 - Account Mapping

    [ObservableProperty]
    private ObservableCollection<AccountMappingItem> _accountMappings = new();

    [ObservableProperty]
    private int _newAccountCount;

    [ObservableProperty]
    private int _existingAccountCount;

    #endregion

    #region Step 3 - Category Preview

    [ObservableProperty]
    private ObservableCollection<CategoryMappingItem> _categoryMappings = new();

    [ObservableProperty]
    private int _newCategoryCount;

    [ObservableProperty]
    private int _existingCategoryCount;

    #endregion

    #region Step 4 - Summary

    /// <summary>
    /// Gets the total transaction count for summary display.
    /// </summary>
    public int SummaryTransactionCount => ValidRowCount;

    /// <summary>
    /// Gets the total account count (new + existing) for summary display.
    /// </summary>
    public int SummaryAccountCount => AccountMappings.Count;

    /// <summary>
    /// Gets the total category count (new + existing) for summary display.
    /// </summary>
    public int SummaryCategoryCount => CategoryMappings.Count;

    #endregion

    #region Step 5 - Progress

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImportComplete))]
    private int _importProgress;

    [ObservableProperty]
    private string _importStatusMessage = string.Empty;

    [ObservableProperty]
    private int _importedCount;

    [ObservableProperty]
    private int _totalToImport;

    #endregion

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
        ICategoryQueries categoryQueries,
        ICsvImportExecutor csvImportExecutor,
        BackgroundJobManager backgroundJobManager)
    {
        _csvImportParser = csvImportParser;
        _csvTemplateGenerator = csvTemplateGenerator;
        _accountQueries = accountQueries;
        _categoryQueries = categoryQueries;
        _csvImportExecutor = csvImportExecutor;
        _backgroundJobManager = backgroundJobManager;
    }

    #region Step 1 Commands

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

    #endregion

    #region Step 2-3 Processing

    /// <summary>
    /// Processes account and category mappings from parsed CSV data.
    /// Called when transitioning from Step 1 to Step 2.
    /// </summary>
    private async Task ProcessMappingsAsync()
    {
        if (ParseResult is null || _accountQueries is null || _categoryQueries is null)
            return;

        // Get existing accounts and categories
        var existingAccounts = (await _accountQueries.GetAccountsAsync(showHiddenAccounts: true)).ToList();
        var existingCategories = (await _categoryQueries.GetCategoriesAsync()).Items;

        // Extract unique account names from CSV (both from and to accounts)
        var csvAccountNames = ParseResult.Rows
            .SelectMany(r => new[] { r.AccountName, r.ToAccountName })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Extract unique category names from CSV
        var csvCategoryNames = ParseResult.Rows
            .Select(r => r.CategoryName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Build account mappings
        AccountMappings.Clear();
        foreach (var csvName in csvAccountNames)
        {
            // Try to match by name (case-insensitive, ignoring bracket suffix)
            var cleanName = GetCleanAccountName(csvName!);
            var existingAccount = existingAccounts.FirstOrDefault(a =>
                a.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            var mapping = AccountMappingItem.Create(csvName!, existingAccount);
            AccountMappings.Add(mapping);
        }

        NewAccountCount = AccountMappings.Count(m => m.IsNew);
        ExistingAccountCount = AccountMappings.Count(m => !m.IsNew);

        // Build category mappings
        CategoryMappings.Clear();
        foreach (var csvName in csvCategoryNames)
        {
            // Try to match by name (case-insensitive)
            var existingCategory = existingCategories.FirstOrDefault(c =>
                c.SimpleName.Equals(csvName, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Equals(csvName, StringComparison.OrdinalIgnoreCase));

            var mapping = CategoryMappingItem.Create(csvName, existingCategory);
            CategoryMappings.Add(mapping);
        }

        NewCategoryCount = CategoryMappings.Count(m => m.IsNew);
        ExistingCategoryCount = CategoryMappings.Count(m => !m.IsNew);
    }

    /// <summary>
    /// Removes the bracket suffix from account names (e.g., "Checking [USD]" -> "Checking").
    /// </summary>
    private static string GetCleanAccountName(string csvAccountName)
    {
        var bracketStart = csvAccountName.LastIndexOf('[');
        if (bracketStart > 0)
        {
            return csvAccountName.Substring(0, bracketStart).Trim();
        }
        return csvAccountName;
    }

    #endregion

    #region Step 5 - Import Process

    /// <summary>
    /// Starts the import process by executing the CSV import.
    /// </summary>
    private async Task StartImportAsync()
    {
        // Initialize progress tracking
        TotalToImport = ValidRowCount;
        ImportedCount = 0;
        ImportProgress = 0;
        ImportStatusMessage = language.ImportWizard_Importing;

        // Stop background jobs during import
        await _backgroundJobManager!.StopAll();

        try
        {
            var progress = new Progress<CsvImportProgress>(p =>
            {
                ImportProgress = p.Percentage;
                ImportedCount = p.CurrentRow;
                ImportStatusMessage = p.CurrentAction;
            });

            // Convert UI mapping items to service mapping records
            var accountMappings = AccountMappings
                .Select(m => new CsvAccountMapping(
                    m.CsvAccountName,
                    m.ExistingAccount?.Id,
                    m.IsNew,
                    m.IsBtcAccount,
                    m.Currency))
                .ToList();

            var categoryMappings = CategoryMappings
                .Select(m => new CsvCategoryMapping(
                    m.CsvCategoryName,
                    m.ExistingCategory?.Id,
                    m.IsNew))
                .ToList();

            var result = await _csvImportExecutor!.ExecuteAsync(
                ParseResult!.Rows,
                accountMappings,
                categoryMappings,
                progress);

            if (result.Success)
            {
                ImportStatusMessage = language.ImportWizard_ImportComplete;
                ImportProgress = 100;
            }
            else
            {
                ImportStatusMessage = $"Import completed with errors: {result.Errors.Count} issues";
                // Could show errors in a list, but for MVP just show count
            }
        }
        finally
        {
            // Restart background jobs (all job types)
            _backgroundJobManager.StartAllJobs(BackgroundJobTypes.App);
            _backgroundJobManager.StartAllJobs(BackgroundJobTypes.ValtDatabase);
            _backgroundJobManager.StartAllJobs(BackgroundJobTypes.PriceDatabase);
        }
    }

    /// <summary>
    /// Closes the wizard after successful import.
    /// </summary>
    [RelayCommand]
    private void CloseAfterImport()
    {
        CloseWindow?.Invoke();
    }

    #endregion

    #region Navigation Commands

    /// <summary>
    /// Advances to the next step or starts the import process on the Summary step.
    /// </summary>
    [RelayCommand]
    private async Task GoNext()
    {
        if (!CanGoNext)
            return;

        // Process mappings when leaving Step 1
        if (CurrentStep == WizardStep.FileSelection)
        {
            await ProcessMappingsAsync();
        }

        if (CurrentStep == WizardStep.Summary)
        {
            // Start import process
            CurrentStep = WizardStep.Progress;
            await StartImportAsync();
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

    #endregion
}
