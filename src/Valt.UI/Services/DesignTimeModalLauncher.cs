using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class DesignTimeModalLauncher : IModalLauncher
{
    public Task ShowAsync(ApplicationModalNames modalName, Window owner, object? parameter = null)
        => Task.CompletedTask;

    public Task<TResult?> ShowAsync<TResult>(ApplicationModalNames modalName, Window owner, object? parameter = null)
        => Task.FromResult(default(TResult?));

    public Task<TResult?> ShowAsync<TViewModel, TResult>(
        ApplicationModalNames modalName,
        Window owner,
        Action<TViewModel>? configure = null,
        object? parameter = null)
        => Task.FromResult(default(TResult?));
}
