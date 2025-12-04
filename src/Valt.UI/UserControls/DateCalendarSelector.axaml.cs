using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Input;
using Valt.UI.Converters;

namespace Valt.UI.UserControls;

public partial class DateCalendarSelector : UserControl
{
    private string _displayValue = string.Empty;
    private DateCalendarSelectorMode _selectorMode = DateCalendarSelectorMode.Month;
    private DateTime _baseDate = DateTime.Now.Date;

    private DateRange _range = new(DateTime.MinValue, DateTime.MinValue);

    public ObservableCollection<object> SelectorMenuItems { get; } = new();

    public static readonly DirectProperty<DateCalendarSelector, DateCalendarSelectorMode> SelectorModeProperty =
        AvaloniaProperty.RegisterDirect<DateCalendarSelector, DateCalendarSelectorMode>(
            nameof(SelectorMode),
            o => o.SelectorMode,
            (o, v) => o.SelectorMode = v,
            enableDataValidation: true,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<DateCalendarSelector, DateTime> DateProperty =
        AvaloniaProperty.RegisterDirect<DateCalendarSelector, DateTime>(
            nameof(Date),
            o => o.Date,
            (o, v) => o.Date = v,
            enableDataValidation: true,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<DateCalendarSelector, string> DisplayValueProperty =
        AvaloniaProperty.RegisterDirect<DateCalendarSelector, string>(
            nameof(DisplayValue),
            o => o.DisplayValue,
            (o, v) => o.DisplayValue = v);

    public static readonly DirectProperty<DateCalendarSelector, DateRange> RangeProperty =
        AvaloniaProperty.RegisterDirect<DateCalendarSelector, DateRange>(
            nameof(DateRange),
            o => o.Range,
            (o, v) => o.Range = v,
            defaultBindingMode: BindingMode.OneWayToSource);

    public static readonly StyledProperty<AvaloniaList<DateCalendarSelectorMode>> AllowedSelectorModesProperty =
        AvaloniaProperty.Register<DateCalendarSelector, AvaloniaList<DateCalendarSelectorMode>>(
            nameof(AllowedSelectorModes));

    static DateCalendarSelector()
    {
        AllowedSelectorModesProperty.Changed.AddClassHandler<DateCalendarSelector>(OnAllowedSelectorModesChanged);
    }
    
    private static void OnAllowedSelectorModesChanged(DateCalendarSelector instance, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is IAvaloniaList<DateCalendarSelectorMode> oldList)
        {
            oldList.CollectionChanged -= instance.OnAllowedModesCollectionChanged;
        }

        if (e.NewValue is IAvaloniaList<DateCalendarSelectorMode> newList)
        {
            newList.CollectionChanged += instance.OnAllowedModesCollectionChanged;
            instance.RebuildMenuItems();

            // Adjust SelectorMode if invalid (like in Aura.UI CardControl)
            if (newList.Count > 0 && !newList.Contains(instance.SelectorMode))
            {
                instance.SelectorMode = newList[0];
            }
        }
    }
    
    private void OnAllowedModesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildMenuItems();

        if (AllowedSelectorModes.Count > 0 && !AllowedSelectorModes.Contains(SelectorMode))
        {
            SelectorMode = AllowedSelectorModes[0];
        }
    }

    public AvaloniaList<DateCalendarSelectorMode> AllowedSelectorModes
    {
        get => GetValue(AllowedSelectorModesProperty);
        set => SetValue(AllowedSelectorModesProperty, value);
    }

    private void RebuildMenuItems()
    {
        SelectorMenuItems.Clear();

        var modes = AllowedSelectorModes ?? new AvaloniaList<DateCalendarSelectorMode>();  // Safe fallback

        // Optional: Dedupe modes to prevent any navigation glitches
        var uniqueModes = modes.Distinct().ToArray();

        foreach (var mode in uniqueModes)
        {
            SelectorMenuItems.Add(new MenuItem
            {
                Header = mode.ToString(),
                Command = ChangeSelectorModeCommand,
                CommandParameter = mode,
                [!MenuItem.IsCheckedProperty] = new Binding
                {
                    Source = this,
                    Path = nameof(SelectorMode),
                    Converter = new EnumEqualityConverter(),
                    ConverterParameter = mode
                }
            });
        }

        if (uniqueModes.Any())
        {
            SelectorMenuItems.Add(new Separator());
        }

        SelectorMenuItems.Add(new MenuItem
        {
            Header = "Reset date",
            Command = ResetDateCommand
        });
    }
    
