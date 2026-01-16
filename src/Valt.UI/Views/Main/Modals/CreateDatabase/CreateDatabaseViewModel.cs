using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;
using Valt.UI.Helpers;

namespace Valt.UI.Views.Main.Modals.CreateDatabase;

public partial class CreateDatabaseViewModel : ValtModalValidatorViewModel
{
    #region Form Data
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Inform a valid path to store the database file.")]
    private string _path = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required.")]
    [CustomValidation(typeof(CreateDatabaseViewModel), "ValidatePassword")]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = GetDefaultLanguage();

    /// <summary>
    /// All available fiat currencies (excluding USD which is mandatory)
    /// </summary>
    public AvaloniaList<FiatCurrencyItem> AvailableFiatCurrencies { get; } = new(FiatCurrencyItem.GetAllExceptUsd());

    /// <summary>
    /// Selected fiat currencies (excluding USD which is mandatory)
    /// </summary>
    public AvaloniaList<FiatCurrencyItem> SelectedFiatCurrencies { get; } = new();

    #endregion

    public static List<ComboBoxValue> AvailableLanguages =>
    [
        new("English (en-US)", "en-US"),
        new("Español (es)", "es"),
        new("Português (pt-BR)", "pt-BR"),
    ];

    private static string GetDefaultLanguage()
    {
        var current = CultureInfo.CurrentCulture.Name;
        if (current == "pt-BR") return "pt-BR";
        if (current.StartsWith("es")) return "es";
        return "en-US";
    }

    [RelayCommand]
    private Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            // Build the list of selected currencies (always include USD)
            var selectedCurrencies = new List<string> { "USD" };
            selectedCurrencies.AddRange(SelectedFiatCurrencies.Select(c => c.Code));

            CloseDialog?.Invoke(new Response(Path, Password, SelectedLanguage, selectedCurrencies));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private Task CloseEmpty()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveDatabaseFile()
    {
        var fileResult = await OwnerWindow!.StorageProvider.SaveFilePickerAsync(CreateFilePickerSaveOptions());

        var pathResponse = fileResult?.Path.LocalPath;

        if (pathResponse is not null)
        {
            Path = pathResponse;
        }
    }

    public static ValidationResult ValidatePassword(string password, ValidationContext context)
    {
        var instance = (CreateDatabaseViewModel)context.ObjectInstance;
        var isValid = instance.Password == instance.ConfirmPassword;

        return (isValid ? ValidationResult.Success : new ValidationResult("Passwords do not match."))!;
    }

    private static FilePickerSaveOptions CreateFilePickerSaveOptions()
    {
        return new FilePickerSaveOptions()
        {
            Title = "Create new database",
            DefaultExtension = "valt",
            SuggestedFileName = "MyDatabase.valt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("VALT Files")
                {
                    Patterns = new[] { "*.valt" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*" }
                }
            }
        };
    }
    
    public record Response(string Path, string Password, string Language, List<string> SelectedCurrencies);
}