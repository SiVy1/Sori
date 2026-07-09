using System;
using Avalonia;
using Avalonia.Controls;

namespace App.Controls;

public class AdaptiveGrid : Panel
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<AdaptiveGrid, double>(nameof(ItemWidth), 160);

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemWidth = Math.Max(1, ItemWidth);
        var columns = Math.Max(1, (int)(availableSize.Width / itemWidth));
        var childAvailable = new Size(availableSize.Width / columns, double.PositiveInfinity);

        double maxHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(childAvailable);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        var rows = (int)Math.Ceiling((double)Children.Count / columns);
        return new Size(availableSize.Width, rows * maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = Math.Max(1, ItemWidth);
        var columns = Math.Max(1, (int)(finalSize.Width / itemWidth));
        var cellWidth = finalSize.Width / columns;

        double currentY = 0;
        double currentRowHeight = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var col = i % columns;

            if (col == 0 && i > 0)
            {
                currentY += currentRowHeight;
                currentRowHeight = 0;
            }

            currentRowHeight = Math.Max(currentRowHeight, child.DesiredSize.Height);
            var rect = new Rect(col * cellWidth, currentY, cellWidth, child.DesiredSize.Height);
            child.Arrange(rect);
        }

        return finalSize;
    }
}
