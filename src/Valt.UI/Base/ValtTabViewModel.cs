using Valt.UI.Views;

namespace Valt.UI.Base;

public abstract class ValtTabViewModel : ValtViewModel
{
    public abstract MainViewTabNames TabName { get; }
}