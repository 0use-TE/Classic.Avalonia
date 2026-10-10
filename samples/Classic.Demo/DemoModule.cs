using System;
using Classic.Demo.ViewModels;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Classic.Demo;

public sealed class DemoModule : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<DataDisplayViewModel>();
    }

    public void InitializeModule(IServiceProvider services)
    {
    }
}
