using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Browser;

[assembly: SupportedOSPlatform("browser")]
namespace Classic.Demo.Browser;
public static class Program
{
    public static Task Main(string[] args) => App.BuildAvaloniaApp().StartBrowserAppAsync("out");
}
