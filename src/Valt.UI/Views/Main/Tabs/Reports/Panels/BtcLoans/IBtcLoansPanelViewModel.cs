using System.ComponentModel;
using System.Threading.Tasks;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// ViewModel contract for the BTC loans dashboard panel.
/// </summary>
public interface IBtcLoansPanelViewModel : INotifyPropertyChanged
{
    DashboardData Data { get; set; }
    bool IsLoading { get; set; }
    bool IsVisible { get; set; }

    Task RefreshAsync();
}
