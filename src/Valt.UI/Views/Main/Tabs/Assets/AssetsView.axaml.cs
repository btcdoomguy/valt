using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Assets.Models;

namespace Valt.UI.Views.Main.Tabs.Assets;

public partial class AssetsView : ValtBaseUserControl
{
    public AssetsView()
    {
        InitializeComponent();

        AssetsItemsControl.AddHandler(KeyDownEvent, AssetsItemsControl_KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
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

        AssetsItemsControl.RemoveHandler(KeyDownEvent, AssetsItemsControl_KeyDownHandler);
    }

    private void AssetsItemsControl_KeyDownHandler(object? sender, KeyEventArgs e)
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

    private void AssetCard_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border { DataContext: AssetViewModel assetVm })
        {
            assetVm.IsHovered = true;
        }
    }

    private void AssetCard_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border { DataContext: AssetViewModel assetVm })
        {
            assetVm.IsHovered = false;
        }
    }

    private void AssetCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: AssetViewModel assetVm })
            return;

        var vm = DataContext as AssetsViewModel;
        if (vm is null) return;

        // Clear previous selection
        if (vm.SelectedAsset is not null && vm.SelectedAsset != assetVm)
        {
            vm.SelectedAsset.IsSelected = false;
        }

        // Set new selection
        assetVm.IsSelected = true;
        vm.SelectedAsset = assetVm;

        // Handle double-click for edit
        if (e.ClickCount == 2)
        {
            _ = vm.EditAssetCommand.ExecuteAsync(assetVm);
            e.Handled = true;
        }
    }
}
