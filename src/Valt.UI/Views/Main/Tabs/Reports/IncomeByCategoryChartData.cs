using Valt.Infra.Modules.Reports.IncomeByCategory;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class IncomeByCategoryChartData : CategoryBarChartDataBase
{
    public IncomeByCategoryChartData() : base(1m)
    {
    }

    public void RefreshChart(IncomeByCategoryData incomeByCategoryData)
    {
        RefreshChartCore(
            incomeByCategoryData.MainCurrency,
            incomeByCategoryData.Items,
            x => x.FiatTotal,
            x => x.CategoryName,
            x => x.Icon);
    }
}
