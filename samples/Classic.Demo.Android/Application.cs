using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Classic.Avalonia.Theme;

namespace Classic.Demo.Android;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    public Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).WithClassicFont().LogToTrace();
}
