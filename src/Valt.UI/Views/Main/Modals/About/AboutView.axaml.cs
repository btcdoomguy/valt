using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.About;

public partial class AboutView : ValtBaseWindow
{
    public AboutView()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Design.IsDesignMode) 
            return;
        
        _ = (DataContext as AboutViewModel)!.LoadDonationAddressesCommand.ExecuteAsync(null);
    }
}