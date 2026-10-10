using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Classic.Avalonia.Tests;

public class TabLayoutTests
{
    [AvaloniaFact]
    public void Demo_left_tabs_do_not_reserve_the_outer_navigation_width()
    {
        var view = new Classic.Demo.Views.MainView();
        var window = new Window { Width = 960, Height = 720, Content = view };
        window.Show();
        try
        {
            var categories = view.FindControl<TabControl>("DemoCategories")!;
            categories.SelectedItem = categories.Items.OfType<TabItem>().Single(t => t.Header?.ToString() == "Navigation");
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var group = view.GetVisualDescendants().OfType<global::Classic.Avalonia.Theme.Controls.GroupBox>()
                .Single(g => g.Header?.ToString() == "Left");
            var tabs = Assert.IsType<TabControl>(group.Content);
            foreach (var index in new[] { 0, 2 })
            {
                tabs.SelectedIndex = index;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                var content = tabs.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "ContentPanel");
                var edge = tabs.Items.Cast<TabItem>().Max(item => item.TranslatePoint(default, tabs)!.Value.X + item.Bounds.Width);
                Assert.InRange(content.Bounds.Left - edge, -3, 3);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Dock.Left)]
    [InlineData(Dock.Right)]
    public void Nested_side_tabs_size_their_header_column_independently(Dock placement)
    {
        var inner = new TabControl { TabStripPlacement = placement, Items =
        {
            new TabItem { Header = "A", Content = new TextBlock { Text = "First page" } },
            new TabItem { Header = "B", Content = new TextBlock { Text = "Second page" } },
            new TabItem { Header = "C", Content = new TextBlock { Text = "Third page" } }
        } };
        var outer = new TabControl { TabStripPlacement = placement,
            Items = { new TabItem { Header = "A much wider navigation category", Content = inner } } };
        var window = new Window { Width = 800, Height = 500, Content = outer };
        window.Show();
        try
        {
            foreach (var index in new[] { 0, 2 })
            {
                inner.SelectedIndex = index;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                var strip = inner.GetVisualDescendants().OfType<ItemsPresenter>().Single(p => p.Name == "PART_ItemsPresenter");
                var content = inner.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "ContentPanel");
                var innerGrid = inner.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "RootGrid");
                var outerGrid = outer.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "RootGrid");
                Assert.NotSame(outerGrid.ColumnDefinitions[0], innerGrid.ColumnDefinitions[0]);
                Assert.NotSame(outerGrid.RowDefinitions[0], innerGrid.RowDefinitions[0]);
                var items = inner.Items.Cast<TabItem>().ToArray();
                Assert.True(strip.Bounds.Width < 80, $"Nested header column grew to {strip.Bounds.Width}");
                if (placement == Dock.Left)
                {
                    var edge = items.Max(item => item.TranslatePoint(default, inner)!.Value.X + item.Bounds.Width);
                    Assert.InRange(content.Bounds.Left - edge, -3, 3);
                }
                else
                {
                    var edge = items.Min(item => item.TranslatePoint(default, inner)!.Value.X);
                    Assert.InRange(edge - content.Bounds.Right, -3, 3);
                }
                window.Width = index == 0 ? 900 : 700;
            }
        }
        finally { window.Close(); }
    }
}
