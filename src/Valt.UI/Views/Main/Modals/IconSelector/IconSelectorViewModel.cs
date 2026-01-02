using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.UI.Base;
using Valt.UI.Services.IconMaps;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace Valt.UI.Views.Main.Modals.IconSelector;

public partial class IconSelectorViewModel : ValtModalViewModel
{
    const string MATERIAL_DESIGN_ICON_SOURCE = "MaterialSymbolsOutlined";
    
    private static HashSet<IconMap> _icons = [];
    private bool? _isSearching;

    #region Form Data
    
    [ObservableProperty] private IconMap? _selectedIcon;

    [ObservableProperty] private string? _selectedCategory;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(SelectedColorBrush))]
    private Color _selectedColor = Colors.White;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isFiltering;
    
    public SolidColorBrush SelectedColorBrush => new(SelectedColor);
    public AvaloniaList<IconMap> Icons { get; set; } = [];
    public AvaloniaList<string> Categories { get; set; } = [];
    
    #endregion

    public IconSelectorViewModel()
    {
        if (Design.IsDesignMode)
        {
            Categories.Add("A");
            Categories.Add("B");
            Categories.Add("C");
            Categories.Add("D");
            Categories.Add("E");

            Icons.Add(new IconMap(MATERIAL_DESIGN_ICON_SOURCE, "workspaces", char.Parse("\uE1A0"), "A"));
            Icons.Add(new IconMap(MATERIAL_DESIGN_ICON_SOURCE, "workspaces", char.Parse("\uE1A0"), "B"));
            Icons.Add(new IconMap(MATERIAL_DESIGN_ICON_SOURCE, "workspaces", char.Parse("\uE1A0"), "C"));
            Icons.Add(new IconMap(MATERIAL_DESIGN_ICON_SOURCE, "workspaces", char.Parse("\uE1A0"), "D"));
            Icons.Add(new IconMap(MATERIAL_DESIGN_ICON_SOURCE, "workspaces", char.Parse("\uE1A0"), "E"));

            SelectedCategory = "A";
        }
        else
        {
            if (Icons.Count == 0)
            {
                IconMapLoader.LoadIcons(MATERIAL_DESIGN_ICON_SOURCE);

                _icons = IconMapLoader.GetIconPack(MATERIAL_DESIGN_ICON_SOURCE);
                Icons = new AvaloniaList<IconMap>(_icons);
                LoadIconCategories();
                SelectedCategory = Categories.FirstOrDefault()!;
                //FilterIconsAsync().Wait();
            }
        }
    }

    public override Task OnBindParameterAsync()
    {
        if (Parameter is not string iconId)
            return Task.CompletedTask;

        var selectedIcon = Icon.RestoreFromId(iconId);

        var iconOnPack = _icons.SingleOrDefault(x => x.Name == selectedIcon.Name && x.Source == selectedIcon.Source);

        if (iconOnPack is not null)
            SelectedCategory = iconOnPack.Category ?? string.Empty;

        SelectedIcon = new IconMap(selectedIcon.Source, selectedIcon.Name, selectedIcon.Unicode, null);
        SelectedColor = new Color(selectedIcon.Color.A, selectedIcon.Color.R, selectedIcon.Color.G,
            selectedIcon.Color.B);

        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = FilterIconsAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = FilterIconsAsync();
    }

    [RelayCommand]
    private Task Ok()
    {
        CloseDialog?.Invoke(new Response(SelectedIcon, SelectedColor));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Clear()
    {
        CloseDialog?.Invoke(new Response(null, Colors.White));
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    private async Task FilterIconsAsync()
    {
        IsFiltering = true;

        try
        {
            // Perform filtering on a background thread
            var filteredIcons = await Task.Run(() =>
            {
                IEnumerable<IconMap> source;
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    if (_isSearching is null || !_isSearching.Value)
                        _isSearching = true;

                    source = _icons.Where(icon => icon.Name.Contains((string)SearchText, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    if (_isSearching is null || _isSearching.Value)
                        _isSearching = false;

                    source = _icons.Where(x => x.Category == SelectedCategory);
                }
                return source.ToList(); // Materialize the result
            });

            // Update the UI on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrWhiteSpace(SearchText) && (_isSearching is null || !_isSearching.Value))
                {
                    Categories.Clear();
                }
                else if (string.IsNullOrWhiteSpace(SearchText) && (_isSearching is null || _isSearching.Value))
                {
                    LoadIconCategories();
                }

                Icons.Clear();
                Icons.AddRange(filteredIcons);
            });
        }
        finally
        {
            IsFiltering = false;
        }
    }

    private void LoadIconCategories()
    {
        Categories.Clear();

        var categories = IconMapLoader.GetIconPackCategories(MATERIAL_DESIGN_ICON_SOURCE).OrderBy(x => x);
        foreach (var category in categories)
            Categories.Add(category);
    }

    public record Response(IconMap? Icon, Color Color);
}