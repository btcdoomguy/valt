using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Valt.UI.Views.Main.Tabs.Reports;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceView : UserControl
{
    public AvgPriceView()
    {
        InitializeComponent();
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        if (DataContext is AvgPriceViewModel vm)
        {
            vm.Initialize();
        }
    }
}