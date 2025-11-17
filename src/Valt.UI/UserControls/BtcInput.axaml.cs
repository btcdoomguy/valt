using System;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Valt.Core.Common;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Valt.UI.UserControls;

public partial class BtcInput : UserControl
{
    private BtcValue _btcValue = BtcValue.Empty;
    private bool _isBitcoin;
    private string _displayValue = string.Empty;
    
    public BtcInput()
    {
        InitializeComponent();
        IsBitcoin = false; // Default to Sats.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _inputBox = e.NameScope.Find<TextBox>("InputBox");
        if (_inputBox != null)
        {
            _inputBox.GotFocus += InputBox_GotFocus;
            _inputBox.LostFocus += InputBox_LostFocus;
        }
    }
    
    #region Properties
    
    public static readonly DirectProperty<BtcInput, BtcValue> BtcValueProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, BtcValue>(
            nameof(BtcValue),
            o => o.BtcValue,
            (o, v) => o.BtcValue = v,
            enableDataValidation: true,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<BtcInput, bool> IsBitcoinProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, bool>(
            nameof(IsBitcoin),
            o => o.IsBitcoin,
            (o, v) => o.IsBitcoin = v);

    public static readonly DirectProperty<BtcInput, string> DisplayValueProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, string>(nameof(DisplayValue), o => o.DisplayValue,
            (o, v) => o.DisplayValue = v);
    
    public BtcValue BtcValue
    {
        get => _btcValue;
        set
        {
            this.SetAndRaise(BtcValueProperty, ref _btcValue, value);
            UpdateDisplayValue();
        }
    }

    public string DisplayValue
    {
        get => _displayValue;
        set
        {
            this.SetAndRaise(DisplayValueProperty, ref _displayValue, value);
            UpdateBtcValue();
        }
    }

    public bool IsBitcoin
    {
        get => _isBitcoin;
        set
        {
            SetAndRaise(IsBitcoinProperty, ref _isBitcoin, value);
            UpdateDisplayValue();
        }
    }
    
    #endregion
    
    #region Calculator
    
    public static readonly DirectProperty<BtcInput, ICommand> CalculatorCommandProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, ICommand>(
            nameof(CalculatorCommand),
            o => o.CalculatorCommand,
            (o, v) => o.CalculatorCommand = v);

    private ICommand _calculatorCommand;

    public ICommand CalculatorCommand
    {
        get => _calculatorCommand;
        set
        {
            SetAndRaise(CalculatorCommandProperty, ref _calculatorCommand, value);
            SetAndRaise(IsCommandSetProperty, ref _isCommandSet, value != null);
        }
    }
    
    public static readonly DirectProperty<BtcInput, object> CalculatorCommandParameterProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, object>(
            nameof(CalculatorCommandParameter),
            o => o.CalculatorCommandParameter,
            (o, v) => o.CalculatorCommandParameter = v);

    private object _calculatorCommandParameter;

    public object CalculatorCommandParameter
    {
        get => _calculatorCommandParameter;
        set => SetAndRaise(CalculatorCommandParameterProperty, ref _calculatorCommandParameter, value);
    }
    
    public static readonly DirectProperty<BtcInput, bool> IsCommandSetProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, bool>(
            nameof(IsCommandSet),
            o => o.IsCommandSet,
            unsetValue: false); // Default to false when unset

    private bool _isCommandSet;

    public bool IsCommandSet
    {
        get => _isCommandSet;
        private set => SetAndRaise(IsCommandSetProperty, ref _isCommandSet, value);
    }
    
    #endregion
    
    #region InputFocused
    
    private TextBox? _inputBox;
    
    public static readonly DirectProperty<BtcInput, bool> IsInputFocusedProperty =
        AvaloniaProperty.RegisterDirect<BtcInput, bool>(
            nameof(IsInputFocused),
            o => o.IsInputFocused,
            (o, v) => o.IsInputFocused = v);

    private bool _isInputFocused;

    public bool IsInputFocused
    {
        get => _isInputFocused;
        private set => SetAndRaise(IsInputFocusedProperty, ref _isInputFocused, value);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        var textBox = sender as TextBox;

        if (textBox is null)
            return;

        if (e.Key == Key.Tab)
        {
            e.Handled = false;
            return;
        }

        var validChar = ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                         (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key == Key.OemPeriod));

        if (!validChar ||
            e.Key == Key.OemPeriod && !_isBitcoin ||
            e.Key == Key.OemPeriod && _isBitcoin &&
            textBox.Text!.Contains(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
        {
            e.Handled = true;
        }
    }
    
    private void InputBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        IsInputFocused = true;
    }

    private void InputBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        IsInputFocused = false;
    }
    
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        _inputBox?.Focus();
        _inputBox?.SelectAll();
    }
    
    #endregion

    private void UpdateBtcValue()
    {
        if (decimal.TryParse(_displayValue, out decimal valueAsDecimal))
        {
            long valueInSats = _isBitcoin ? (long)(valueAsDecimal * 100000000m) : (long)valueAsDecimal;
            if (valueInSats != BtcValue.Sats)
                BtcValue = BtcValue.New(valueInSats);
        }
    }

    private void UpdateDisplayValue()
    {
        var newValue = _isBitcoin ? BtcValue.ToBitcoinString() : BtcValue.ToString();
        this.SetAndRaise(DisplayValueProperty, ref _displayValue, newValue);
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        if (property == BtcValueProperty)
        {
            DataValidationErrors.SetError(this, error);
        }
    }
}