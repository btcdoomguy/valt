using System;
using Avalonia.Controls;
using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.ConversionCalculator;

public partial class ConversionCalculatorView : ValtBaseWindow
{
    public ConversionCalculatorView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Focus the main border so keyboard events work immediately
        var mainBorder = this.FindControl<Border>("MainBorder");
        mainBorder?.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ConversionCalculatorViewModel vm)
            return;

        var handled = true;
        var hasShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            // Shift+digit for special characters (must come before plain digits)
            case Key.D8 when hasShift:
                vm.AppendCharacterCommand.Execute("×");
                break;
            case Key.D9 when hasShift:
                vm.AppendCharacterCommand.Execute("(");
                break;
            case Key.D0 when hasShift:
                vm.AppendCharacterCommand.Execute(")");
                break;

            // Digits
            case Key.D0 or Key.NumPad0:
                vm.AppendCharacterCommand.Execute("0");
                break;
            case Key.D1 or Key.NumPad1:
                vm.AppendCharacterCommand.Execute("1");
                break;
            case Key.D2 or Key.NumPad2:
                vm.AppendCharacterCommand.Execute("2");
                break;
            case Key.D3 or Key.NumPad3:
                vm.AppendCharacterCommand.Execute("3");
                break;
            case Key.D4 or Key.NumPad4:
                vm.AppendCharacterCommand.Execute("4");
                break;
            case Key.D5 or Key.NumPad5:
                vm.AppendCharacterCommand.Execute("5");
                break;
            case Key.D6 or Key.NumPad6:
                vm.AppendCharacterCommand.Execute("6");
                break;
            case Key.D7 or Key.NumPad7:
                vm.AppendCharacterCommand.Execute("7");
                break;
            case Key.D8 or Key.NumPad8:
                vm.AppendCharacterCommand.Execute("8");
                break;
            case Key.D9 or Key.NumPad9:
                vm.AppendCharacterCommand.Execute("9");
                break;

            // Operators
            case Key.Add or Key.OemPlus:
                vm.AppendCharacterCommand.Execute("+");
                break;
            case Key.Subtract or Key.OemMinus:
                vm.AppendCharacterCommand.Execute("-");
                break;
            case Key.Multiply:
                vm.AppendCharacterCommand.Execute("×");
                break;
            case Key.Divide or Key.OemQuestion:
                vm.AppendCharacterCommand.Execute("÷");
                break;

            // Decimal
            case Key.OemPeriod or Key.OemComma or Key.Decimal:
                vm.AppendDecimalCommand.Execute(null);
                break;

            // Actions
            case Key.Back:
                vm.BackspaceCommand.Execute(null);
                break;
            case Key.Delete:
                vm.ClearCommand.Execute(null);
                break;
            case Key.Enter:
                vm.EqualsCommand.Execute(null);
                break;
            case Key.Escape:
                vm.CloseCommand.Execute(null);
                break;

            default:
                handled = false;
                break;
        }

        e.Handled = handled;
    }
}
