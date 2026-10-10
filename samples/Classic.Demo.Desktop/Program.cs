using Classic.Samples;
using Avalonia;
using System;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using Avalonia.Controls;
using Avalonia.Dialogs;

namespace Classic.Demo;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Avalonia 12.0.0's WinUI composition path can report idle wake-up
            // timeouts. Classic windows do not require WinUI acrylic effects.
            .With(new Win32PlatformOptions
            {
                CompositionMode = [Win32CompositionMode.DirectComposition, Win32CompositionMode.RedirectionSurface]
            })
            .WithClassicFont()
            .LogToTrace()
            .UseMessageBoxSounds()
            .UseManagedSystemDialogs<ClassicWindow>();
}
