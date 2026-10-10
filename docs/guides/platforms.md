# 平台支持与已知限制

## 验证范围

以下记录对应 2026-10-10 的本地 Windows 环境和 .NET SDK 10.0.401。移动端与 WASM 按当前项目要求只做编译检查，不进行运行或 UI 验证。

| 范围 | 已完成 | 未覆盖 |
| --- | --- | --- |
| 三个控件/主题库 | Release 构建、打包、公开 API 文档生成 | 全部控件与所有状态的逐像素核对 |
| 库级 NativeAOT | 全成员编译分析、正常裁剪发布；两种实际 NativeAOT 产物均通过 287 项运行断言 | 本地只执行 Windows x64 Headless/Skia 主机，其他平台原生运行未验证 |
| 桌面共享交互 | 63 项 Headless 测试，涵盖绑定、指针输入、嵌套导航布局、菜单、字体、日期/时间和颜色选择 | 所有平台的原生窗口、辅助功能和实际设备行为 |
| WASM | Release 编译 | 当前版本浏览器运行验证 |
| Android | Release 编译 | 设备或模拟器运行 |
| iOS | Windows 上的模拟器目标托管代码编译 | macOS/Xcode 原生打包、签名、模拟器及设备运行 |
| Notepad / Explorer | 文件、编辑、地址校验和选项窗口等自动化测试 | 实际打印、原生 WebView 联网和所有文件提供程序 |

测试成功说明已覆盖的行为可用，不代表实现已经“完美”。完整 iOS 发布仍需 macOS、Xcode 与签名环境。

## 当前限制

- 经典主题没有完整独立的深色样式，建议显式选择内置经典配色。
- 内置字体不覆盖中文；尤其 Browser 需要配置合适的中文嵌入字体。
- Tahoma 的微软授权独立于源码 MIT 授权；保留原始授权说明。
- 还原桌面相关依赖时存在 NU1903：传递依赖 Tmds.DBus.Protocol 0.90.3 的 GHSA-xrw6-gwf8-vvr9 告警，尚未修复。
- 当前仓库没有 Classic.Avalonia.Theme.DataGrid 和 Classic.Avalonia.Theme.Dock 项目，不提供其项目引用或已验证接入步骤。
- 示例音效只在桌面示例中启用，不是跨平台系统音效实现。
- Demo 菜单和 Explorer 的部分历史选项仅用于样式展示。

## 常见问题

### 点击日期没有反应

确认正在使用新编译的共享主题，以及 Browser 主机已重启。自定义根控件模板需保留 Avalonia 弹层相关命名部件。当前主题的 EmbeddableControlRoot 包含 PART_VisualLayerManager。

### 文字缺字或显示方框

确认每个平台主机调用 WithClassicFont。英文字符和 Tahoma 两个字重已有测试；中文需要额外字体回退。

### 选择 Dark 后颜色不一致

这不是完整支持的深色经典模式。改用 ClassicTheme.Standard 或其他内置配色，见[配色说明](appearance.md)。

### 消息框没有声音

主题库默认无声音。桌面示例通过 samples/Shared 的帮助代码启用，Browser 与移动端当前没有播放器接入。
