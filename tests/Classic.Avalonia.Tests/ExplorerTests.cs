using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaExplorer;
using Classic.CommonControls;
using Xunit;

namespace Classic.Avalonia.Tests;

public class ExplorerTests
{
    [Theory]
    [InlineData("example.com/path", "https://example.com/path")]
    [InlineData("  https://example.com/path  ", "https://example.com/path")]
    [InlineData("HTTPS://example.com/path", "https://example.com/path")]
    [InlineData("http://localhost:8080/", "http://localhost:8080/")]
    [InlineData("file:///C:/example.txt", "file:///C:/example.txt")]
    public void Address_input_normalizes_supported_urls(string input, string expected)
    {
        var model = new MainWindowViewModel();
        Uri? request = null;
        model.LoadUriRequest += uri => request = uri;
        model.LoadLink(input);
        Assert.Equal(new Uri(expected), request);
        Assert.Equal(new Uri(expected).AbsoluteUri, model.Url);
        Assert.Null(model.NavigationError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("https://")]
    [InlineData("http://[broken")]
    [InlineData("ftp://example.com")]
    public void Invalid_address_does_not_crash_or_trigger_navigation(string input)
    {
        var model = new MainWindowViewModel();
        var requests = 0;
        model.LoadUriRequest += _ => requests++;
        var original = model.Url;
        model.LoadLink(input);
        Assert.Equal(0, requests);
        Assert.Equal(original, model.Url);
        Assert.NotNull(model.NavigationError);
        model.LoadLink("example.com");
        Assert.Null(model.NavigationError);
        Assert.Equal(1, requests);
    }

    [Fact]
    public void Unchecking_previous_radio_option_does_not_override_new_selection()
    {
        var model = new MainWindowViewModel { IsLargeIcons = true, IsTextRight = true };
        model.IsSmallIcons = false;
        model.IsTextDown = false;
        Assert.Equal(ToolbarSize.Large, model.ToolbarSize);
        Assert.Equal(ToolbarTextPlacement.Right, model.ToolbarTextPlacement);
    }

    [AvaloniaFact]
    public void Options_tabs_render_and_ok_closes_window_with_pointer_input()
    {
        var window = new OptionsDialog();
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        try
        {
            window.UpdateLayout();
            var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
            foreach (var tab in tabs.Items.OfType<TabItem>())
            {
                tabs.SelectedItem = tab;
                global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.True(Assert.IsAssignableFrom<Control>(tab.Content).Bounds.Width > 0);
            }
            var ok = window.GetVisualDescendants().OfType<Button>().Single(button => button.Content?.ToString() == "OK");
            var point = ok.TranslatePoint(new Point(ok.Bounds.Width / 2, ok.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.True(closed, $"OK click at {point}, button={ok.Bounds}, window={window.Bounds}, hit={window.InputHitTest(point)}");
        }
        finally { window.Close(); }
    }
}
