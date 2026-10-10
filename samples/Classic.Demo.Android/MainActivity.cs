using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Classic.Demo.Android;

[Activity(Label = "Classic Demo", Theme = "@style/ClassicDemo", MainLauncher = true,
    Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity { }
