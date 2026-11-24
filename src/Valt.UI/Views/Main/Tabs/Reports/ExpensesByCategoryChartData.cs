using Valt.Infra.Modules.Reports.ExpensesByCategory;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class ExpensesByCategoryChartData : CategoryBarChartDataBase
{
    public ExpensesByCategoryChartData() : base(-1m)
    {
    }

    public void RefreshChart(ExpensesByCategoryData expensesByCategoryData)
    {
        RefreshChartCore(
            expensesByCategoryData.MainCurrency,
            expensesByCategoryData.Items,
            x => x.FiatTotal,
            x => x.CategoryName,
            x => x.Icon);
    }
}
