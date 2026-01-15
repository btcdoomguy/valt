using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Valt.UI.Services.MessageBoxes;

public partial class ValtMessageBox : Window
{
    public enum MessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Question
    }

    public enum MessageBoxButtons
    {
        Ok,
        OkCancel,
        YesNo
    }

    public enum MessageBoxResult
    {
        None,
        Ok,
        Cancel,
        Yes,
        No
    }

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    private MessageBoxButtons _buttons;
    private Button? _focusButton;

    public ValtMessageBox()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    public void Configure(string title, string message, MessageBoxIcon icon, MessageBoxButtons buttons)
    {
        TitleText.Text = title;
        MessageText.Text = message;
        _buttons = buttons;

        // Set icon
        SetIcon(icon);

        // Create buttons
        CreateButtons(buttons);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Focus the safe button after the window opens
        _focusButton?.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // ESC triggers the "safe" result based on button type
            Result = _buttons switch
            {
                MessageBoxButtons.Ok => MessageBoxResult.Ok,           // Alert/Error - OK is the only option
                MessageBoxButtons.OkCancel => MessageBoxResult.Cancel, // Cancel is the safe option
                MessageBoxButtons.YesNo => MessageBoxResult.No,        // No is the safe option
                _ => MessageBoxResult.None
            };
            Close();
            e.Handled = true;
        }
    }

    private void SetIcon(MessageBoxIcon icon)
    {
        // Material Design icon codes
        var (iconChar, resourceKey) = icon switch
        {
            MessageBoxIcon.Info => ("\uE88E", "MessageBoxInfoBrush"),      // info icon - blue
            MessageBoxIcon.Warning => ("\uE002", "MessageBoxWarningBrush"),   // warning icon - orange
            MessageBoxIcon.Error => ("\uE000", "MessageBoxErrorBrush"),     // error icon - red
            MessageBoxIcon.Question => ("\uE887", "MessageBoxQuestionBrush"), // help icon - purple
            _ => ("", (string?)null)
        };

        IconText.Text = iconChar;
        if (resourceKey != null && Application.Current!.TryGetResource(resourceKey, Avalonia.Styling.ThemeVariant.Default, out var brush))
            IconText.Foreground = (IBrush)brush!;
        else
            IconText.Foreground = new SolidColorBrush(Colors.White);
        IconText.IsVisible = icon != MessageBoxIcon.None;
    }

    private void CreateButtons(MessageBoxButtons buttons)
    {
        ButtonPanel.Children.Clear();
        _focusButton = null;

        switch (buttons)
        {
            case MessageBoxButtons.Ok:
                // OK is the only option, so it gets focus
                AddButton("OK", MessageBoxResult.Ok, isAccent: true, shouldFocus: true);
                break;
            case MessageBoxButtons.OkCancel:
                // Cancel is the safe option, so it gets focus
                AddButton("OK", MessageBoxResult.Ok, isAccent: true, shouldFocus: false);
                AddButton("Cancel", MessageBoxResult.Cancel, isAccent: false, shouldFocus: true);
                break;
            case MessageBoxButtons.YesNo:
                // No is the safe option, so it gets focus
                AddButton("Yes", MessageBoxResult.Yes, isAccent: true, shouldFocus: false);
                AddButton("No", MessageBoxResult.No, isAccent: false, shouldFocus: true);
                break;
        }
    }

    private void AddButton(string text, MessageBoxResult result, bool isAccent, bool shouldFocus)
    {
        var button = new Button
        {
            Content = text,
            Width = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        if (isAccent)
        {
            button.Classes.Add("accent");
        }

        if (shouldFocus)
        {
            _focusButton = button;
        }

        button.Click += (_, _) =>
        {
            Result = result;
            Close();
        };

        ButtonPanel.Children.Add(button);
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
