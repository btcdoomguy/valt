using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Valt.UI.Base;

public abstract class ValtModalValidatorViewModel : ValtValidatorViewModel, IValtModal
{
    public Action? CloseWindow { get; set; }
    public Action<object>? CloseDialog { get; set; }
    public Window? OwnerWindow { get; set; }
    public Func<Window>? GetWindow { get; set; }
    public object? Parameter { get; set; }

    public virtual Task OnBindParameterAsync()
    {
        return Task.CompletedTask;
    }
}