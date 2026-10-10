using Avalonia;
using Avalonia.Markup.Xaml;
using Classic.Avalonia.Theme;
using Classic.Demo.ViewModels;
using Classic.Demo.Views;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Classic.Demo;

public partial class App : CrystalApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().WithClassicFont().LogToTrace();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddMvvmTransient<MainView, MainViewModel>();
    }

    public override void RegisterModules(IModuleRegistrar modules) => modules.RegisterModule<DemoModule>();

    public override void CreateShell(IServiceProvider services) => CreateShell<MainWindow, MainView>();
}
