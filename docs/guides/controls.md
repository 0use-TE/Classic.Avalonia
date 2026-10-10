# 控件与交互

ClassicTheme 为 Avalonia 控件加载经典样式；Classic.CommonControls.Avalonia 则提供额外控件。完整公开成员见 [API 文档](../api/toc.yml)。

## 日期和时间

DatePicker 与 TimePicker 的只读数值区域和箭头均可打开弹层。CalendarDatePicker 的文本区域保留输入能力。

- 鼠标悬停候选值时显示浅色提示，不修改选中值。
- 点击候选值后选中并居中，弹层保持打开，可继续修改其他列。
- 深色选中背景表示当前弹层内的选择，焦点移到 OK 后仍保持显示。
- OK 将临时选择写入控件；Cancel 保留原值，再次打开恢复已确认的选择。
- 右键点击或禁用状态不会打开只读日期/时间弹层。

```xml
<StackPanel Spacing="8">
  <CalendarDatePicker SelectedDate="{Binding CalendarDate, Mode=TwoWay}" />
  <DatePicker SelectedDate="{Binding SelectedDate, Mode=TwoWay}" />
  <TimePicker SelectedTime="{Binding SelectedTime, Mode=TwoWay}" />
</StackPanel>
```

CalendarDate 为 DateTime?，SelectedDate 为 DateTimeOffset?，SelectedTime 为 TimeSpan?。ViewModel 应实现属性变更通知。

## 颜色选择

引用颜色样式项目并加载 Classic.axaml 后，可使用 ColorView 或 ColorPicker：

```xml
<ColorView Color="Red" HorizontalAlignment="Left" VerticalAlignment="Top" />
```

ColorView 提供基本色、自定义色区域、色谱以及 RGB/HSV 数值编辑。Val 表示 HSV 的 Value，不是 HSL 的亮度。色谱滑条和 RGB 编辑器与 Color 属性同步。

## ToolBar 与 ListView

```xml
<common:ToolBar xmlns:common="using:Classic.CommonControls">
  <common:ToolBarButton Text="Open" SmallIcon="avares://MyApp/Assets/open.png" />
</common:ToolBar>
```

ToolBarButton 可配置小图标、大图标和切换状态。ListView 支持 Icon、SmallIcon、List 三种视图。绑定对象时通过 ListViewItem 的 Text、SmallIcon、LargeIcon 等属性映射内容，而不是替换 ItemTemplate。

## 桌面对话框

以下接口要求桌面 Window owner，不能直接作为移动端或 Browser 的窗口对话框方案。

```csharp
using Classic.CommonControls.Dialogs;

var result = await MessageBox.ShowDialog(
    owner, "是否继续？", "确认",
    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

var font = await FontDialog.ShowDialog(owner);

await AboutDialog.ShowDialog(owner, new AboutDialogOptions
{
    Title = "MyApp",
    Copyright = "Copyright 2026"
});
```

InputBox 还提供 prompt、caption、defaultText 参数的 ShowDialog 接口。返回结果及枚举见 API 文档。

## 音效

主题库不会自动播放音效。当前声音实现位于 samples/Shared，属于桌面示例帮助代码，不是主题库公开扩展。

| 桌面示例中的触发条件 | 声音 |
| --- | --- |
| Error / Warning 消息框加载 | chord.wav |
| Information / Question 消息框加载 | ding.wav |
| 无图标消息框 | 无 |
| 普通按钮、菜单、日期/时间变更、应用启动退出 | 无 |

chimes.wav 是未配置触发条件的示例资源。Browser、Android、iOS 没有实现示例声音播放；桌面实际声音取决于平台播放器与音频设备。

## 独立使用公共控件

若应用使用 FluentTheme，可以只引用 Classic.CommonControls.Avalonia，并加载：

```xml
<StyleInclude Source="avares://Classic.CommonControls.Avalonia/Themes/Fluent.axaml" />
```

使用 ClassicTheme 时无需加载此 Fluent 样式。
