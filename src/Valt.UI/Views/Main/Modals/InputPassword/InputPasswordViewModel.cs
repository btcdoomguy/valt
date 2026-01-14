using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.InputPassword;

public partial class InputPasswordViewModel : ValtModalValidatorViewModel
{
    #region Form Data

    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Password is required.")]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _startInSecureMode;

    [ObservableProperty]
    private bool _hideSecureModeCheckbox;

    #endregion

    [RelayCommand]
    private Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            CloseDialog?.Invoke(new Response(Password, StartInSecureMode));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }
    
    public record Response(string? Password, bool StartInSecureMode);
}