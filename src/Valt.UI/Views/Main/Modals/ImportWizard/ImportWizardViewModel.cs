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
    private CsvImportResult? _parseResult;

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
        WizardStep.FileSelection => IsFileSelected,
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
            // Import logic will be implemented in Phase 2 Plan 2
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
