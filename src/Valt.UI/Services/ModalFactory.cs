using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class ModalFactory : IModalFactory
{
    private readonly Func<ApplicationModalNames, ValtBaseWindow> _factoryMethod;

    public ModalFactory(Func<ApplicationModalNames, ValtBaseWindow> factoryMethod)
    {
        _factoryMethod = factoryMethod;
    }

    public async Task<ValtBaseWindow>? CreateAsync(ApplicationModalNames modalName, Window? owner,
        object? parameter = null)
    {
        var window = _factoryMethod.Invoke(modalName);

        if (window.DataContext is IValtModal modal)
        {
            modal.OwnerWindow = owner;
            modal.Parameter = parameter;
            await modal.OnBindParameterAsync();
        }

        return window;
    }
}