using System.Collections.ObjectModel;
using LiveChartsCore.Kernel;

namespace Valt.UI.Views.Main.Tabs.Reports;

public record DashboardData(string Title, ObservableCollection<RowItem> Rows);