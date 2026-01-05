using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Exceptions;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Infra.Modules.Budget.Transactions.Queries;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.IconSelector;
using Valt.UI.Views.Main.Modals.ManageCategories.Models;

namespace Valt.UI.Views.Main.Modals.ManageCategories;

public partial class ManageCategoriesViewModel : ValtModalValidatorViewModel
{
    private readonly ICategoryRepository? _categoryRepository;
    private readonly ITransactionQueries _transactionQueries;
    private readonly IModalFactory? _modalFactory;

    #region Form data

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdding))]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(EditFields))]
    private CategoryTreeElement? _selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdding))]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(EditFields))]
    private bool _editMode;

    [ObservableProperty]
    private string? _id;

    [ObservableProperty]
    [Required(ErrorMessage = "Name is required")] 
    private string _name = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IconUiWrapper))] [Required(ErrorMessage = "Icon is required")]
    private Icon _icon = Icon.Empty;

    public AvaloniaList<CategoryTreeElement> NewCategories { get; set; } = new();
    public IconUIWrapper IconUiWrapper => new(Icon);

    public bool IsViewing => SelectedCategory is not null && !EditMode;
    
    public bool IsAdding => SelectedCategory is null;

    public bool IsEditing => SelectedCategory is not null && EditMode;
    
    public bool EditFields => IsAdding || IsEditing;

    #endregion

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageCategoriesViewModel()
    {
        if (!Design.IsDesignMode) return;

        NewCategories =
        [
            new CategoryTreeElement(new CategoryId("A").Value, null, "House", Icon.Empty.Unicode, Icon.Empty.Color,
            [
                new CategoryTreeElement(new CategoryId().Value, new CategoryId("A").Value, "Furniture",
                    Icon.Empty.Unicode, Icon.Empty.Color),
                new CategoryTreeElement(new CategoryId().Value, new CategoryId("A").Value, "Food",
                    Icon.Empty.Unicode, Icon.Empty.Color),
                new CategoryTreeElement(new CategoryId().Value, new CategoryId("A").Value, "Services",
                    Icon.Empty.Unicode, Icon.Empty.Color)
            ]),
            new CategoryTreeElement(new CategoryId("B").Value, null, "Fun", Icon.Empty.Unicode, Icon.Empty.Color,
            [
                new CategoryTreeElement(new CategoryId().Value, new CategoryId("B").Value, "Movies",
                    Icon.Empty.Unicode, Icon.Empty.Color),
                new CategoryTreeElement(new CategoryId().Value, new CategoryId("B").Value, "Games",
                    Icon.Empty.Unicode, Icon.Empty.Color)
            ]),
            new CategoryTreeElement(new CategoryId().Value, null, "Health", Icon.Empty.Unicode, Icon.Empty.Color)
        ];
    }

    public ManageCategoriesViewModel(ICategoryRepository categoryRepository,
        ITransactionQueries transactionQueries,
        IModalFactory modalFactory)
    {
        _categoryRepository = categoryRepository;
        _transactionQueries = transactionQueries;
        _modalFactory = modalFactory;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchCategoriesAsync();
    }

    private async Task FetchCategoriesAsync()
    {
        var categories = (await _categoryRepository!.GetCategoriesAsync()).OrderBy(x => x.Name.Value);

        NewCategories.Clear();

        //two levels only, so it's easy to manage the tree
        foreach (var rootLevelCategory in categories.Where(x => x.ParentId is null))
        {
            NewCategories.Add(new CategoryTreeElement(rootLevelCategory.Id.Value, rootLevelCategory.ParentId?.Value,
                rootLevelCategory.Name.Value,
                rootLevelCategory.Icon.Unicode, rootLevelCategory.Icon.Color));
        }

        foreach (var childLevelCategory in categories.Where(x => x.ParentId is not null))
        {
            var parent = NewCategories.SingleOrDefault(x => x.Id == childLevelCategory.ParentId!.Value);

            if (parent is not null)
            {
                parent.SubNodes.Add(new CategoryTreeElement(childLevelCategory.Id.Value,
                    childLevelCategory.ParentId?.Value, childLevelCategory.Name.Value,
                    childLevelCategory.Icon.Unicode, childLevelCategory.Icon.Color));
            }
            else
            {
                NewCategories.Add(new CategoryTreeElement(childLevelCategory.Id.Value,
                    childLevelCategory.ParentId?.Value, childLevelCategory.Name.Value,
                    childLevelCategory.Icon.Unicode, childLevelCategory.Icon.Color));
            }
        }
    }

    [RelayCommand]
    private async Task AddNew()
    {
        await ClearSelection();
    }

    [RelayCommand]
    private async Task SaveChanges()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            if (Id == null)
            {
                var category = Category.New(CategoryName.New(Name), Icon);

                await _categoryRepository!.SaveCategoryAsync(category);
            }
            else
            {
                var id = new CategoryId(Id);
                var name = CategoryName.New(Name);

                var category = await _categoryRepository!.GetCategoryByIdAsync(id);

                if (category is null)
                    throw new EntityNotFoundException(nameof(Category), id);

                category.Rename(name);
                category.ChangeIcon(Icon);

                await _categoryRepository.SaveCategoryAsync(category);
            }

            await FetchCategoriesAsync();
            await ClearSelection();
        }
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedCategory is null)
            return;

        var selectedCategoryId = new CategoryId(SelectedCategory.Id);
        var transactionsWithCategoryId = await _transactionQueries.GetTransactionsAsync(new TransactionQueryFilter()
        {
            Categories = [SelectedCategory.Id]
        });

        if (transactionsWithCategoryId.Items.Count > 0)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error,
                "Cannot delete category if it was already used by a transaction.", GetWindow!());
            return;
        }

        await _categoryRepository!.DeleteCategoryAsync(selectedCategoryId);
        await FetchCategoriesAsync();
        await ClearSelection();
    }

    [RelayCommand]
    private async Task RemoveFromGroup()
    {
        if (SelectedCategory is not null && SelectedCategory.ParentId is not null)
        {
            await ChangeCategoryParent(SelectedCategory.Id, null);
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedCategory is not null)
        {
            EditMode = true;
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        EditMode = false;
    }

    private Task ClearSelection()
    {
        SelectedCategory = null;
        EditMode = false;
        Id = null;
        Name = "";
        Icon = Icon.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task IconSelectorOpen()
    {
        var modal =
            (IconSelectorView)await _modalFactory!.CreateAsync(ApplicationModalNames.IconSelector, OwnerWindow,
                Icon.ToString())!;

        var response = await modal.ShowDialog<IconSelectorViewModel.Response?>(OwnerWindow!);

        if (response is null)
            return;

        if (response.Icon is not null)
        {
            Icon = new Icon(response.Icon.Source, response.Icon.Name, response.Icon.Unicode,
                System.Drawing.Color.FromArgb(response.Color.A, response.Color.R, response.Color.G, response.Color.B));
        }
        else
        {
            Icon = Icon.Empty;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedCategory))
        {
            if (SelectedCategory is not null)
            {
                _ = LoadCategoryInfoAsync(SelectedCategory.Id);
            }
        }
    }

    private async Task LoadCategoryInfoAsync(string selectedCategoryId)
    {
        var category = await _categoryRepository!.GetCategoryByIdAsync(selectedCategoryId);

        Id = category!.Id;
        Name = category.Name;
        Icon = category.Icon;
    }

    public async Task ChangeCategoryParent(string categoryId, string? newParentId)
    {
        var category = await _categoryRepository!.GetCategoryByIdAsync(new CategoryId(categoryId));

        category!.ChangeParent(newParentId is not null ? new CategoryId(newParentId) : null);

        await _categoryRepository!.SaveCategoryAsync(category);

        Dispatcher.UIThread.Post(() =>
        {
            _ = FetchCategoriesAsync(); 
            _ = ClearSelection();
            SelectedCategory = NewCategories.SingleOrDefault(x => x.Id == categoryId);
        });
    }

    public CategoryTreeElement? FindParent(CategoryTreeElement item)
    {
        return FindParent(item, NewCategories);
    }

    private static CategoryTreeElement? FindParent(CategoryTreeElement item, AvaloniaList<CategoryTreeElement> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.SubNodes != null && node.SubNodes.Contains(item))
                return node;

            if (node.SubNodes == null) continue;

            var parent = FindParent(item, node.SubNodes);
            if (parent is not null)
                return parent;
        }

        return null;
    }
}