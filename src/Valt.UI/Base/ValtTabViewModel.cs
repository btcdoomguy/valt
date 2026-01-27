using System.Threading.Tasks;
using Valt.UI.Views;

namespace Valt.UI.Base;

public abstract class ValtTabViewModel : ValtViewModel
{
    public abstract MainViewTabNames TabName { get; }

    /// <summary>
    /// Refreshes the tab's data. Called when the user clicks the Refresh button after MCP changes.
    /// </summary>
    public virtual Task RefreshAsync() => Task.CompletedTask;
}