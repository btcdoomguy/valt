using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Assets.Models;

namespace Valt.UI.Views.Main.Tabs.Assets;

public partial class AssetsView : ValtBaseUserControl
{
    private bool _isUpdatingSelection;

    public AssetsView()
    {
        InitializeComponent();

        AssetsGrid.AddHandler(KeyDownEvent, AssetsGrid_KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
        AssetsGrid.AddHandler(DoubleTappedEvent, AssetsGrid_OnDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);
        AssetsGrid.AddHandler(PointerPressedEvent, AssetsGrid_OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (DataContext is AssetsViewModel vm)
        {
            vm.Initialize();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        AssetsGrid.RemoveHandler(KeyDownEvent, AssetsGrid_KeyDownHandler);
        AssetsGrid.RemoveHandler(DoubleTappedEvent, AssetsGrid_OnDoubleTapped);
        AssetsGrid.RemoveHandler(PointerPressedEvent, AssetsGrid_OnPointerPressed);
    }

    private void AssetsGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = DataContext as AssetsViewModel;
        if (vm is null) return;

        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<DataGridRow>();

        if (row is null || vm.SelectedAsset is null) return;

        _ = vm.EditAssetCommand.ExecuteAsync(vm.SelectedAsset);
        e.Handled = true;
    }

    private void AssetsGrid_KeyDownHandler(object? sender, KeyEventArgs e)
    {
        var vm = DataContext as AssetsViewModel;
        if (vm is null) return;

        // F2 to add new asset
        if (e.Key == Key.F2)
        {
            _ = vm.AddAssetCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        // Enter to edit selected asset
        if (e.Key == Key.Enter && vm.SelectedAsset is not null)
        {
            _ = vm.EditAssetCommand.ExecuteAsync(vm.SelectedAsset);
            e.Handled = true;
        }
    }

    private void AssetsGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Select the row on right-click so context menu commands work correctly
        if (!e.GetCurrentPoint(AssetsGrid).Properties.IsRightButtonPressed)
            return;

        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<DataGridRow>();

        if (row?.DataContext is AssetViewModel assetVm)
        {
            AssetsGrid.SelectedItem = assetVm;
        }
    }

    private void AssetsGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        try
        {
            _isUpdatingSelection = true;

            if (DataContext is AssetsViewModel vm && sender is DataGrid dataGrid)
            {
                vm.SelectedAsset = dataGrid.SelectedItem as AssetViewModel;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}
