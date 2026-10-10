# GOZA.Dock 经典主题

`GOZA.Dock.Themes.Classic` 是独立的主题适配项目，依赖 GOZA.Dock **3.0.9**（2026-10-10 查询的最新 NuGet 发布版）及本仓库的 `Classic.Avalonia.Theme`。不修改 GOZA.Dock 源码，也不让基础主题库依赖 Dock。

## 项目引用

```xml
<ItemGroup>
  <ProjectReference Include="../Classic.Avalonia/src/GOZA.Dock.Themes.Classic/GOZA.Dock.Themes.Classic.csproj" />
</ItemGroup>
```

适配项目会传递引用基础主题和 GOZA.Dock。宿主在 `App.axaml` 中使用 `<ClassicTheme />`，启动时调用 `.WithClassicFont()`，与基础主题的接入方式相同。

## 启用主题

```xml
<DockShell ClassicDock.IsEnabled="True">
  <Grid ColumnDefinitions="200,Auto,*">
    <DockRegion ItemsSource="{Binding ExplorerTabs}" />
    <DockSplitter Grid.Column="1" />
    <DockRegion Grid.Column="2" ItemsSource="{Binding Documents}" />
  </Grid>
</DockShell>
```

此处的 `DockShell`、`DockRegion` 和 `DockSplitter` 使用 GOZA.Dock 的 Avalonia XML 命名空间注册；绑定属性由应用的 ViewModel 提供。

适配库也注册到 Avalonia 默认 XML 命名空间 `https://github.com/avaloniaui`，因此在正常的 Avalonia XAML 文件中直接写 `ClassicDock.IsEnabled="True"`，无需额外声明 `xmlns:dockClassic` 或添加命名空间前缀。启用方式仍是附加属性；省去的是前缀，不是属性本身。

`ClassicDock.IsEnabled` 为每个 shell 安装独立样式和资源，可以通过 `ClassicDock.SetIsEnabled(shell, false)` 移除。移除后恢复原有 Dock 样式，保留应用资源、标签和内容。多个工作区可分别选择经典或默认外观。

## 配色与外观

适配层保留 VS Code 资源键，将它们映射到 Classic 的系统颜色。颜色在 shell 的资源上下文中解析，跟随经典配色和资源覆盖变化。默认使用方形面板、经典立体标签与按下按钮；支持上、下、左、右四种标签位置，字号跟随基础主题的 `FontSizeNormal`。

**经典模式下请不要设置 `DockShell.ColorTheme`。** 该属性会直接写入 shell 本地的 VS Code 颜色，优先级高于适配字典。已有的本地颜色覆盖也会优先，适配层不会删除应用自定义资源。

Dock 现有区域、分隔条、内容缓存、关闭、最大化及拖拽逻辑保持由 GOZA.Dock 管理；适配层不重写这些行为。标签选中时使用浅色背景，悬停时使用中间亮度的背景，Win98 标签按下时使用深色背景；这些状态不会通过加粗文字改变标签宽度。拖放目标使用透明背景和黑色虚线框。

## 与原主题的关系

主题配色、标签外观和区域布局可以分别设置。启用适配层会映射 VS Code 颜色资源、替换工具按钮外观，并默认安装 Win98 立体标签；不会删除 GOZA 的原有模板。

| 标签外观 | ClassicDock.UseClassicTabs | TabPresentation |
| --- | --- | --- |
| Win98 立体标签（默认） | True | ClassicTabs |
| GOZA 原生 VS Code 经典标签 | False | ClassicTabs |
| GOZA 原生 VS Code 现代标签 | False | ModernPills |

例如，保留原有现代标签模板并使用经典配色：

```xml
<DockShell ClassicDock.IsEnabled="True"
           ClassicDock.UseClassicTabs="False"
           TabPresentation="ModernPills"
           PanePresentation="ModernCards">
  <!-- 应用的 DockRegion 与 DockSplitter 布局 -->
</DockShell>
```

`UseClassicTabs=False` 时，`TabPresentation=Auto` 保留 GOZA 的自动选择规则：贴边区域使用经典标签，间隔区域使用现代标签。`UseClassicTabs=True` 默认选择 `ClassicTabs`；应用显式指定 `ModernPills` 时使用 GOZA 原生现代模板。原生模板仍采用适配层的经典配色、方形度量和按钮，不会恢复整套 VS Code 原始外观；完全恢复原 Dock 外观请禁用 `ClassicDock.IsEnabled`。

| 区域布局 | PanePresentation | 适配后的行为 |
| --- | --- | --- |
| 贴边直缝 | ClassicSeams（默认） | 区域贴边，常态显示 1px 分隔线；悬停与拖动显示较宽反馈 |
| 间隔面板 | ModernCards | 保留 4px 区域间距和独立面板边框；Classic 配色使用方角 |

这些是布局呈现模式，不是两种拖拽磁吸算法。GOZA 的区域拖放、重排和分隔条拖动功能仍由原库负责。将上方区域的标签放到底部、下方区域的标签放到顶部时，会出现相邻的两排标签，因为它们属于两个独立区域；分隔线或面板间距用于区分它们。若不希望两排标签相邻，可将上方区域改为顶部标签。

## 示例

独立示例项目：`samples/Classic.Dock.Demo.Desktop`。它使用 Crystal.Avalonia 3.1.1 的应用、模块、DI 和 MVVM 架构，工具栏通过 EventToCommand 创建文档。工作区包含 Explorer、编辑文档、Output 和 Tasks，固定展示 Win98 标签，可切换经典配色和两种区域布局、添加和关闭文档、改变标签方向、最大化区域。示例不提供 Tabs 外观选择器；库仍保留 `UseClassicTabs` 和原生 `TabPresentation` 的组合能力。

```powershell
dotnet run --project samples/Classic.Dock.Demo.Desktop/Classic.Dock.Demo.Desktop.csproj
```

现有跨平台演示的 **Dock** 导航页复用同一个工作区视图，因此 GitHub Pages 上的演示也会包含 Dock。

## 验证范围

最新 Dock 专项测试通过 19 项，覆盖四种标签方向、真实的经典边框模板、选择后的内容、14 个配色、重复启用与移除、关闭、添加、最大化、拖放提示，以及三种标签外观与两种区域布局的全部组合。组合测试使用实际指针验证悬停、点击选择、Win98 按下反馈，以及底部标签和下方区域顶部标签的交界分隔线。移动端与 WebAssembly 仅执行编译检查，不宣称已经完成设备或浏览器交互验证。

适配库也纳入严格 NativeAOT 全成员分析和普通裁剪发布，两种 Windows x64 原生产物均通过 318 项库级断言（包括新增的 Dock 检查）。具体范围见 [NativeAOT 支持](native-aot.md)；这不代表整个 Crystal 示例已经完成 AOT 应用认证。
