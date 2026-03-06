using System;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.Tips;

public partial class TipsView : ValtBaseWindow
{
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

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TipsViewModel.CurrentTipText))
                RenderTipText(vm.CurrentTipText);
        };
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
