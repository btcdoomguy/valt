using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;
using Valt.UI.Views.Main.Modals.ManageCategories.Models;

namespace Valt.UI.Views.Main.Modals.ManageCategories;

public partial class ManageCategoriesView : ValtBaseWindow
{
    private CategoryTreeElement? _draggedItem;
    private CategoryTreeElement? _targetItem;
    private bool _isPotentialDrag;
    private Point _initialPoint;
    private ScrollViewer? _scrollViewer;
    
    public ManageCategoriesView()
    {
        InitializeComponent();
        
        Tree.AddHandler(DragDrop.DragOverEvent, TreeView_OnDragOver);
        Tree.AddHandler(DragDrop.DropEvent, TreeView_OnDrop);
        Tree.AddHandler(PointerPressedEvent, TreeView_OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        Tree.AddHandler(PointerMovedEvent, TreeView_OnPointerMoved, RoutingStrategies.Bubble, true);
        Tree.AddHandler(PointerReleasedEvent, TreeView_OnPointerReleased, RoutingStrategies.Bubble, true);
        Tree.AddHandler(PointerCaptureLostEvent, TreeView_OnPointerCaptureLost, RoutingStrategies.Bubble);
    }

    private void TreeView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            return;
        
        var treeView = sender as TreeView;
        var point = e.GetPosition(treeView);
        var hitTestResult = treeView?.InputHitTest(point);
        var element = hitTestResult as Visual;

        // Find the TreeViewItem under the pointer
        while (element != null && !(element is TreeViewItem))
        {
            element = element.GetVisualParent<Visual>();
        }

        if (element is TreeViewItem tvi)
        {
            _draggedItem = tvi.DataContext as CategoryTreeElement;
            if (_draggedItem != null && (_draggedItem.SubNodes is null || _draggedItem.SubNodes.Count == 0))
            {
                _isPotentialDrag = true;
                _initialPoint = point;
                e.Pointer.Capture(treeView);
            }
        }
    }
    
    private void TreeView_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPotentialDrag)
        {
            var treeView = sender as TreeView;
            var currentPoint = e.GetPosition(treeView);
            var distance = Point.Distance(currentPoint, _initialPoint);

            if (distance > 5) // Adjust this threshold as needed
            {
                _isPotentialDrag = false;
                if (_draggedItem is null) return;
#pragma warning disable CS0618 // DataObject and DragDrop.DoDragDrop are obsolete
                var data = new DataObject();
                data.Set("draggedItem", _draggedItem);
                DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618
                ResetDragState(e.Pointer);
            }
        }
    }
    
    private void TreeView_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPotentialDrag)
        {
            ResetDragState(e.Pointer);
        }
    }
    
    private void TreeView_OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isPotentialDrag)
        {
            ResetDragState(e.Pointer);
        }
    }

    private void ResetDragState(IPointer pointer)
    {
        _isPotentialDrag = false;
        _draggedItem = null;
        pointer.Capture(null);
    }
    
    private void TreeView_OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
        var data = e.Data.Get("draggedItem") as CategoryTreeElement;
#pragma warning restore CS0618

        if (data is null)
            return;

        var vm = DataContext as ManageCategoriesViewModel;
        if (vm is null)
            return;

        if (_targetItem is null || (_targetItem != null && _targetItem != data && _targetItem.ParentId == null))
        {
            _ = vm.ChangeCategoryParent(data.Id, _targetItem?.Id);
        }

        _targetItem = null;
    }

    private void TreeView_OnDragOver(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
        var data = e.Data.Get("draggedItem") as CategoryTreeElement;
#pragma warning restore CS0618
        
        if (data is null)
            return;
        
        var treeView = sender as TreeView;
        var point = e.GetPosition(treeView!);
        var hitTestResult = treeView!.InputHitTest(point);
        var element = hitTestResult as Visual;

        //find the TreeViewItem under the pointer
        while (element != null && !(element is TreeViewItem))
        {
            element = element.GetVisualParent<Visual>();
        }

        if (element is TreeViewItem tvi)
        {
            _targetItem = tvi.DataContext as CategoryTreeElement;
            //prevent dropping onto itself or in a child item
            if (_targetItem != null && _targetItem != data && _targetItem.ParentId == null) 
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            //dropping outside any item moves it to root
            _targetItem = null;
            e.DragEffects = DragDropEffects.Move;
        }
        
        if (_scrollViewer == null)
            _scrollViewer = GetScrollViewer(Tree);
        
        // Auto-scroll logic
        if (_scrollViewer != null)
        {
            var scrollMargin = 20; // Distance from edge to trigger scrolling (in pixels)
            var scrollStep = 10;   // Amount to scroll each time (in pixels)

            if (point.Y < scrollMargin)
            {
                // Scroll up
                var newOffset = Math.Max(0, _scrollViewer.Offset.Y - scrollStep);
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, newOffset);
            }
            else if (point.Y > treeView!.Bounds.Height - scrollMargin)
            {
                // Scroll down
                var maxOffset = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;
                var newOffset = Math.Min(maxOffset, _scrollViewer.Offset.Y + scrollStep);
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, newOffset);
            }
        }
    }
    
    private ScrollViewer? GetScrollViewer(Visual visual)
    {
        if (visual is ScrollViewer scrollViewer)
            return scrollViewer;

        foreach (var visualChild in visual.GetVisualChildren())
        {
            var result = GetScrollViewer(visualChild);
            if (result != null)
                return result;
        }

        return null;
    }

    private void Tree_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ManageCategoriesViewModel vm && sender is TreeView treeView)
        {
            vm.SelectedCategory = treeView.SelectedItem as CategoryTreeElement;
        }
    }
}