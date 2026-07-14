using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.DateSoldPrompt;

public partial class DateSoldPromptView : ValtBaseWindow
{
    public DateSoldPromptView()
    {
        InitializeComponent();

        this.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not DateSoldPromptViewModel viewModel)
            return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.Left:
                    viewModel.PreviousDayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Right:
                    viewModel.NextDayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R:
                    viewModel.SelectTodayCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    viewModel.OkCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Enter)
        {
            viewModel.OkCommand.Execute(null);
            e.Handled = true;
        }
    }
}
