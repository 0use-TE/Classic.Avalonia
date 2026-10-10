using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Classic.CommonControls;

public class ToolbarPanel : Panel
{
    public static readonly StyledProperty<Panel?> OutOfBoundsPanelProperty = AvaloniaProperty.Register<ToolbarPanel, Panel?>(nameof(OutOfBoundsPanel));
    public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarPanel, bool>(nameof(IsOverflow));
    public static readonly StyledProperty<bool> WrapOnOverflowProperty = AvaloniaProperty.Register<ToolbarPanel, bool>(nameof(WrapOnOverflow));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<ToolbarPanel, double>(nameof(Spacing), 4);

    private readonly List<Control> overflowControls = new List<Control>();

    public Panel? OutOfBoundsPanel
    {
        get => (Panel?)GetValue(OutOfBoundsPanelProperty);
        set => SetValue(OutOfBoundsPanelProperty, value);
    }

    public bool IsOverflow
    {
        get => (bool)GetValue(IsOverflowProperty);
        set => SetValue(IsOverflowProperty, value);
    }

    public bool WrapOnOverflow
    {
        get => (bool)GetValue(WrapOnOverflowProperty);
        set => SetValue(WrapOnOverflowProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }


    static ToolbarPanel()
    {

        OutOfBoundsPanelProperty.Changed.AddClassHandler<ToolbarPanel>((panel, e) => panel.OnChangedPanel(e));
        AffectsMeasure<ToolbarPanel>(SpacingProperty, WrapOnOverflowProperty);
        AffectsArrange<ToolbarPanel>(SpacingProperty);
    }

    private void OnChangedPanel(AvaloniaPropertyChangedEventArgs changed)
    {
        var newPanel = changed.NewValue as Panel;
        if (newPanel == null)
            return;
        newPanel.AttachedToVisualTree += NewPanelOnAttachedToVisualTree;
    }

    private void NewPanelOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        foreach (var child in overflowControls)
        {
            OutOfBoundsPanel!.Children.Insert(0, child);
            // @todo ava11: is that necessary?
            child.ApplyStyling();
        }
        overflowControls.Clear();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (OutOfBoundsPanel is { } panel)
        {
            for (var index = panel.Children.Count - 1; index >= 0; index--)
            {
                var child = panel.Children[0];
                panel.Children.RemoveAt(0);
                Children.Add(child);
            }
        }

        if (overflowControls.Count > 0)
        {
            foreach (var control in overflowControls)
                Children.Add(control);
            overflowControls.Clear();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (WrapOnOverflow)
            return MeasureWithWrapOverride(availableSize);
        var spacing = Spacing;
        double maxDesiredWidth = 0;
        double maxDesiredHeight = 0;
        var children = Children;

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null)
                continue;

            if (!child.IsVisible)
            {
                child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                continue;
            }

            if (child is ToolbarSpacer)
                continue;

            child.Measure(availableSize.WithWidth(double.PositiveInfinity));
            maxDesiredWidth += child.DesiredSize.Width + spacing;
            maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
        }

        foreach (var child in overflowControls)
        {
            child.Measure(availableSize.WithWidth(double.PositiveInfinity));
            maxDesiredWidth += child.DesiredSize.Width + spacing;
            maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
        }

        if (OutOfBoundsPanel != null)
        {
            // The flyout owns this panel's measure pass. Measuring it here with a
            // different constraint repeatedly invalidates the popup's layout.
            foreach (var child in OutOfBoundsPanel.Children.Where(child => child.IsVisible))
            {
                maxDesiredWidth += child.DesiredSize.Width + spacing;
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
            }
        }

