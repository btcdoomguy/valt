using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries;
using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Settings;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class ReduceExpenseCategoryGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly CurrencySettings? _currencySettings;
    private readonly IQueryDispatcher? _queryDispatcher;
    private string? _pendingCategoryId;

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    [ObservableProperty]
    private CategoryDTO? _selectedCategory;

    public string Description => language.GoalType_ReduceExpenseCategory_Description;

    public string MainFiatCurrency =>
        _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    public AvaloniaList<CategoryDTO> AvailableCategories { get; set; } = new();

    public ReduceExpenseCategoryGoalTypeEditorViewModel()
    {
    }

    public ReduceExpenseCategoryGoalTypeEditorViewModel(CurrencySettings currencySettings, IQueryDispatcher queryDispatcher)
    {
        _currencySettings = currencySettings;
        _queryDispatcher = queryDispatcher;
        LoadCategoriesAsync();
    }

    private async void LoadCategoriesAsync()
    {
        if (_queryDispatcher is null) return;
        var result = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());
        var categories = result.Items.OrderBy(x => x.Name);

        AvailableCategories.Clear();
        foreach (var category in categories)
            AvailableCategories.Add(category);

        if (AvailableCategories.Count > 0)
        {
            if (_pendingCategoryId is not null)
            {
                SelectCategoryById(_pendingCategoryId);
                _pendingCategoryId = null;
            }
            else if (SelectedCategory is null)
            {
                SelectedCategory = AvailableCategories.First();
            }
        }
    }

    private string GetSelectedCategoryName()
    {
        return SelectedCategory?.SimpleName ?? string.Empty;
    }

    public IGoalType CreateGoalType()
    {
        return new ReduceExpenseCategoryGoalType(
            TargetFiatAmount.Value,
            SelectedCategory?.Id ?? string.Empty,
            GetSelectedCategoryName());
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is ReduceExpenseCategoryGoalType reduceExpenseCategory)
        {
            return new ReduceExpenseCategoryGoalType(
                TargetFiatAmount.Value,
                SelectedCategory?.Id ?? string.Empty,
                GetSelectedCategoryName(),
                reduceExpenseCategory.CalculatedSpending);
        }

        return CreateGoalType();
    }

    public void LoadFrom(IGoalType goalType)
    {
        if (goalType is ReduceExpenseCategoryGoalType reduceExpenseCategory)
        {
            TargetFiatAmount = FiatValue.New(reduceExpenseCategory.TargetAmount);

            // If categories are already loaded, select immediately; otherwise store for later
            if (AvailableCategories.Count > 0)
            {
                SelectCategoryById(reduceExpenseCategory.CategoryId);
            }
            else
            {
                _pendingCategoryId = reduceExpenseCategory.CategoryId;
            }
        }
    }

    private void SelectCategoryById(string categoryId)
    {
        var category = AvailableCategories.FirstOrDefault(c => c.Id == categoryId);
        if (category is not null)
        {
            SelectedCategory = category;
        }
    }
}
