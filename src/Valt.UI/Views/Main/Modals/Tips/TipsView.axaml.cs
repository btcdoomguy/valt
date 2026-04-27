using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.Tips;

public partial class TipsView : ValtBaseWindow
{
    private PropertyChangedEventHandler? _propertyChangedHandler;

    public TipsView()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Design.IsDesignMode) return;

        var vm = (TipsViewModel)DataContext!;
        RenderTipText(vm.CurrentTipText);

        _propertyChangedHandler = (sender, args) =>
        {
            if (args.PropertyName == nameof(TipsViewModel.CurrentTipText))
                RenderTipText(vm.CurrentTipText);
        };

        vm.PropertyChanged += _propertyChangedHandler;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is TipsViewModel vm && _propertyChangedHandler is not null)
        {
            vm.PropertyChanged -= _propertyChangedHandler;
            _propertyChangedHandler = null;
        }
    }

    private void RenderTipText(string? rawText)
    {
        var tb = this.FindControl<TextBlock>("TipTextBlock")!;
        tb.Inlines ??= new InlineCollection();
        tb.Inlines.Clear();

        if (string.IsNullOrEmpty(rawText))
            return;

        var remaining = rawText;
        var isBold = false;

        while (remaining.Length > 0)
        {
            var idx = remaining.IndexOf("**", StringComparison.Ordinal);
            if (idx < 0)
            {
                tb.Inlines.Add(new Run(remaining)
                {
                    FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal
                });
                break;
            }

            if (idx > 0)
            {
                tb.Inlines.Add(new Run(remaining[..idx])
                {
                    FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal
                });
            }

            isBold = !isBold;
            remaining = remaining[(idx + 2)..];
        }
    }
}
