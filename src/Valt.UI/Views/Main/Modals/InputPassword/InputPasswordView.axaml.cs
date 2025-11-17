using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.InputPassword;

public partial class InputPasswordView : ValtBaseWindow
{
    public InputPasswordView()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        Dispatcher.UIThread.InvokeAsync(() => PasswordBox.Focus(), DispatcherPriority.ApplicationIdle);
    }
}