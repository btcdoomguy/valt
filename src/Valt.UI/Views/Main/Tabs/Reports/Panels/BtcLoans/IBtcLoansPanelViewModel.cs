using System.Threading.Tasks;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// ViewModel contract for the BTC loans dashboard panel.
/// </summary>
public interface IBtcLoansPanelViewModel
{
    Task RefreshAsync();
}
