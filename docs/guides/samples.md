# Crystal 示例架构

Classic.Demo 使用 Crystal.Avalonia 3.1.1 和 CommunityToolkit.Mvvm 8.4.0。主题库本身不依赖 Crystal。

## 目录与职责

| 位置 | 职责 |
| --- | --- |
| samples/Classic.Demo/App.axaml.cs | CrystalApplication、服务注册、模块注册和平台 Shell 创建 |
| samples/Classic.Demo/DemoModule.cs | 注册 DataDisplayViewModel 等模块依赖 |
| samples/Classic.Demo/Views | 共享 MainView、桌面 MainWindow，以及视图专属布局和对话框处理 |
| samples/Classic.Demo/ViewModels | 可观察状态、命令、示例数据与分类选择 |
| samples/Classic.Demo.Desktop | 桌面主机 |
| samples/Classic.Demo.Browser | WASM 主机 |
| samples/Classic.Demo.Android | Android 主机 |
| samples/Classic.Demo.iOS | iOS 主机 |

App 通过 AddMvvmTransient 注册 MainView/MainViewModel，通过 DemoModule 注册数据模型，再使用 CreateShell<MainWindow, MainView>() 创建平台对应入口。

MainView 使用 ViewModelLocator.AutoWireViewModel 自动绑定；每个主视图拥有独立模型和可编辑数据。分类索引在模型、桌面标签页和紧凑导航之间双向同步。

## EventToCommand

Buttons 分类的点击计数和 Spinner 使用 Crystal EventToCommand，并显式指定路由事件：

```xml
<Button Content="Click"
        xmlns:crystal="using:Crystal.Avalonia"
        crystal:EventToCommand.Command="{Binding CountClickCommand}"
        crystal:EventToCommand.RoutedEvent="{x:Static Button.ClickEvent}" />
```

Spinner 示例启用 PassEventArgs，将 SpinEventArgs 传入模型命令。依赖窗口 owner 的对话框和控件布局仍由视图负责。

## 运行示例

在仓库根目录运行桌面示例：

```powershell
dotnet run --project samples/Classic.Demo.Desktop
```

运行 Browser 主机后打开输出的 HTTP 地址：

```powershell
dotnet run --project samples/Classic.Demo.Browser
```

主机地址可能变化。重新构建 Browser 后应重启主机，以更新静态资源指纹。

## 其他示例

Notepad 展示文件编码、未保存变更确认、搜索替换和生成打印 HTML。AvaloniaExplorer 展示桌面浏览器外壳、地址验证、工具栏和选项窗口。部分演示菜单或历史设置仅展示界面，不代表完整产品功能。
