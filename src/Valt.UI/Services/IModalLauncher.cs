using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.UI.Views;

namespace Valt.UI.Services;

public interface IModalLauncher
{
    Task ShowAsync(ApplicationModalNames modalName, Window owner, object? parameter = null);

    Task<TResult?> ShowAsync<TResult>(ApplicationModalNames modalName, Window owner, object? parameter = null);

    Task<TResult?> ShowAsync<TViewModel, TResult>(
        ApplicationModalNames modalName,
        Window owner,
        Action<TViewModel>? configure = null,
        object? parameter = null);
}
