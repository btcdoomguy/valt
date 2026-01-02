using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.AvgPrice;

namespace Valt.UI.Views.Main.Modals.ManageAvgPriceProfiles;

public partial class ManageAvgPriceProfilesView : ValtBaseWindow
{
    public ManageAvgPriceProfilesView()
    {
        InitializeComponent();
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        if (DataContext is ManageAvgPriceProfilesViewModel vm)
        {
            vm.Initialize();
        }
    }
}