using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.SpendingEvolution;

public partial class SpendingEvolutionViewModel : ValtModalViewModel
{
    [ObservableProperty]
    private string? _preSelectedCategoryId;

    public override Task OnBindParameterAsync()
    {
        if (Parameter is string categoryId)
        {
            PreSelectedCategoryId = categoryId;
        }

        return Task.CompletedTask;
    }
}
