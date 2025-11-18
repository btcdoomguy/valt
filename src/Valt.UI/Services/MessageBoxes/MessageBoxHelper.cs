using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace Valt.UI.Services.MessageBoxes;

public static class MessageBoxHelper
{
    public static async Task ShowAlertAsync(string title, string message, Window owner)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Warning,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.None
        });

        await messageBox.ShowWindowDialogAsync(owner);
    }

    public static async Task ShowErrorAsync(string title, string message, Window owner)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Error,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.None
        });

        await messageBox.ShowWindowDialogAsync(owner);
    }
    
    public static async Task<bool> ShowQuestionAsync(string title, string message, Window owner)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.YesNo,
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Question,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.None
        });

        var result = await messageBox.ShowWindowDialogAsync(owner);
        
        return result == ButtonResult.Yes;
    }
    
    public static async Task<bool> ShowOkCancelAsync(string title, string message, Window owner)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.OkCancel,
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Info,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.Full,
        });

        var result = await messageBox.ShowWindowDialogAsync(owner);

        return result == ButtonResult.Ok;
    }
}