    public DateCalendarSelectorMode SelectorMode
    {
        get => _selectorMode;
        set
        {
            this.SetAndRaise(SelectorModeProperty, ref _selectorMode, value);
            UpdateDisplayValue();
        }
    }

    public DateTime Date
    {
        get => _baseDate;
        set
        {
            this.SetAndRaise(DateProperty, ref _baseDate, value);
            UpdateDisplayValue();
        }
    }

    public string DisplayValue
    {
        get => _displayValue;
        set { this.SetAndRaise(DisplayValueProperty, ref _displayValue, value); }
    }

    public DateRange Range
    {
        get => _range;
        set { this.SetAndRaise(RangeProperty, ref _range, value); }
    }

    public DateCalendarSelector()
    {
        InitializeComponent();
        
        AllowedSelectorModes = new AvaloniaList<DateCalendarSelectorMode>();
        
        UpdateDisplayValue();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // Populate defaults ONLY if no custom modes were added by XAML
        if (AllowedSelectorModes.Count == 0)
        {
            AllowedSelectorModes.AddRange(new[]
            {
                DateCalendarSelectorMode.All,
                DateCalendarSelectorMode.Year,
                DateCalendarSelectorMode.Month,
                DateCalendarSelectorMode.Week,
                DateCalendarSelectorMode.Day
            });
        }
        
        RebuildMenuItems();
    }

    [RelayCommand]
    private void PreviousPeriod()
    {
        switch (SelectorMode)
        {
            case DateCalendarSelectorMode.All:
                break;
            case DateCalendarSelectorMode.Year:
                Date = Date.AddYears(-1);
                break;
            case DateCalendarSelectorMode.Month:
                Date = Date.AddMonths(-1);
                break;
            case DateCalendarSelectorMode.Week:
                Date = Date.AddDays(-7);
                break;
            case DateCalendarSelectorMode.Day:
                Date = Date.AddDays(-1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void NextPeriod()
    {
        switch (SelectorMode)
        {
            case DateCalendarSelectorMode.All:
                break;
            case DateCalendarSelectorMode.Year:
                Date = Date.AddYears(1);
                break;
            case DateCalendarSelectorMode.Month:
                Date = Date.AddMonths(1);
                break;
            case DateCalendarSelectorMode.Week:
                Date = Date.AddDays(7);
                break;
            case DateCalendarSelectorMode.Day:
                Date = Date.AddDays(1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void ChangeSelectorMode(DateCalendarSelectorMode mode)
    {
        SelectorMode = mode;

        UpdateDisplayValue();
    }

    [RelayCommand]
    private void ResetDate()
    {
        Date = DateTime.Now.Date;

        UpdateDisplayValue();
    }

    private void UpdateDisplayValue()
    {
        DateTime start, end;

        switch (SelectorMode)
        {
            case DateCalendarSelectorMode.All:
                DisplayValue = "All";
                start = DateTime.MinValue;
                end = DateTime.MaxValue;
                break;
            case DateCalendarSelectorMode.Year:
                DisplayValue = Date.Year.ToString();
                start = new DateTime(Date.Year, 1, 1);
                end = new DateTime(Date.Year, 12, 31);
                break;
            case DateCalendarSelectorMode.Month:
                DisplayValue = Date.ToString("MMMM yyyy");
                start = new DateTime(Date.Year, Date.Month, 1);
                end = start.AddMonths(1).AddDays(-1);
                break;
            case DateCalendarSelectorMode.Week:
                DisplayValue = GetWeekRangeDisplay();
                start = Date.AddDays(-(int)Date.DayOfWeek);
                end = start.AddDays(6);
                break;
            case DateCalendarSelectorMode.Day:
                DisplayValue = Date.ToString("dddd, MMMM d");
                start = Date;
                end = Date;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Range = new DateRange(start, end);
    }

    private string GetWeekRangeDisplay()
    {
        // Calculate start of week (Sunday)
        var startOfWeek = Date.AddDays(-(int)Date.DayOfWeek);
        // Calculate end of week (Saturday)
        var endOfWeek = startOfWeek.AddDays(6);
        // Format as "8/10/25 - 8/16/25" (adjust format as needed)
        return $"{startOfWeek:M/d/yy} - {endOfWeek:M/d/yy}";
    }

    private void PopupFlyoutBase_OnOpening(object? sender, EventArgs e)
    {
        RebuildMenuItems();
    }
}

public enum DateCalendarSelectorMode
{
    All,
    Year,
    Month,
    Week,
    Day
}

public record DateRange(DateTime Start, DateTime End);