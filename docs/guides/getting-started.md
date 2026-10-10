# 项目引用与接入

当前先通过项目引用接入，不要求安装 Classic 的 NuGet 包。所需第三方依赖由各项目的 PackageReference 自动还原。

## 环境

- .NET 10 SDK。global.json 以 10.0.100 为基准并允许 latestFeature 前滚；本地验证使用 10.0.401。
- Avalonia 12.0.0。引用应用应与仓库的 Avalonia 版本兼容。
- 普通桌面或共享应用使用 net10.0；Android、iOS、WASM 主机另需对应工作负载。

## 添加引用

假设解决方案目录如下：

```text
Workspace/
  Classic.Avalonia/
    src/
  MyApp/
    MyApp.csproj
```

在 MyApp.csproj 中添加：

```xml
<ItemGroup>
  <ProjectReference Include="../Classic.Avalonia/src/Classic.Avalonia.Theme/Classic.Avalonia.Theme.csproj" />
</ItemGroup>
```

主题项目已经传递引用 Classic.CommonControls.Avalonia。使用颜色选择器时再添加：

```xml
<ItemGroup>
  <ProjectReference Include="../Classic.Avalonia/src/Classic.Avalonia.Theme.ColorPicker/Classic.Avalonia.Theme.ColorPicker.csproj" />
</ItemGroup>
```

路径以你的应用项目目录为基准调整。多主机应用通常将引用放在共享 UI 项目中，让各主机引用共享项目。

## 加载主题

在 App.axaml 中加载 ClassicTheme。作为应用主主题时，替换原来的 FluentTheme 或 SimpleTheme：

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:classic="using:Classic.Avalonia.Theme"
             x:Class="MyApp.App"
             RequestedThemeVariant="{x:Static classic:ClassicTheme.Standard}">
  <Application.Styles>
    <classic:ClassicTheme />
    <!-- 仅在引用颜色选择器项目时保留下一行。 -->
    <StyleInclude Source="avares://Classic.Avalonia.Theme.ColorPicker/Classic.axaml" />
  </Application.Styles>
</Application>
```

MyApp.App 应替换为实际应用类。ClassicTheme 已包含公共控件的经典样式，无需重复加载公共控件的 Fluent 样式。

## 注册字体

在每个平台主机的 AppBuilder 中调用 WithClassicFont：

```csharp
using Avalonia;
using Classic.Avalonia.Theme;

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithClassicFont()
        .LogToTrace();
```

以上是桌面配置示例。Browser、Android、iOS 保留各自的主机初始化方式，只在其 AppBuilder 链中增加 WithClassicFont。仓库示例已经完成注册。

## 使用经典窗口

桌面窗口可同时在 XAML 和后台类中改用 ClassicWindow：

```xml
<classic:ClassicWindow xmlns="https://github.com/avaloniaui"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                      xmlns:classic="using:Classic.Avalonia.Theme"
                      x:Class="MyApp.MainWindow"
                      Width="800" Height="600" Title="MyApp">
  <TextBlock Margin="16" Text="Hello, Classic!" />
</classic:ClassicWindow>
```

```csharp
using Classic.Avalonia.Theme;

namespace MyApp;

public partial class MainWindow : ClassicWindow
{
    public MainWindow() => InitializeComponent();
}
```

Browser 和移动端使用共享视图，不套用桌面窗口生命周期。Crystal 不是使用主题的必要依赖，只有示例采用该架构。