        return new Size(Math.Max(0, maxDesiredWidth - spacing), maxDesiredHeight);
    }

    private Size MeasureWithWrapOverride(Size availableSize)
    {
        var children = Children;
        double x = 0;
        double y = 0;
        double maxWidth = availableSize.Width;
        double maxDesiredHeight = 0;
        double maxEffectiveWidth = maxWidth;
        int rows = 1;
        bool anyInRow = false;

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            child.Measure(availableSize.WithWidth(double.PositiveInfinity));

            maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
        }

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            var desiredSize = child.DesiredSize;
            if (anyInRow && x + desiredSize.Width > maxWidth)
            {
                maxEffectiveWidth = Math.Max(maxEffectiveWidth, x);
                anyInRow = false;
                x = 0;
                y += maxDesiredHeight;
                rows++;
            }
            else
                anyInRow = true;

            var finalChildWidth = Math.Max(1, Math.Min(desiredSize.Width, maxWidth - x));
            x += finalChildWidth;
        }

        return new Size(rows == 1 ? x : maxEffectiveWidth, rows * maxDesiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (WrapOnOverflow)
            return ArrangeWithWrapOverride(finalSize);

        var children = Children;
        Rect rcChild = new Rect(finalSize);
        double previousChildSize = 0.0;
        var spacing = Spacing;

        int spacerCount = 0;
        double totalDesiredWidth = 0;
        bool any = false;

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;
            if (child is ToolbarSpacer)
                spacerCount++;
            else
            {
                any = true;
                totalDesiredWidth += child.DesiredSize.Width + spacing;
            }
        }
        if (any)
            totalDesiredWidth -= spacing;

        double leftSpace = finalSize.Width - totalDesiredWidth;
        var overflowWidth = overflowControls.Concat(OutOfBoundsPanel?.Children.AsEnumerable() ?? Enumerable.Empty<Control>())
            .Where(child => child.IsVisible && child is not ToolbarSpacer).Sum(child => child.DesiredSize.Width + spacing);
        var stretchCount = children.Count(child => child.IsVisible && child is not ToolbarSpacer && child.HorizontalAlignment == HorizontalAlignment.Stretch);
        var extraWidth = Math.Max(0, leftSpace - overflowWidth);
        var share = spacerCount + stretchCount == 0 ? 0 : extraWidth / (spacerCount + stretchCount);
        double spacerWidth = share;

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            child.ApplyTemplate();

            rcChild = rcChild.WithX(rcChild.X + previousChildSize);
            rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
            if (child is ToolbarSpacer)
            {
                previousChildSize = spacerWidth;
                rcChild = rcChild.WithWidth(previousChildSize);
            }
            else if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                previousChildSize = child.DesiredSize.Width + share;
                rcChild = rcChild.WithWidth(previousChildSize);
                previousChildSize += spacing;
            }
            else
            {
                previousChildSize = child.DesiredSize.Width;
                rcChild = rcChild.WithWidth(previousChildSize);
                previousChildSize += spacing;
            }
            //if (rcChild.Right > finalSize.Width)
            //    rcChild = rcChild.WithWidth(Math.Max(0, finalSize.Width - rcChild.X));

            if (rcChild.Right > finalSize.Width)
            {
                if (OutOfBoundsPanel is { } panel)
                {
                    for (var j = i; j < count; ++j)
                    {
                        var c = children[children.Count - 1];
                        Children.RemoveAt(Children.Count - 1);
                        if (panel.IsAttachedToVisualTree())
                        {
                            panel.Children.Insert(0, c);
                            ((ISetInheritanceParent)c).SetParent(this);
                        }
                        else
                            overflowControls.Add(c);
                    }
                    SetCurrentValue(IsOverflowProperty, true);
                    return finalSize;
                }
            }
            ArrangeChild(child, rcChild, finalSize);
        }

        if (OutOfBoundsPanel != null)
        {
            if (leftSpace > 0)
            {
                Control? child = null;
                if (overflowControls.Count > 0)
                {
                    child = overflowControls[overflowControls.Count - 1];
                }
                else if (OutOfBoundsPanel.Children.Count > 0)
                {
                    child = OutOfBoundsPanel.Children[0];
                }

                if (child != null)
                {
                    if (leftSpace >= child.DesiredSize.Width + spacing)
                    {
                        if (overflowControls.Count > 0)
                            overflowControls.RemoveAt(overflowControls.Count - 1);
                        else
                            OutOfBoundsPanel.Children.RemoveAt(0);
                        Children.Add(child);
                        child.ApplyStyling();
                    }
                }
            }
            SetCurrentValue(IsOverflowProperty, OutOfBoundsPanel.Children.Count > 0 || overflowControls.Count > 0);
        }

        return finalSize;
    }

    private Size ArrangeWithWrapOverride(Size finalSize)
    {
        var children = Children;
        double x = 0;
        double y = 0;
        double maxWidth = finalSize.Width;
        double maxDesiredHeight = 0;
        double maxEffectiveWidth = maxWidth;
        int rows = 1;
        bool anyInRow = false;

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
        }

        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
                continue;

            var desiredSize = child.DesiredSize;
            if (anyInRow && x + desiredSize.Width > maxWidth)
            {
                maxEffectiveWidth = Math.Max(maxEffectiveWidth, x);
                anyInRow = false;
                x = 0;
                y += maxDesiredHeight;
                rows++;
            }
            else
                anyInRow = true;

            var finalChildWidth = Math.Max(1, Math.Min(desiredSize.Width, maxWidth - x));
            child.Arrange(new Rect(x, y, finalChildWidth, maxDesiredHeight));
            x += finalChildWidth;
        }

        return new Size(rows == 1 ? x : maxEffectiveWidth, rows * maxDesiredHeight);
    }

    internal virtual void ArrangeChild(
        Control child,
        Rect rect,
        Size panelSize)
    {
        child.Arrange(rect);
    }
}

public class ToolbarSpacer : Control
{
}
