using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.Commands.ChangeCategoryParent;
using Valt.App.Modules.Budget.Categories.Commands.CreateCategory;
using Valt.App.Modules.Budget.Categories.Commands.DeleteCategory;
using Valt.App.Modules.Budget.Categories.Commands.EditCategory;
using Valt.App.Modules.Budget.Categories.Queries;
using Valt.App.Modules.Budget.Categories.Queries.GetCategory;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Categories;
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
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
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

    public ManageCategoriesViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IModalFactory modalFactory)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _modalFactory = modalFactory;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchCategoriesAsync();
    }

    private async Task FetchCategoriesAsync()
    {
        var result = await _queryDispatcher!.DispatchAsync(new GetCategoriesQuery());
        var categories = result.Items.OrderBy(x => x.SimpleName);

        NewCategories.Clear();

        // Two levels only, so it's easy to manage the tree
        foreach (var rootLevelCategory in categories.Where(x => x.ParentId is null))
        {
            NewCategories.Add(new CategoryTreeElement(
                rootLevelCategory.Id,
                rootLevelCategory.ParentId,
                rootLevelCategory.SimpleName,
                rootLevelCategory.Unicode,
                rootLevelCategory.Color));
        }

        foreach (var childLevelCategory in categories.Where(x => x.ParentId is not null))
        {
            var parent = NewCategories.SingleOrDefault(x => x.Id == childLevelCategory.ParentId);

            if (parent is not null)
            {
                parent.SubNodes.Add(new CategoryTreeElement(
                    childLevelCategory.Id,
                    childLevelCategory.ParentId,
                    childLevelCategory.SimpleName,
                    childLevelCategory.Unicode,
                    childLevelCategory.Color));
            }
            else
            {
                NewCategories.Add(new CategoryTreeElement(
                    childLevelCategory.Id,
                    childLevelCategory.ParentId,
                    childLevelCategory.SimpleName,
                    childLevelCategory.Unicode,
                    childLevelCategory.Color));
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
                var result = await _commandDispatcher!.DispatchAsync(new CreateCategoryCommand
                {
                    Name = Name,
                    IconId = Icon.ToString()
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
            }
            else
            {
                var result = await _commandDispatcher!.DispatchAsync(new EditCategoryCommand
                {
                    CategoryId = Id,
                    Name = Name,
                    IconId = Icon.ToString()
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
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

        var result = await _commandDispatcher!.DispatchAsync(new DeleteCategoryCommand
        {
            CategoryId = SelectedCategory.Id
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
            return;
        }

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
        var category = await _queryDispatcher!.DispatchAsync(new GetCategoryQuery { CategoryId = selectedCategoryId });

        if (category is null) return;

        Id = category.Id;
        Name = category.SimpleName;
        Icon = category.IconId is not null ? Icon.RestoreFromId(category.IconId) : Icon.Empty;
    }

    public async Task ChangeCategoryParent(string categoryId, string? newParentId)
    {
        var result = await _commandDispatcher!.DispatchAsync(new ChangeCategoryParentCommand
        {
            CategoryId = categoryId,
            NewParentId = newParentId
        });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
            return;
        }

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