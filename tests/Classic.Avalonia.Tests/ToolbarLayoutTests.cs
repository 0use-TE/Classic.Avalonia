using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Classic.CommonControls;
using Xunit;

namespace Classic.Avalonia.Tests;

public class ToolbarLayoutTests
{
    [AvaloniaFact]
    public void Fully_overflowed_toolbar_keeps_its_height_and_restores_items()
    {
        var toolbar = new ToolBar { VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top };
        toolbar.Children.Add(new Button { Width = 120, Height = 28, Content = "Wide command" });
        var window = new Window { Width = 60, Height = 200, Content = toolbar };
        var original = Logger.Sink;
        var log = new LayoutLog();
        Logger.Sink = log;
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.True(toolbar.IsOverflow);
            Assert.True(toolbar.Bounds.Height >= 28);
            window.Width = 300;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.False(toolbar.IsOverflow);
            Assert.True(toolbar.Children[0].Bounds.Width >= 120);
            Assert.Empty(log.Cycles);
        }
        finally { window.Close(); Logger.Sink = original; }
    }

    private sealed class LayoutLog : ILogSink
    {
        public List<string> Cycles { get; } = new();
        public bool IsEnabled(LogEventLevel level, string area) => area == "Layout";
        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Log(level, area, source, messageTemplate, []);
        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (messageTemplate.Contains("Layout cycle")) Cycles.Add(messageTemplate + " " + string.Join(", ", propertyValues));
        }
    }

    [AvaloniaFact]
    public void Demo_toolbars_settle_when_resized_with_overflow_open_and_closed()
    {
        var original = Logger.Sink;
        var log = new LayoutLog();
        Logger.Sink = log;
        var window = new Classic.Demo.Views.MainWindow();
        try
        {
            window.Show();
            foreach (var width in new[] { 960, 500, 300, 650, 960, 400, 800, 960 })
            {
                window.Width = width;
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                var toolbar = window.GetVisualDescendants().OfType<ToolBar>().First();
                if (toolbar.IsOverflow)
                {
                    var button = toolbar.GetVisualDescendants().OfType<Button>().Single(button => button.Flyout != null);
                    button.Flyout!.ShowAt(button);
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    button.Flyout.Hide();
                    Dispatcher.UIThread.RunJobs();
                }
            }
            var firstToolbar = window.GetVisualDescendants().OfType<ToolBar>().First();
            Assert.False(firstToolbar.IsOverflow);
            var mainPanel = firstToolbar.GetVisualDescendants().OfType<ToolbarPanel>().Single(panel => panel.Name == "PART_Panel");
            Assert.Equal(firstToolbar.Children, mainPanel.Children);
            foreach (var button in firstToolbar.Children.OfType<ToolBarButton>())
            {
                Assert.Equal(ToolbarSize.Small, button.Size);
                Assert.NotNull(button.Template);
            }
            Assert.Empty(log.Cycles);
        }
        finally { window.Close(); Logger.Sink = original; }
    }
}
