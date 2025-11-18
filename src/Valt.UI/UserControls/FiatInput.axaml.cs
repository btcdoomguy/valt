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

public partial class FiatInput : UserControl
{
    private FiatValue _fiatValue = FiatValue.Empty;
    private int _decimalPlaces = 2;
    private string _currencySymbol = "$";
    private bool _symbolOnRight = false;
    private long _rawValue = 0; // Internal value in smallest unit (e.g., cents)
    private string _displayValue = "0.00";
    private TextBox? _textBox;

    public FiatInput()
    {
        InitializeComponent();
        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("TextBoxInput");
        if (_textBox != null)
        {
            _textBox.GotFocus += InputBox_GotFocus;
            _textBox.LostFocus += InputBox_LostFocus;
        }
    }

    #region Properties

    public static readonly DirectProperty<FiatInput, FiatValue> FiatValueProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, FiatValue>(
            nameof(FiatValue),
            o => o.FiatValue,
            (o, v) => o.FiatValue = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<FiatInput, string> DisplayValueProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, string>(
            nameof(DisplayValue),
            o => o.DisplayValue,
            (o, v) => o.DisplayValue = v);

    public static readonly DirectProperty<FiatInput, int> DecimalPlacesProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, int>(
            nameof(DecimalPlaces),
            o => o.DecimalPlaces,
            (o, v) => o.DecimalPlaces = v);

    public static readonly DirectProperty<FiatInput, string> CurrencySymbolProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, string>(
            nameof(CurrencySymbol),
            o => o.CurrencySymbol,
            (o, v) => o.CurrencySymbol = v);

    public static readonly DirectProperty<FiatInput, bool> SymbolOnRightProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, bool>(
            nameof(SymbolOnRight),
            o => o.SymbolOnRight,
            (o, v) => o.SymbolOnRight = v);
    
    
    public FiatValue FiatValue
    {
        get => _fiatValue;
        set
        {
            SetAndRaise(FiatValueProperty, ref _fiatValue, value);
            _rawValue = (long)(value.Value * (decimal)Math.Pow(10, _decimalPlaces));
            UpdateDisplayValue();
        }
    }

    public string DisplayValue
    {
        get => _displayValue;
        set
        {
            SetAndRaise(DisplayValueProperty, ref _displayValue, value);
            UpdateFiatValue();
        }
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            SetAndRaise(DecimalPlacesProperty, ref _decimalPlaces, value);
            UpdateDisplayValue();
        }
    }

    public string CurrencySymbol
    {
        get => _currencySymbol;
        set => SetAndRaise(CurrencySymbolProperty, ref _currencySymbol, value);
    }

    public bool SymbolOnRight
    {
        get => _symbolOnRight;
        set => SetAndRaise(SymbolOnRightProperty, ref _symbolOnRight, value);
    }
    
    #endregion

    #region Calculator

    public static readonly DirectProperty<FiatInput, ICommand> CalculatorCommandProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, ICommand>(
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

    public static readonly DirectProperty<FiatInput, object> CalculatorCommandParameterProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, object>(
            nameof(CalculatorCommandParameter),
            o => o.CalculatorCommandParameter,
            (o, v) => o.CalculatorCommandParameter = v);

    private object _calculatorCommandParameter;

    public object CalculatorCommandParameter
    {
        get => _calculatorCommandParameter;
        set => SetAndRaise(CalculatorCommandParameterProperty, ref _calculatorCommandParameter, value);
    }

    public static readonly DirectProperty<FiatInput, bool> IsCommandSetProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, bool>(
            nameof(IsCommandSet),
            o => o.IsCommandSet,
            unsetValue: false);

    private bool _isCommandSet;

    public bool IsCommandSet
    {
        get => _isCommandSet;
        private set => SetAndRaise(IsCommandSetProperty, ref _isCommandSet, value);
    }

    #endregion

    #region InputFocused

    public static readonly DirectProperty<FiatInput, bool> IsInputFocusedProperty =
        AvaloniaProperty.RegisterDirect<FiatInput, bool>(
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
    
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateDisplayValue();
    }
    
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && char.IsDigit(e.Text[0]))
        {
            _rawValue = _rawValue * 10 + long.Parse(e.Text);
            UpdateDisplayValue();
        }

        if (_textBox is not null)
        {
            _textBox.CaretIndex = _textBox.Text!.Length;
            _textBox.SelectionStart = _textBox.Text.Length;
            _textBox.SelectionEnd = _textBox.Text.Length;
        }

        e.Handled = true; // Prevent default text input handling
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var sourceText = e.Source as TextBox;
        switch (e.Key)
        {
            case Key.Back:
            {
                if (sourceText!.SelectedText == sourceText.Text)
                {
                    _rawValue = 0;
                    UpdateDisplayValue();
                }

                if (_rawValue > 0)
                {
                    _rawValue /= 10;
                    UpdateDisplayValue();
                }

                e.Handled = true;
                break;
            }
            case Key.Tab or Key.Escape or Key.Left or Key.Right:
                e.Handled = false; // Allow tab navigation
                break;
            case >= Key.D0 and <= Key.D9:
            // Allow numeric keys
            case >= Key.NumPad0 and <= Key.NumPad9:
            {
                if (sourceText!.SelectedText == sourceText.Text)
                    _rawValue = 0;

                e.Handled = false; // Let TextInput handle numbers
                break;
            }
            default:
                e.Handled = true; // Block other keys
                break;
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
        _textBox?.Focus();
        _textBox?.SelectAll();
    }
    
    #endregion

    private void UpdateFiatValue()
    {
        decimal value = _rawValue / (decimal)Math.Pow(10, _decimalPlaces);
        if (value != _fiatValue.Value)
            FiatValue = FiatValue.New(value);
    }

    private void UpdateDisplayValue()
    {
        var displayDecimal = _rawValue / (decimal)Math.Pow(10, _decimalPlaces);
        var format = $"N{_decimalPlaces}";
        _displayValue = displayDecimal.ToString(format, CultureInfo.CurrentUICulture);
        if (_textBox != null)
        {
            _textBox.Text = _displayValue;
        }

        SetCurrentValue(DisplayValueProperty, _displayValue);
        UpdateFiatValue();
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        if (property == FiatValueProperty)
        {
            DataValidationErrors.SetError(this, error);
        }
    }
}