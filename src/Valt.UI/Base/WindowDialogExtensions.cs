using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Valt.UI.Base;

/// <summary>
/// Extension methods for Avalonia Window dialogs that work around Avalonia 12 focus transition
/// issues on Windows. After a modal dialog closes, yields to the dispatcher at Render priority
/// to ensure focus state is fully synchronized before continuing.
/// </summary>
public static class WindowDialogExtensions
{
    /// <summary>
    /// Shows a modal dialog and yields to the dispatcher after it closes to ensure focus
    /// transitions are complete. Use this instead of <see cref="Window.ShowDialog(Window)"/>.
    /// </summary>
    public static async Task ShowDialogSafeAsync(this Window dialog, Window owner)
    {
        await dialog.ShowDialog(owner);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Shows a modal dialog with a result and yields to the dispatcher after it closes to ensure
    /// focus transitions are complete. Use this instead of <see cref="Window.ShowDialog{T}(Window)"/>.
    /// </summary>
    public static async Task<TResult?> ShowDialogSafeAsync<TResult>(this Window dialog, Window owner)
    {
        var result = await dialog.ShowDialog<TResult>(owner);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        return result;
    }
}
