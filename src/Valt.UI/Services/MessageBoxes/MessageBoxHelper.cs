using System.Threading.Tasks;
using Avalonia.Controls;

namespace Valt.UI.Services.MessageBoxes;

public static class MessageBoxHelper
{
    public static async Task ShowAlertAsync(string title, string message, Window owner)
    {
        var messageBox = new ValtMessageBox();
        messageBox.Configure(title, message, ValtMessageBox.MessageBoxIcon.Warning, ValtMessageBox.MessageBoxButtons.Ok);
        await messageBox.ShowDialog(owner);
    }

    public static async Task ShowErrorAsync(string title, string message, Window owner)
    {
        var messageBox = new ValtMessageBox();
        messageBox.Configure(title, message, ValtMessageBox.MessageBoxIcon.Error, ValtMessageBox.MessageBoxButtons.Ok);
        await messageBox.ShowDialog(owner);
    }

    public static async Task<bool> ShowQuestionAsync(string title, string message, Window owner)
    {
        var messageBox = new ValtMessageBox();
        messageBox.Configure(title, message, ValtMessageBox.MessageBoxIcon.Question, ValtMessageBox.MessageBoxButtons.YesNo);
        await messageBox.ShowDialog(owner);
        return messageBox.Result == ValtMessageBox.MessageBoxResult.Yes;
    }

    public static async Task<bool> ShowOkCancelAsync(string title, string message, Window owner)
    {
        var messageBox = new ValtMessageBox();
        messageBox.Configure(title, message, ValtMessageBox.MessageBoxIcon.Info, ValtMessageBox.MessageBoxButtons.OkCancel);
        await messageBox.ShowDialog(owner);
        return messageBox.Result == ValtMessageBox.MessageBoxResult.Ok;
    }
}