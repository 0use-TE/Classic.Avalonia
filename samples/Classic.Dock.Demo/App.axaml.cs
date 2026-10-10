using Avalonia;
using Avalonia.Markup.Xaml;
using Classic.Avalonia.Theme;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.DockDemo;

public partial class App : CrystalApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().WithClassicFont().LogToTrace();
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void RegisterServices(IServiceCollection services) => services.AddMvvmTransient<MainView, MainViewModel>();
    public override void RegisterModules(IModuleRegistrar modules) => modules.RegisterModule<DockDemoModule>();
    public override void CreateShell(IServiceProvider services) => CreateShell<MainWindow, MainView>();
}

public sealed class DockDemoModule : IModule
{
    public void RegisterServices(IServiceCollection services) { }
    public void InitializeModule(IServiceProvider services) { }
}
