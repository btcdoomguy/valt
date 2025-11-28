using System.Collections.ObjectModel;

namespace Valt.UI.Views.Main.Tabs.Reports;

public record DashboardData(string Title, ObservableCollection<RowItem> Rows);