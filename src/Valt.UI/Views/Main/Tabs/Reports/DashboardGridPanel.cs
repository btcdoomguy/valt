using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class DashboardGridPanel : Panel
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<DashboardGridPanel, double>(nameof(ItemWidth), 320d);

    public static readonly StyledProperty<double> HorizontalSpacingProperty =
        AvaloniaProperty.Register<DashboardGridPanel, double>(nameof(HorizontalSpacing), 10d);

    public static readonly StyledProperty<double> VerticalSpacingProperty =
        AvaloniaProperty.Register<DashboardGridPanel, double>(nameof(VerticalSpacing), 10d);

    public static readonly StyledProperty<int> MinColumnsProperty =
        AvaloniaProperty.Register<DashboardGridPanel, int>(nameof(MinColumns), 3);

    public static readonly AttachedProperty<int> RowSpanProperty =
        AvaloniaProperty.RegisterAttached<DashboardGridPanel, Control, int>("RowSpan", 1);

    public static readonly IValueConverter IsLargeToRowSpanConverter = new IsLargeRowSpanConverter();

    static DashboardGridPanel()
    {
        AffectsMeasure<DashboardGridPanel>(ItemWidthProperty, HorizontalSpacingProperty, VerticalSpacingProperty, MinColumnsProperty);
        RowSpanProperty.Changed.AddClassHandler<Control>((control, _) =>
        {
            if (control.GetVisualParent() is DashboardGridPanel panel)
                panel.InvalidateMeasure();
        });
    }

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double HorizontalSpacing
    {
        get => GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public int MinColumns
    {
        get => GetValue(MinColumnsProperty);
        set => SetValue(MinColumnsProperty, value);
    }

    public static int GetRowSpan(Control control) => control.GetValue(RowSpanProperty);
    public static void SetRowSpan(Control control, int value) => control.SetValue(RowSpanProperty, value);

    private (int column, int row)[] _placements = Array.Empty<(int, int)>();
    private int _columns;
    private double _rowHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemWidth = ItemWidth;
        var hSpacing = HorizontalSpacing;
        var vSpacing = VerticalSpacing;
        var minCols = Math.Max(1, MinColumns);

        var availableWidth = double.IsInfinity(availableSize.Width) ? itemWidth * minCols + hSpacing * (minCols - 1) : availableSize.Width;

        var columns = Math.Max(minCols, (int)((availableWidth + hSpacing) / (itemWidth + hSpacing)));
        _columns = columns;

        var visibleChildren = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            visibleChildren++;
        }

        if (_placements.Length < Children.Count)
            _placements = new (int, int)[Children.Count];

        var childMeasure = new Size(itemWidth, double.PositiveInfinity);
        double maxSingleRowHeight = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(childMeasure);
            var span = Math.Max(1, GetRowSpan(child));
            if (span == 1 && child.DesiredSize.Height > maxSingleRowHeight)
                maxSingleRowHeight = child.DesiredSize.Height;
        }

        if (maxSingleRowHeight <= 0)
        {
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                var span = Math.Max(1, GetRowSpan(child));
                var approx = child.DesiredSize.Height / span;
                if (approx > maxSingleRowHeight)
                    maxSingleRowHeight = approx;
            }
        }

        if (maxSingleRowHeight <= 0)
            maxSingleRowHeight = 150;

        _rowHeight = maxSingleRowHeight + 20;

        var occupancy = new bool[columns, Math.Max(1, visibleChildren * 2 + 2)];
        var totalRows = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            if (!Children[i].IsVisible)
                _placements[i] = (-1, -1);
        }

        var order = new int[Children.Count];
        for (var i = 0; i < order.Length; i++) order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            var visA = Children[a].IsVisible;
            var visB = Children[b].IsVisible;
            if (visA != visB) return visA ? -1 : 1;
            if (!visA) return a.CompareTo(b);
            var spanA = Math.Max(1, GetRowSpan(Children[a]));
            var spanB = Math.Max(1, GetRowSpan(Children[b]));
            if (spanA != spanB) return spanB.CompareTo(spanA);
            return a.CompareTo(b);
        });

        foreach (var childIndex in order)
        {
            var child = Children[childIndex];
            if (!child.IsVisible) continue;

            var span = Math.Max(1, GetRowSpan(child));
            span = Math.Min(span, occupancy.GetLength(1));

            var placed = false;
            for (var r = 0; r < occupancy.GetLength(1) - span + 1 && !placed; r++)
            {
                for (var c = 0; c < columns && !placed; c++)
                {
                    var fits = true;
                    for (var s = 0; s < span; s++)
                    {
                        if (occupancy[c, r + s]) { fits = false; break; }
                    }
                    if (!fits) continue;

                    for (var s = 0; s < span; s++)
                        occupancy[c, r + s] = true;

                    _placements[childIndex] = (c, r);
                    if (r + span > totalRows) totalRows = r + span;
                    placed = true;
                }
            }

            if (!placed)
                _placements[childIndex] = (0, 0);
        }

        var desiredWidth = columns * itemWidth + (columns - 1) * hSpacing;
        var desiredHeight = totalRows <= 0 ? 0 : totalRows * _rowHeight + (totalRows - 1) * vSpacing;
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = ItemWidth;
        var hSpacing = HorizontalSpacing;
        var vSpacing = VerticalSpacing;

        var maxRow = 0;
        var childIndex = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible || childIndex >= _placements.Length)
            {
                childIndex++;
                continue;
            }

            var (col, row) = _placements[childIndex];
            if (col < 0)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                childIndex++;
                continue;
            }

            var span = Math.Max(1, GetRowSpan(child));
            var x = col * (itemWidth + hSpacing);
            var y = row * (_rowHeight + vSpacing);
            var height = span * _rowHeight + (span - 1) * vSpacing;
            child.Arrange(new Rect(x, y, itemWidth, height));

            if (row + span > maxRow) maxRow = row + span;
            childIndex++;
        }

        var width = _columns * itemWidth + (_columns - 1) * hSpacing;
        var height2 = maxRow <= 0 ? 0 : maxRow * _rowHeight + (maxRow - 1) * vSpacing;
        return new Size(width, height2);
    }

    private sealed class IsLargeRowSpanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? 2 : 1;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
