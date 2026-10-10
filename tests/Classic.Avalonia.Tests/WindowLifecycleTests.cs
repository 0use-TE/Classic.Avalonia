using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Media;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Classic.CommonControls.Dialogs;
using Xunit;

namespace Classic.Avalonia.Tests;

public class WindowLifecycleTests
{
    private sealed class BindingLog : ILogSink
    {
        public List<string> Errors { get; } = new();
        public bool IsEnabled(LogEventLevel level, string area) => area == "Binding" && level >= LogEventLevel.Warning;
        public void Log(LogEventLevel level, string area, object? source, string template) => Errors.Add(template);
        public void Log(LogEventLevel level, string area, object? source, string template, params object?[] values) => Errors.Add(template + " " + string.Join(", ", values));
    }

    [AvaloniaFact]
    public void Title_bar_updates_title_without_null_host_errors_on_close()
    {
        var original = Logger.Sink;
        var log = new BindingLog();
        Logger.Sink = log;
        try
        {
            for (var index = 0; index < 3; index++)
            {
                var window = new ClassicWindow { Title = "Original", Width = 400, Height = 200 };
                window.UseClientSideDecorations = true;
                window.Show();
                window.Title = "Changed";
                window.UpdateLayout();
                Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), block => block.Text == "Changed");
                var titleBar = window.GetVisualDescendants().Single(v => v.GetType().Name == "AutoAttachTitleBar");
                Assert.Equal(TextRenderingMode.Antialias, TextOptions.GetTextRenderingMode(titleBar));
                Assert.Equal(TextHintingMode.Strong, TextOptions.GetTextHintingMode(titleBar));
                Assert.Equal(BaselinePixelAlignment.Aligned, TextOptions.GetBaselinePixelAlignment(titleBar));
                window.Close();
            }
            Assert.Empty(log.Errors);
        }
        finally { Logger.Sink = original; }
    }

    [Fact]
    public void About_runtime_information_requires_no_native_memory_query()
    {
        var failures = new List<Exception>();
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is System.ComponentModel.Win32Exception) failures.Add(args.Exception);
        };
        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            var model = new AboutDialogViewModel(new AboutDialogOptions { Title = "Test" });
            Assert.True(model.TotalMemory > 0);
            Assert.InRange(model.FreeResources, 0, 100);
            Assert.Empty(failures);
            Assert.Contains("runtime", model.MemoryDescription);
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= handler; }
    }

    [Fact]
    public void About_accepts_application_information_and_handles_unknown_memory()
    {
        var model = new AboutDialogViewModel(new AboutDialogOptions
        {
            SystemInformation = new AboutSystemInformation
            {
                OperatingSystem = "Custom platform", Username = "User", TotalMemoryBytes = 2048L * 1024 * 1024,
                AvailableMemoryBytes = 1024L * 1024 * 1024, MemoryDescription = "Physical memory:"
            }
        });
        Assert.Equal("Custom platform", model.OperatingSystem);
        Assert.Equal(2048, model.TotalMemory);
        Assert.Equal(50, model.FreeResources);
        var unknown = new AboutDialogViewModel(new AboutDialogOptions { SystemInformation = new AboutSystemInformation() });
        Assert.Equal(0, unknown.TotalMemory);
        Assert.Equal(0, unknown.FreeResources);
    }
}
