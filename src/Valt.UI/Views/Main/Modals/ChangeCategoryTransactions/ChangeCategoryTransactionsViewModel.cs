using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Modules.Budget.Categories.Queries;
using Valt.Infra.Modules.Budget.Categories.Queries.DTOs;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.ChangeCategoryTransactions;

public partial class ChangeCategoryTransactionsViewModel : ValtModalValidatorViewModel
{
    private readonly ITransactionTermService _transactionTermService;
    private readonly ICategoryQueries _categoryQueries;

    #region Form Data

    [ObservableProperty]
    private bool _renameEnabled = true;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ChangeCategoryTransactionsViewModel), nameof(ValidateName))]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _changeCategoryEnabled;

    [ObservableProperty]
    [CustomValidation(typeof(ChangeCategoryTransactionsViewModel), nameof(ValidateSelectedCategory))]
    private CategoryDTO? _selectedCategory;

    #endregion

    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = new();

    public static ValidationResult ValidateName(string? name, ValidationContext context)
    {
        var instance = (ChangeCategoryTransactionsViewModel)context.ObjectInstance;

        if (instance.RenameEnabled && string.IsNullOrWhiteSpace(name))
        {
            return new ValidationResult("New name is required when renaming.");
        }

        return ValidationResult.Success!;
    }

    public static ValidationResult ValidateSelectedCategory(CategoryDTO? category, ValidationContext context)
    {
        var instance = (ChangeCategoryTransactionsViewModel)context.ObjectInstance;

        if (instance.ChangeCategoryEnabled && category is null)
        {
            return new ValidationResult("Category is required when changing category.");
        }

        return ValidationResult.Success!;
    }

    public ChangeCategoryTransactionsViewModel(
        ITransactionTermService transactionTermService,
        ICategoryQueries categoryQueries)
    {
        _transactionTermService = transactionTermService;
        _categoryQueries = categoryQueries;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchCategoriesAsync();
    }

    private async Task FetchCategoriesAsync()
    {
        var categories = (await _categoryQueries.GetCategoriesAsync()).Items.OrderBy(x => x.Name);

        AvailableCategories.Clear();
        foreach (var category in categories)
            AvailableCategories.Add(category);
    }

    [RelayCommand]
    private Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            CloseDialog?.Invoke(new Response(RenameEnabled, Name, ChangeCategoryEnabled, SelectedCategory?.Id));
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

    public record Response(bool RenameEnabled, string? Name, bool ChangeCategoryEnabled, string? CategoryId);
}