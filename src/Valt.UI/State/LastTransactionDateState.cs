using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.UI.State;

public partial class LastTransactionDateState : ObservableObject
{
    [ObservableProperty]
    private DateTime? _lastDate;
}
