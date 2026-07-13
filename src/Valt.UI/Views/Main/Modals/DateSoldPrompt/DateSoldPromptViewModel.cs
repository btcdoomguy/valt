using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.DateSoldPrompt;

public partial class DateSoldPromptViewModel : ValtModalViewModel
{
    [ObservableProperty] private string _windowTitle = language.SoldAssetHistory_DateSold_Title;
    [ObservableProperty] private DateTime _dateSold = DateTime.Now.Date;

    [RelayCommand]
    private void Ok()
    {
        CloseDialog?.Invoke(new Response(DateSold));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    public record Response(DateTime? DateSold);
}
