using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.SoldAssetHistory;

public partial class SoldAssetHistoryViewModel : ValtModalViewModel
{
    [ObservableProperty] private string _windowTitle = "Sold Asset History";

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response
    {
    }
}
