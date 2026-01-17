using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Settings;
using Valt.UI.Helpers;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.ManageGoal.GoalTypeEditors;

public partial class ReduceExpenseCategoryGoalTypeEditorViewModel : ObservableObject, IGoalTypeEditorViewModel
{
    private readonly CurrencySettings? _currencySettings;
    private readonly ICategoryRepository? _categoryRepository;
    private List<Category> _categories = [];

    [ObservableProperty]
    private FiatValue _targetFiatAmount = FiatValue.Empty;

    [ObservableProperty]
    private string? _selectedCategoryId;

    public string Description => language.GoalType_ReduceExpenseCategory_Description;

    public string MainFiatCurrency =>
        _currencySettings?.MainFiatCurrency ?? FiatCurrency.Usd.Code;

    public List<ComboBoxValue> AvailableCategories =>
        _categories.Select(c => new ComboBoxValue(c.Name.Value, c.Id.Value)).ToList();

    public ReduceExpenseCategoryGoalTypeEditorViewModel()
    {
    }

    public ReduceExpenseCategoryGoalTypeEditorViewModel(CurrencySettings currencySettings, ICategoryRepository categoryRepository)
    {
        _currencySettings = currencySettings;
        _categoryRepository = categoryRepository;
        LoadCategoriesAsync();
    }

    private async void LoadCategoriesAsync()
    {
        if (_categoryRepository is null) return;
        _categories = (await _categoryRepository.GetCategoriesAsync()).ToList();
        OnPropertyChanged(nameof(AvailableCategories));
        if (_categories.Count > 0 && string.IsNullOrEmpty(SelectedCategoryId))
        {
            SelectedCategoryId = _categories.First().Id.Value;
        }
    }

    private string GetSelectedCategoryName()
    {
        if (string.IsNullOrEmpty(SelectedCategoryId)) return string.Empty;
        return _categories.FirstOrDefault(c => c.Id.Value == SelectedCategoryId)?.Name.Value ?? string.Empty;
    }

    public IGoalType CreateGoalType()
    {
        return new ReduceExpenseCategoryGoalType(
            TargetFiatAmount.Value,
            SelectedCategoryId ?? string.Empty,
            GetSelectedCategoryName());
    }

    public IGoalType CreateGoalTypePreservingCalculated(IGoalType? existing)
    {
        if (existing is ReduceExpenseCategoryGoalType reduceExpenseCategory)
        {
            return new ReduceExpenseCategoryGoalType(
                TargetFiatAmount.Value,
                SelectedCategoryId ?? string.Empty,
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
            SelectedCategoryId = reduceExpenseCategory.CategoryId;
        }
    }
}
