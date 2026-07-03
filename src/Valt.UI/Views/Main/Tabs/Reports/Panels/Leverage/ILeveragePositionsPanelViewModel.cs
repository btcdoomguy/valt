using System.ComponentModel;
using System.Threading.Tasks;
using Valt.UI.UserControls;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// ViewModel contract for the leverage positions dashboard panel.
/// </summary>
public interface ILeveragePositionsPanelViewModel : INotifyPropertyChanged
{
    DashboardData Data { get; set; }
    bool IsLoading { get; set; }
    bool IsVisible { get; set; }
    decimal? AllTimeHighFiatValue { get; set; }

    Task RefreshAsync();
}
