using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Transactions;

namespace Valt.UI.State;

public partial class FilterState :  ObservableObject
{
    [ObservableProperty] private DateTime _mainDate = DateTime.MinValue;
    [ObservableProperty] private DateRange _range = new(DateTime.MinValue, DateTime.MinValue);
    //[ObservableProperty] private FixedExpenseProviderEntry? _selectedFixedExpense;
    
    partial void OnRangeChanged(DateRange value)
    {
        WeakReferenceMessenger.Default.Send(new FilterDateRangeChanged());
    }
    
    /*partial void OnSelectedFixedExpenseChanged(FixedExpenseProviderEntry? value)
    {
        WeakReferenceMessenger.Default.Send(new FilterFixedExpenseChanged());
    }*/
}