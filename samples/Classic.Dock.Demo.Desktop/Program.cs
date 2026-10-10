using Avalonia;
using Classic.Avalonia.Theme;

namespace Classic.DockDemo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => App.BuildAvaloniaApp()
        .UsePlatformDetect()
        .With(new Win32PlatformOptions
        {
            CompositionMode = [Win32CompositionMode.DirectComposition, Win32CompositionMode.RedirectionSurface]
        })
        .StartWithClassicDesktopLifetime(args);
}
