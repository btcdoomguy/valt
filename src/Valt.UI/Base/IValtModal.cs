using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Valt.UI.Base;

public interface IValtModal
{
    Action? CloseWindow { get; set; }
    Action<object>? CloseDialog { get; set; }
    Window? OwnerWindow { get; set; }
    Func<Window>? GetWindow { get; set; }

    /// <summary>
    /// A parameter that can be sent to the modal
    /// </summary>
    object? Parameter { get; set; }

    /// <summary>
    /// Triggered when the parameter is initialized with a value
    /// </summary>
    /// <returns></returns>
    Task OnBindParameterAsync();
}