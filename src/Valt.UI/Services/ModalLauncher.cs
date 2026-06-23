using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class ModalLauncher : IModalLauncher
{
    private readonly IModalFactory _modalFactory;

    public ModalLauncher(IModalFactory modalFactory)
    {
        _modalFactory = modalFactory;
    }

    public async Task ShowAsync(ApplicationModalNames modalName, Window owner, object? parameter = null)
    {
        var modal = await CreateModalAsync(modalName, owner, parameter);
        await ShowDialogAsync(modal, owner);
    }

    public async Task<TResult?> ShowAsync<TResult>(ApplicationModalNames modalName, Window owner, object? parameter = null)
    {
        var modal = await CreateModalAsync(modalName, owner, parameter);
        return await ShowDialogAsync<TResult>(modal, owner);
    }

    public async Task<TResult?> ShowAsync<TViewModel, TResult>(
        ApplicationModalNames modalName,
        Window owner,
        Action<TViewModel>? configure = null,
        object? parameter = null)
    {
        var modal = await CreateModalAsync(modalName, owner, parameter);

        if (modal.DataContext is TViewModel vm)
        {
            configure?.Invoke(vm);
        }

        return await ShowDialogAsync<TResult>(modal, owner);
    }

    protected virtual Task ShowDialogAsync(ValtBaseWindow dialog, Window owner)
        => dialog.ShowDialogSafeAsync(owner);

    protected virtual Task<TResult?> ShowDialogAsync<TResult>(ValtBaseWindow dialog, Window owner)
        => dialog.ShowDialogSafeAsync<TResult>(owner);

    private async Task<ValtBaseWindow> CreateModalAsync(
        ApplicationModalNames modalName,
        Window owner,
        object? parameter)
    {
        var task = _modalFactory.CreateAsync(modalName, owner, parameter)
            ?? throw new InvalidOperationException($"Modal factory returned null for '{modalName}'.");

        var modal = await task;
        if (modal is null)
            throw new InvalidOperationException($"Modal factory returned null for '{modalName}'.");

        return modal;
    }
}
