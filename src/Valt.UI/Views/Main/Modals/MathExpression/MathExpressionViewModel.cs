using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StringMath;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.MathExpression;

public partial class MathExpressionViewModel : ValtModalViewModel
{
    [ObservableProperty] private string _expression = string.Empty;


    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private decimal? _previewResult;

    public record Response(decimal? Result);

    private bool CanOk()
    {
        return PreviewResult is not null;
    }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private async Task Ok()
    {
        CloseDialog?.Invoke(new Response(PreviewResult));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    partial void OnExpressionChanged(string value)
    {
        try
        {
            var total = value.Eval();
            PreviewResult = Convert.ToDecimal(total);
        }
        catch (Exception)
        {
            PreviewResult = null;
        }
    }
}