using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;

public partial class ChangeCategoryTransactionsViewModel : ValtModalValidatorViewModel
{
    private readonly ITransactionTermService _transactionTermService;

    #region Form Data
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "New name is required.")]
    private string _name = string.Empty;
    
    #endregion
    
    public ChangeCategoryTransactionsViewModel(ITransactionTermService transactionTermService)
    {
        _transactionTermService = transactionTermService;
    }

    [RelayCommand]
    private Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            CloseDialog?.Invoke(new Response(Name));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    public Task<IEnumerable<object>> GetSearchTermsAsync(string? term, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(term)
            ? Task.FromResult(Enumerable.Empty<object>())
            : Task.FromResult<IEnumerable<object>>(_transactionTermService!.Search(term, 5).Select(x => x.Name)
                .Distinct());
    }
    
    public record Response(string? Name);
}