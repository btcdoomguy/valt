using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.UI.State.Events;

namespace Valt.UI.State;

public partial class SecureModeState : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        WeakReferenceMessenger.Default.Send(new SecureModeChanged(value));
    }
}
