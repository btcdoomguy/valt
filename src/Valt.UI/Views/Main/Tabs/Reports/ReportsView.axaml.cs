using Avalonia;
using Avalonia.Threading;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsView : ValtBaseUserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        if (DataContext is ReportsViewModel vm)
        {
            vm.Initialize();
        }
    }
}