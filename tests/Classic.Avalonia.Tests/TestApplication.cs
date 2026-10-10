using Classic.Avalonia.Theme;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Classic.Avalonia.Tests.TestApplication))]

namespace Classic.Avalonia.Tests;

public class TestApplication : Classic.Demo.App
{
    public new static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .WithClassicFont()
        .UseSkia()
        .UseHarfBuzz()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
