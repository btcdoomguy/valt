using System.Threading.Tasks;

namespace Valt.UI.Views.Main.Tabs.Reports.Panels;

/// <summary>
/// ViewModel contract for the leverage positions dashboard panel.
/// </summary>
public interface ILeveragePositionsPanelViewModel
{
    decimal? AllTimeHighFiatValue { get; set; }

    Task RefreshAsync();
}
