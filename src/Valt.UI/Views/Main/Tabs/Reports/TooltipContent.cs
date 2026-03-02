using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Valt.UI.Views.Main.Tabs.Reports;

public sealed record TooltipContent(IReadOnlyList<TooltipLine> Lines)
{
    public static TooltipContent Text(string text) =>
        new([new TooltipLine([new TooltipRun(text)])]);

    public static implicit operator Control(TooltipContent content) => content.Build();

    public Control Build()
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var line in Lines)
        {
            var row = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var run in line.Runs)
            {
                row.Children.Add(new TextBlock
                {
                    Text = run.Text,
                    FontWeight = run.Bold ? FontWeight.Bold : FontWeight.Normal,
                    FontSize = 11
                });
            }
            panel.Children.Add(row);
        }
        return panel;
    }
}

public sealed record TooltipLine(IReadOnlyList<TooltipRun> Runs);

public sealed record TooltipRun(string Text, bool Bold = false);
