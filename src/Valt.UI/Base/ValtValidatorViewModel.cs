using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Views;

namespace Valt.UI.Base;

public abstract class ValtValidatorViewModel : ObservableValidator
{
    public MainViewTabNames PageName { get; init; }
}