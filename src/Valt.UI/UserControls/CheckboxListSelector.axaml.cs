using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Valt.UI.UserControls;

public partial class CheckboxListSelector : UserControl
{
    private string _title = string.Empty;
    private string _displayMemberPath = string.Empty;
    private double _maxListHeight = 200;
    private bool _isUpdatingSelection;

    public ObservableCollection<CheckboxListItem> InternalItems { get; } = new();

    public static readonly DirectProperty<CheckboxListSelector, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<CheckboxListSelector, string>(
            nameof(Title),
            o => o.Title,
            (o, v) => o.Title = v);

    public static readonly DirectProperty<CheckboxListSelector, string> DisplayMemberPathProperty =
        AvaloniaProperty.RegisterDirect<CheckboxListSelector, string>(
            nameof(DisplayMemberPath),
            o => o.DisplayMemberPath,
            (o, v) => o.DisplayMemberPath = v);

    public static readonly DirectProperty<CheckboxListSelector, double> MaxListHeightProperty =
        AvaloniaProperty.RegisterDirect<CheckboxListSelector, double>(
            nameof(MaxListHeight),
            o => o.MaxListHeight,
            (o, v) => o.MaxListHeight = v);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<CheckboxListSelector, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<CheckboxListSelector, IList?>(
            nameof(SelectedItems),
            defaultBindingMode: BindingMode.TwoWay);

    static CheckboxListSelector()
    {
        ItemsSourceProperty.Changed.AddClassHandler<CheckboxListSelector>(OnItemsSourceChanged);
        SelectedItemsProperty.Changed.AddClassHandler<CheckboxListSelector>(OnSelectedItemsChanged);
    }

    public CheckboxListSelector()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }

    public string DisplayMemberPath
    {
        get => _displayMemberPath;
        set => SetAndRaise(DisplayMemberPathProperty, ref _displayMemberPath, value);
    }

    public double MaxListHeight
    {
        get => _maxListHeight;
        set => SetAndRaise(MaxListHeightProperty, ref _maxListHeight, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    private static void OnItemsSourceChanged(CheckboxListSelector selector, AvaloniaPropertyChangedEventArgs e)
    {
        selector.RebuildInternalItems();
    }

    private static void OnSelectedItemsChanged(CheckboxListSelector selector, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= selector.OnSelectedItemsCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += selector.OnSelectedItemsCollectionChanged;
        }

        selector.SyncSelectionFromSelectedItems();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;
        SyncSelectionFromSelectedItems();
    }

    private void RebuildInternalItems()
    {
        // Unsubscribe from old items
        foreach (var item in InternalItems)
        {
            item.PropertyChanged -= OnInternalItemPropertyChanged;
        }

        InternalItems.Clear();

        if (ItemsSource == null) return;

        foreach (var item in ItemsSource)
        {
            var displayText = GetDisplayText(item);
            var internalItem = new CheckboxListItem(item, displayText);
            internalItem.PropertyChanged += OnInternalItemPropertyChanged;
            InternalItems.Add(internalItem);
        }

        SyncSelectionFromSelectedItems();
    }

    private string GetDisplayText(object item)
    {
        if (string.IsNullOrEmpty(DisplayMemberPath))
        {
            return item.ToString() ?? string.Empty;
        }

        var property = item.GetType().GetProperty(DisplayMemberPath, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(item)?.ToString() ?? string.Empty;
    }

    private void SyncSelectionFromSelectedItems()
    {
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        try
        {
            var selectedItems = SelectedItems;

            foreach (var internalItem in InternalItems)
            {
                internalItem.IsSelected = selectedItems?.Cast<object>().Contains(internalItem.Item) ?? false;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void OnInternalItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CheckboxListItem.IsSelected)) return;
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        try
        {
            UpdateSelectedItems();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void UpdateSelectedItems()
    {
        var selectedItems = SelectedItems;
        if (selectedItems == null) return;

        selectedItems.Clear();

        foreach (var item in InternalItems.Where(x => x.IsSelected))
        {
            selectedItems.Add(item.Item);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        _isUpdatingSelection = true;
        try
        {
            foreach (var item in InternalItems)
            {
                item.IsSelected = true;
            }

            UpdateSelectedItems();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}

public partial class CheckboxListItem : ObservableObject
{
    public object Item { get; }
    public string DisplayText { get; }

    [ObservableProperty]
    private bool _isSelected;

    public CheckboxListItem(object item, string displayText)
    {
        Item = item;
        DisplayText = displayText;
    }
}
