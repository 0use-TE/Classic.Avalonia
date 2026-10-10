# NativeAOT 支持

Classic.Avalonia.Theme、Classic.CommonControls.Avalonia、Classic.Avalonia.Theme.ColorPicker 支持 NativeAOT，三个项目均声明 IsAotCompatible=true。检查对象是库本身，不以某个示例应用启动成功作为唯一依据。

## 库级检查结果

2026-10-10 在 Windows x64 / .NET SDK 10.0.401 上完成以下检查：

| 检查 | 结果 |
| --- | --- |
| 三个库的 AOT 与裁剪分析器 | 无 IL/AOT 兼容性告警 |
| 将三个库的全部成员纳入 NativeAOT 编译分析 | 成功；未添加告警抑制 |
| 正常裁剪的 NativeAOT 发布 | 成功；不要求应用保留整个库程序集 |
| 运行库专用 NativeAOT 验证程序 | 293 项断言通过；IsDynamicCodeSupported=false |
| 同一程序在正常裁剪的 NativeAOT 产物中运行 | 293 项断言通过 |

运行检查涵盖 Tahoma 两个字重与可打印英文字符、26 种控件的模板和布局、嵌套左右标签的独立行列定义与对齐、全部 14 个配色、日期确认、时间确认/取消及重新打开、颜色编辑双向绑定，以及 MessageBox、AboutDialog、FontDialog、InputBox 的绑定、命令和返回结果。检查记录绑定告警并将其视为失败。

全成员分析模式使用 TrimmerRootAssembly 是为了让编译器检查库的完整成员集合，不是普通应用必须使用的配置。另一次正常裁剪发布不添加这些保留项，用于发现被保留项掩盖的裁剪问题。

本地实际 NativeAOT 执行环境为 Windows x64 的 Headless/Skia 测试主机；其他平台仍需对应 NativeAOT 工具链与 Avalonia 后端。本结果确认库级兼容性，不表示任意应用的运行时 XAML、反射绑定、第三方插件和原生部署都自动兼容。

## AOT 接入要求

- 应用与控件模板的 XAML 应在构建时编译。
- 在 App.axaml 中引用样式，避免在代码中使用动态 StyleInclude/ResourceInclude 加载；后者会产生 IL2026。
- 应用 ViewModel 优先使用带 x:DataType 的编译绑定，主题不能替应用保留任意反射成员。
- 每个平台主机注册 WithClassicFont，并提供其所需的 Avalonia 原生图形依赖。

不需要给三个库补充大范围的 linker 保留文件或关闭裁剪/AOT 分析器。

## 复现库级验证

在仓库根目录运行：

```powershell
dotnet publish tests/Classic.Avalonia.AotTests/Classic.Avalonia.AotTests.csproj -c Release -r win-x64 -p:AnalyzeAllLibraryMembers=true -p:GeneratePackageOnBuild=false -o artifacts/aot/library-analysis
./artifacts/aot/library-analysis/Classic.Avalonia.AotTests.exe
dotnet publish tests/Classic.Avalonia.AotTests/Classic.Avalonia.AotTests.csproj -c Release -r win-x64 -p:GeneratePackageOnBuild=false -o artifacts/aot/library-trimmed
./artifacts/aot/library-trimmed/Classic.Avalonia.AotTests.exe
```

测试项目将裁剪和 AOT 编译告警设为错误。Build 工作流已配置相同的两种检查；本地结果不代表远端 CI 已执行。

## 单文件与静态链接的区别

库支持 NativeAOT 不等于原生图形 DLL 已合入单个 EXE。StaticLink 属于应用部署选项，不是主题库支持 AOT 的必要依赖。

以下保留 Notepad 单文件部署示例，独立于上面的库级结论。

当前接入与本地测试范围为 Notepad / Windows x64。主题库仍通过项目引用接入；静态原生图形依赖使用 [StaticLink.Avalonia](https://github.com/0use-TE/StaticLink.Avalonia)。

## 发布配置

Notepad.csproj 仅在 PublishAot=true 时引用 StaticLink.Avalonia 3.119.4-7922.1。该包将 AOT 发布中的 SkiaSharp 升至 3.119.4，并静态链接 Skia、HarfBuzz 与 ANGLE。普通非 AOT 构建保持原有依赖方式。

只设置 PublishAot 和 PublishSingleFile，并不能自动把 Avalonia 的原生图形 DLL 静态链接进 EXE。仓库提供无凭据的 WinX64Aot 发布配置：

```powershell
dotnet publish samples/Notepad/Notepad.csproj -p:PublishProfile=WinX64Aot -o artifacts/aot/notepad/single-file
```

需要 Windows NativeAOT 编译工具链，包含 Visual Studio 的 C++ 桌面开发工具与 Windows SDK。发布配置关闭调试符号复制，并从发布列表排除原生依赖附带的 PDB。符号仍可在构建目录保留；发布产物不需要它们才能运行。

请使用新的输出目录。对旧发布目录重新发布不会自动清理过去留下的 DLL 或 PDB，不能仅凭旧目录中的文件判断此次链接结果。

## 可重复的启动检查

```powershell
pwsh -File scripts/Test-NotepadAot.ps1 -Executable artifacts/aot/notepad/single-file/Notepad.exe
```

脚本将 EXE 复制到唯一的隔离目录，不附带 DLL、PDB 或 .NET 配置文件，再启动程序并检查：

- PE 格式为 Windows x64，CLR 数据目录为空。
- 程序创建桌面窗口，进程响应正常。
- 运行进程未加载外部 Skia、HarfBuzz、ANGLE DLL，也未加载 coreclr、hostfxr、hostpolicy。
- 记录文件大小、SHA256 和进程模块列表，测试结束关闭自己启动的进程。

结果保存到 artifacts/verification 下对应隔离目录的 smoke-report.json。此脚本是启动检查，不会编辑用户文档或测试打印。

## 示例验证范围

2026-10-10 的 Windows 本地验证成功完成静态链接 AOT 发布、EXE 隔离启动、原生 PE 与运行模块检查。辅助功能树能读取到菜单、状态栏和编辑区。

桌面自动输入工具返回 GetCursorPos 访问拒绝（0x80070005），因此未完成文本编辑和对话框点击验证，不能据此声称全部 NativeAOT 功能已验证。此前的 58 项 Headless 回归测试也不替代发布版 NativeAOT 交互验证。

单个 EXE 仍依赖 Windows 提供的系统库，并不意味着脱离操作系统运行。本次没有验证 Linux/macOS AOT；macOS 还需与 Avalonia 版本匹配的 StaticLink.Avalonia.Native 包。

Tmds.DBus.Protocol 0.90.3 的 NU1903 告警与静态链接问题独立，当前仍未解决。
