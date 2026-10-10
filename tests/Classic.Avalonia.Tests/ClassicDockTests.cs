using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using Classic.DockDemo;
using GOZA.Dock;
using GOZA.Dock.Controls;
using GOZA.Dock.Themes.Classic;
using Xunit;

namespace Classic.Avalonia.Tests;

public class ClassicDockTests
{
    public enum DockHeaderStyle { Windows98, VSCodeClassic, VSCodeModern }
    [AvaloniaTheory]
    [InlineData(DockHeaderStyle.Windows98, DockPanePresentation.ClassicSeams)]
    [InlineData(DockHeaderStyle.VSCodeClassic, DockPanePresentation.ClassicSeams)]
    [InlineData(DockHeaderStyle.VSCodeModern, DockPanePresentation.ClassicSeams)]
    [InlineData(DockHeaderStyle.Windows98, DockPanePresentation.ModernCards)]
    [InlineData(DockHeaderStyle.VSCodeClassic, DockPanePresentation.ModernCards)]
    [InlineData(DockHeaderStyle.VSCodeModern, DockPanePresentation.ModernCards)]
    public void Header_and_pane_modes_preserve_content_and_show_selection_and_hover(DockHeaderStyle mode, DockPanePresentation panes)
    {
        var model = new MainViewModel();
        var view = new MainView { DataContext = model };
        var window = new Window { Width = 1040, Height = 720, Content = view };
        window.Show();
        try
        {
            Flush(window);
            var region = view.FindControl<DockRegion>("DocumentRegion")!;
            region.TabStripPlacement = DockTabStripPlacement.Bottom;
            var shell = view.FindControl<DockShell>("Workspace")!;
            var content = region.ActiveContent;
            ClassicDock.SetUseClassicTabs(shell, mode == DockHeaderStyle.Windows98);
            shell.TabPresentation = mode == DockHeaderStyle.VSCodeModern
                ? DockTabPresentation.ModernPills : DockTabPresentation.ClassicTabs;
            model.PaneLayout = panes;
            Flush(window);
            Assert.Same(content, region.ActiveContent);
            Assert.Equal(panes, shell.PanePresentation);
            Assert.Equal(mode == DockHeaderStyle.VSCodeModern ? DockTabPresentation.ModernPills : DockTabPresentation.ClassicTabs, shell.TabPresentation);
            var strip = region.GetVisualDescendants().OfType<TabStrip>().Single();
            var first = Assert.IsType<TabStripItem>(strip.ContainerFromIndex(0));
            var second = Assert.IsType<TabStripItem>(strip.ContainerFromIndex(1));
            Border Surface(TabStripItem item) => item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_DockTabSurface");
            Color Fill(TabStripItem item) => Assert.IsAssignableFrom<ISolidColorBrush>(Surface(item).Background).Color;
            Assert.Equal(mode == DockHeaderStyle.Windows98, first.GetVisualDescendants().OfType<ClassicBorderDecorator>().Any(b => b.Name == "PART_ClassicTabBorder"));
            var selected = Fill(first);
            var idle = Fill(second);
            Assert.NotEqual(selected, idle);
            var title = second.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_HeaderText");
            var point = title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
            var width = second.Bounds.Width;
            window.MouseMove(point);
            Flush(window);
            Assert.NotEqual(idle, Fill(second));
            Assert.Equal(width, second.Bounds.Width);
            window.MouseDown(point, MouseButton.Left);
            Flush(window);
            Assert.Same(model.Documents[1], region.SelectedItem);
            if (mode == DockHeaderStyle.Windows98)
                Assert.NotEqual(selected, Fill(second));
            window.MouseUp(point, MouseButton.Left);
            Flush(window);
            Assert.Equal(selected, Fill(second));
            window.MouseMove(new Point(0, 0));
            Flush(window);
            Assert.Equal(selected, Fill(second));
            var splitter = view.GetVisualDescendants().OfType<DockSplitter>().Single(s => Grid.GetRow(s) == 1);
            var seam = splitter.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Highlight");
            Assert.Equal(panes == DockPanePresentation.ClassicSeams ? 1 : 4, splitter.Bounds.Height + splitter.Margin.Top + splitter.Margin.Bottom, 5);
            if (panes == DockPanePresentation.ClassicSeams)
            {
                Assert.Equal(1, seam.Bounds.Height);
                Assert.NotEqual(Assert.IsAssignableFrom<ISolidColorBrush>(shell.Background).Color,
                    Assert.IsAssignableFrom<ISolidColorBrush>(seam.Background).Color);
            }
            if (Environment.GetEnvironmentVariable("CLASSIC_DOCK_STATES_PREVIEW") is { } directory)
            {
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(System.IO.Path.Combine(directory, $"classic-dock-{mode}-{panes}.png"));
            }
            ClassicDock.SetUseClassicTabs(shell, true);
            shell.TabPresentation = DockTabPresentation.ClassicTabs;
            Flush(window);
            Assert.Same(model.Documents[1], region.SelectedItem);
            Assert.Contains(region.GetVisualDescendants().OfType<ClassicBorderDecorator>(), b => b.Name == "PART_ClassicTabBorder");
        }
        finally
        {
            TabContainerDragController.CancelPointerInteraction();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Standalone_window_uses_Crystal_to_create_a_populated_workspace()
    {
        var window = new MainWindow();
        window.Show();
        try
        {
            Flush(window);
            var view = Assert.IsType<MainView>(window.Content);
            var model = Assert.IsType<MainViewModel>(view.DataContext);
            Assert.Equal(2, model.Documents.Count);
            Assert.NotNull(view.FindControl<DockRegion>("DocumentRegion")!.ActiveContent);
        }
        finally { window.Close(); }
    }
    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Drop_target_uses_an_independent_transparent_dashed_outline_and_clears_after_drag(bool drop)
    {
        var model = new MainViewModel();
        var view = new MainView { DataContext = model };
        var window = new Window { Width = 1040, Height = 720, Content = view };
        window.Show();
        try
        {
            Flush(window);
            var source = view.FindControl<DockRegion>("DocumentRegion")!;
            var target = view.FindControl<DockRegion>("ExplorerRegion")!;
            var hints = view.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "PART_DropHint").ToArray();
            Assert.Equal(3, hints.Length);
            var outlines = hints.Select(hint => Assert.IsType<global::Avalonia.Controls.Shapes.Rectangle>(hint.Child)).ToArray();
            Assert.Equal(3, outlines.Distinct().Count());
            foreach (var hint in hints)
            {
                Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(hint.Background).Color.A);
                Assert.Equal(new Thickness(0), hint.BorderThickness);
                Assert.False(hint.IsHitTestVisible);
            }
            foreach (var outline in outlines)
            {
                Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(outline.Stroke).Color);
                Assert.Equal(new double[] { 3, 3 }, outline.StrokeDashArray);
            }
            var title = source.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PART_HeaderText");
            var start = title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
            var end = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
            var content = target.ActiveContent;
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Flush(window);
            window.MouseMove(start + new Vector(18, 18));
            window.MouseMove(end);
            Flush(window);
            var activeHint = target.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_DropHint");
            Assert.True(activeHint.IsVisible);
            Assert.Same(content, target.ActiveContent);
            Assert.True(activeHint.Child!.Bounds.Width > 0 && activeHint.Child.Bounds.Height > 0);
            if (drop && Environment.GetEnvironmentVariable("CLASSIC_DOCK_DROP_PREVIEW") is { } path)
            {
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(path);
            }
            if (drop)
                window.MouseUp(end, MouseButton.Left);
            else
                TabContainerDragController.CancelPointerInteraction();
            Flush(window);
            Assert.All(hints, hint => Assert.False(hint.IsVisible));
            Assert.Equal(drop ? 1 : 2, model.Documents.Count);
            Assert.Equal(drop ? 2 : 1, model.Explorer.Count);
            var shell = view.FindControl<DockShell>("Workspace")!;
            ClassicDock.SetIsEnabled(shell, false);
            Flush(window);
            Assert.All(hints, hint => Assert.Null(hint.Child));
            ClassicDock.SetIsEnabled(shell, true);
            Flush(window);
            Assert.All(hints, hint => Assert.IsType<global::Avalonia.Controls.Shapes.Rectangle>(hint.Child));
        }
        finally
        {
            TabContainerDragController.CancelPointerInteraction();
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DockTabStripPlacement.Top)]
    [InlineData(DockTabStripPlacement.Bottom)]
    [InlineData(DockTabStripPlacement.Left)]
    [InlineData(DockTabStripPlacement.Right)]
    public void Pointer_drag_preview_copies_classic_background_and_close_button_has_a_gap(DockTabStripPlacement placement)
    {
        var model = new MainViewModel();
        var view = new MainView { DataContext = model };
        var window = new Window { Width = 1040, Height = 720, Content = view };
        window.Show();
        try
        {
            var region = view.FindControl<DockRegion>("DocumentRegion")!;
            region.TabStripPlacement = placement;
            Flush(window);
            var item = region.GetVisualDescendants().OfType<TabStrip>().Single().ContainerFromIndex(0)!;
            var title = item.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_HeaderText");
            var close = item.GetVisualDescendants().OfType<DockHeaderButton>().Single(b => b.Name == "PART_CloseButton");
            var layout = item.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_HeaderLayout");
            Assert.True(close.Bounds.Left - title.Bounds.Right >= 6,
                $"Title {title.Bounds}, close {close.Bounds}, close margin {close.Margin}, layout margin {layout.Margin}");
            Assert.True(layout.Bounds.Width >= title.Bounds.Width + close.Bounds.Width + 6);
            var surface = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_DockTabSurface");
            var start = title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Flush(window);
            var pressedColor = Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color;
            window.MouseMove(start + new Vector(18, 18));
            Flush(window);
            var overlay = window.GetVisualDescendants().OfType<OverlayLayer>().Single();
            var preview = Assert.Single(overlay.Children.OfType<Border>());
            Assert.Equal(pressedColor,
                Assert.IsAssignableFrom<ISolidColorBrush>(preview.Background).Color);
            Assert.Equal(new CornerRadius(0), preview.CornerRadius);
            Assert.True(preview.Bounds.Width > 0 && preview.Bounds.Height > 0);
            if (placement == DockTabStripPlacement.Top && Environment.GetEnvironmentVariable("CLASSIC_DOCK_DRAG_PREVIEW") is { } path)
            {
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(path);
            }
            window.MouseUp(start + new Vector(18, 18), MouseButton.Left);
            Flush(window);
            Assert.Equal(4, model.Documents.Count + model.Output.Count);
        }
        finally
        {
            TabContainerDragController.CancelPointerInteraction();
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DockTabStripPlacement.Top, ClassicBorderStyle.TabTop)]
    [InlineData(DockTabStripPlacement.Bottom, ClassicBorderStyle.TabBottom)]
    [InlineData(DockTabStripPlacement.Left, ClassicBorderStyle.TabLeft)]
    [InlineData(DockTabStripPlacement.Right, ClassicBorderStyle.TabRight)]
    public void Classic_templates_keep_content_and_work_for_every_tab_direction(
        DockTabStripPlacement placement, ClassicBorderStyle expected)
    {
        var model = new MainViewModel();
        var view = new MainView { DataContext = model };
        var window = new Window { Width = 1040, Height = 720, Content = view };
        window.Show();
        try
        {
            var region = view.FindControl<DockRegion>("DocumentRegion")!;
            region.TabStripPlacement = placement;
            Flush(window);
            Assert.NotNull(region.ActiveContent);
            var items = region.GetVisualDescendants().OfType<TabStripItem>().ToArray();
            Assert.Equal(2, items.Length);
            foreach (var item in items)
            {
                var border = item.GetVisualDescendants().OfType<ClassicBorderDecorator>()
                    .Single(b => b.Name == "PART_ClassicTabBorder");
                Assert.Equal(expected, border.BorderStyle);
                Assert.True(item.Bounds.Width > 0 && item.Bounds.Height > 0);
            }
            region.SelectedItem = model.Documents[1];
            Flush(window);
            Assert.Same(model.Documents[1], region.SelectedItem);
            Assert.Equal("Release notes\n\n- Classic tabs\n- Tahoma\n- Workspace tools",
                Assert.IsType<TextBox>(region.ActiveContent).Text);
            Assert.All(region.GetVisualDescendants().OfType<DockHeaderButton>(), button =>
                Assert.Contains(button.GetVisualDescendants().OfType<ClassicBorderDecorator>(),
                    b => b.Name == "PART_ClassicButtonBorder"));
            if (placement == DockTabStripPlacement.Top && Environment.GetEnvironmentVariable("CLASSIC_DOCK_PREVIEW") is { } preview)
            {
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(preview);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Every_palette_updates_dock_brushes_and_disabling_restores_default_styles()
    {
        var shell = new DockShell();
        var originalStyles = shell.Styles.Count;
        var originalResources = shell.Resources.MergedDictionaries.Count;
        var window = new Window { Width = 800, Height = 500, Content = shell };
        var app = Application.Current!;
        var previousVariant = app.RequestedThemeVariant;
        ClassicDock.SetIsEnabled(shell, true);
        ClassicDock.SetIsEnabled(shell, true);
        window.Show();
        try
        {
            Assert.Equal(originalStyles + 1, shell.Styles.Count);
            foreach (var variant in ClassicTheme.AllVariants)
            {
                app.RequestedThemeVariant = variant;
                Flush(window);
                Assert.True(shell.TryFindResource(SystemColors.ControlColorKey, out var expected));
                Assert.True(shell.TryFindResource("surface.background", out var brush));
                Assert.Equal(Assert.IsType<Color>(expected), Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);
            }
            shell.Resources["host.custom"] = "Keep";
            ClassicDock.SetIsEnabled(shell, false);
            Flush(window);
            Assert.Equal(originalStyles, shell.Styles.Count);
            Assert.Equal(originalResources, shell.Resources.MergedDictionaries.Count);
            Assert.Equal("Keep", shell.Resources["host.custom"]);
            ClassicDock.SetIsEnabled(shell, true);
            Assert.Equal(originalStyles + 1, shell.Styles.Count);
        }
        finally { window.Close(); app.RequestedThemeVariant = previousVariant; }
    }

    [AvaloniaFact]
    public void Close_and_maximize_controls_remain_functional()
    {
        var model = new MainViewModel();
        var view = new MainView { DataContext = model };
        var window = new Window { Width = 1040, Height = 720, Content = view };
        window.Show();
        try
        {
            Flush(window);
            var region = view.FindControl<DockRegion>("DocumentRegion")!;
            var shell = view.FindControl<DockShell>("Workspace")!;
            var close = region.GetVisualDescendants().OfType<DockHeaderButton>()
                .First(b => b.Name == "PART_CloseButton" && b.IsVisible);
            var point = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Flush(window);
            Assert.Single(model.Documents);
            Assert.True(shell.MaximizeRegion(region));
            Flush(window);
            Assert.Same(region, shell.MaximizedRegion);
            Assert.True(shell.RestoreMaximizedRegion());
            Flush(window);
            Assert.Null(shell.MaximizedRegion);
            model.NewDocumentCommand.Execute(null);
            Flush(window);
            Assert.Equal(2, model.Documents.Count);
        }
        finally { window.Close(); }
    }
}
