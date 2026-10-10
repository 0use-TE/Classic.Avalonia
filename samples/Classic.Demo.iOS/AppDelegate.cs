using Avalonia;
using Avalonia.iOS;
using Classic.Avalonia.Theme;
using Foundation;

namespace Classic.Demo.iOS;

[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).WithClassicFont().LogToTrace();
}
