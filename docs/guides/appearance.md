# 配色与字体

## 配色方案与明暗模式

主题按经典配色方案组织。ClassicTheme 提供的所有内置 ThemeVariant 都以 Avalonia 的 Light 为继承基础；经典系统色的 Default 字典使用 Standard 配色。

底层辅助资源中有 Dark 字典，但没有一套完整、独立的深色经典系统色与控件样式。因此 RequestedThemeVariant="Dark" 不应被当作已经支持的完整深色模式；部分辅助资源可能变暗，而经典控件仍使用默认配色。

如果希望经典外观保持一致，请明确选择 Standard 或其他内置经典方案。DocFX 文档网站自身的外观设置与 Avalonia 应用主题无关。

```xml
<Application ...
             xmlns:classic="using:Classic.Avalonia.Theme"
             RequestedThemeVariant="{x:Static classic:ClassicTheme.Brick}">
```

也可以仅为某个窗口或控件设置 RequestedThemeVariant，或在代码中切换：

```csharp
Application.Current!.RequestedThemeVariant = ClassicTheme.Classic;
```

## 内置方案

| 公开属性 | 方案 |
| --- | --- |
| Standard | 默认标准 Windows 配色 |
| Classic | 经典 Windows 配色 |
| Brick | Brick |
| Wheat | Wheat |
| Marine | Marine |
| Sprouce | Sprouce，保留现有 API 拼写 |
| Plum | Plum |
| Rose | Rose |
| Storm | Storm |
| Desert | Desert |
| Eggplant | Eggplant |
| StarsAndStripes | StarsAndStripes |
| Pumpkin | Pumpkin |

ClassicTheme.AllVariants 可用于绑定配色选择列表。

## 字体

WithClassicFont 注册共享主题程序集中的 Tahoma 常规、粗体和 Liberation Sans 回退字体。Standard 和 Classic 配色默认正文与标题使用 Tahoma 11 DIPs，小号字号为 9，大号为 13。部分历史配色保留自己的字体和字号：Eggplant、StarsAndStripes 使用 Times New Roman，Rose 标题使用 Times New Roman 18 号字，Pumpkin 的正文为 13、标题为 14。Times New Roman 未内置，取决于平台字体回退。

Tahoma 恢复的是原作者的字体选择。英文 Windows 98 的默认界面主要采用位图 MS Sans Serif，因此当前外观不等同于逐像素还原其默认字体。

Tahoma 文件来自原仓库的 Browser 字体资源，保留微软授权声明，不属于项目 MIT 或 Liberation Sans 的 OFL 授权范围。授权、来源及哈希记录在主题的 Fonts/README.md 中；字体嵌入标志不等于软件再分发许可。

两种内置字体均不覆盖中文。桌面可能使用系统字体回退；需要中文的 Browser 应用应额外注册适当的嵌入字体，并设置相应字体回退链。

## 字体平滑

FontAliasing 默认为 false，采用灰度抗锯齿、Strong hinting 和基线像素对齐。小字号 Tahoma 强制使用黑白像素时，字符的小数间距与像素取整可能导致笔画挤压；默认平滑模式优先保证可读性。字体仍为内置 Tahoma，字号不变。需要不平滑的复古效果时设置：

```xml
<classic:ClassicTheme FontAliasing="True" />
```

显示缩放和平台渲染方式会影响最终文字观感。

## 窗口图标

桌面建议单独提供 16×16 PNG 小图标，避免不同平台对 ICO 多尺寸数据的处理差异：

```xml
<classic:ClassicWindow ...
                      classic:ClassicWindow.SmallIcon="avares://MyApp/Assets/app-16.png" />
```
