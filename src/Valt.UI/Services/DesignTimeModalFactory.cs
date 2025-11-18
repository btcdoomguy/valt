using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class DesignTimeModalFactory : IModalFactory
{
    public Task<ValtBaseWindow>? CreateAsync(ApplicationModalNames modalName, Window? owner, object? parameter = null)
    {
        return null;
    }
}