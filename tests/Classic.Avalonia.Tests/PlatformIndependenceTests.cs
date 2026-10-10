using System.Reflection;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using Xunit;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;

namespace Classic.Avalonia.Tests;

public class PlatformIndependenceTests
{
    [AvaloniaFact]
    public void Sound_assets_belong_to_samples_and_not_to_published_controls()
    {
        _ = typeof(Classic.Demo.Program).Assembly;
        foreach (var name in new[] { "chord", "ding", "chimes" })
        {
            Assert.False(AssetLoader.Exists(new Uri("avares://Classic.CommonControls.Avalonia/Audio/" + name + ".wav")));
            using var audio = AssetLoader.Open(new Uri("avares://Classic.Demo.Desktop/Audio/" + name + ".wav"));
            var header = new byte[4];
            Assert.Equal(4, audio.Read(header));
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(header));
        }
    }

    [Fact]
    public void Published_libraries_contain_no_native_imports_or_desktop_backend_references()
    {
        var assemblies = new[] { typeof(ClassicWindow).Assembly, typeof(ToolBar).Assembly, typeof(global::Classic.Avalonia.Theme.ColorPicker.ClassicColorPalette).Assembly };
        foreach (var assembly in assemblies)
        {
            var nativeMethods = assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                .Where(method => (method.Attributes & MethodAttributes.PinvokeImpl) != 0);
            Assert.Empty(nativeMethods);
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name is "Avalonia.Win32" or "Avalonia.X11" or "Avalonia.Native" or "Avalonia.Desktop");
        }
    }
}
