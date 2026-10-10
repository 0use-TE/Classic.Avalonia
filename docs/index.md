# Classic.Avalonia

Classic.Avalonia 为 Avalonia 应用提供 Windows 9x 风格的控件、窗口装饰和经典配色方案。当前文档对应本仓库的 .NET 10 / Avalonia 12.0.0 实现，接入方式暂时采用项目引用。

三个库支持 NativeAOT，已完成全成员编译分析与正常裁剪产物的运行检查。配置要求、复现方式和平台验证范围见 [NativeAOT 支持](guides/native-aot.md)。

[在线预览](../artifacts/pages/browser/wwwroot/index.html)提供同一套 Crystal 示例的 WebAssembly 版本。GitHub Pages 将文档和预览部署在同一站点，预览位于 demo/。

从[项目引用与接入](guides/getting-started.md)开始，然后按需配置[配色与字体](guides/appearance.md)或使用[扩展控件](guides/controls.md)。[API 文档](api/toc.yml)由三个现有库项目的公开源码自动生成。

## 项目组成

| 项目 | 用途 |
| --- | --- |
| Classic.CommonControls.Avalonia | ToolBar、ListView、对话框等扩展控件，可独立于经典主题使用 |
| Classic.Avalonia.Theme | 经典控件样式、窗口装饰、配色、内置字体 |
| Classic.Avalonia.Theme.ColorPicker | Avalonia 颜色选择控件的经典样式 |
| Classic.Demo | 使用 Crystal.Avalonia 的共享控件示例 |
| Notepad / AvaloniaExplorer | 桌面功能示例 |

## 当前状态

目前使用经典配色方案切换，没有完整、独立的明暗两套经典控件样式。内置 Tahoma 常规和粗体，默认正文与标题字号为 11 DIPs。

2026-10-10 在 Windows / .NET SDK 10.0.401 上通过 63 项 Release 回归测试；WASM、Android 编译通过，iOS 通过 Windows 上的托管代码编译。这些结果不代表所有原生平台、辅助功能和发布场景均已验证。[平台说明](guides/platforms.md)列出验证范围和剩余限制。
