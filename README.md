# ![icon](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/icon.png) Classic.Avalonia

### 📢 If you have used Classic.Avalonia, share your app [here](https://github.com/BAndysc/Classic.Avalonia/discussions/2)!

## Avalonia 12

This checkout targets .NET 10 and Avalonia 12.0.0. The current repository package version is `12.0.0-beta1`. Use project references while the current implementation is being developed.

## Documentation

The [Chinese DocFX guide](docs/index.md) covers project-reference setup, color schemes, fonts, controls, Crystal samples and platform limits. API documentation is generated from the three current library projects.

GitHub Pages serves [documentation](https://0use-TE.github.io/Classic.Avalonia/) and the [browser preview](https://0use-TE.github.io/Classic.Avalonia/demo/) from one site. The Documentation and Preview workflow deploys master; pull requests build the combined artifact without deploying. Enable GitHub Actions as the repository's Pages source. See [site build instructions](docs/guides/documentation.md).

```powershell
dotnet tool restore
dotnet workload install wasm-tools
dotnet publish samples/Classic.Demo.Browser/Classic.Demo.Browser.csproj -c Release -o artifacts/pages/browser
dotnet docfx docs/docfx.json --warningsAsErrors
pwsh -File scripts/Prepare-GitHubPages.ps1
dotnet docfx serve artifacts/pages/site --hostname 127.0.0.1 --port 8091
```

## Overview

This project brings the classic Windows 9x theme to Avalonia. It is inspired by the [WPF Classic Theme](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes/PresentationFramework.Classic/Themes), with enhancements and additional controls for a more authentic look and feel. Check out the screenshots below for a preview!

## Installation

1. Reference the theme project from your application (adjust the relative path):
   ```xml
   <ItemGroup>
     <ProjectReference Include="../Classic.Avalonia/src/Classic.Avalonia.Theme/Classic.Avalonia.Theme.csproj" />
   </ItemGroup>
   ```
2. In your `App.axaml`, replace the existing theme (e.g., `<FluentTheme />` or `<SimpleTheme />`) with the Classic theme:
   ```xml
   <Application ...>
       <Application.Styles>
           <ClassicTheme />
       </Application.Styles>
   </Application>
   ```

### ColorPicker
For the ColorPicker, add a project reference and include the style in `App.axaml`:
```xml
<ProjectReference Include="../Classic.Avalonia/src/Classic.Avalonia.Theme.ColorPicker/Classic.Avalonia.Theme.ColorPicker.csproj" />
```

```xml
<StyleInclude Source="avares://Classic.Avalonia.Theme.ColorPicker/Classic.axaml"/>
```

The DataGrid theme project is not present in this checkout. GOZA.Dock has a separate classic adapter; historical Dock package instructions do not apply.

### GOZA.Dock

Reference `src/GOZA.Dock.Themes.Classic/GOZA.Dock.Themes.Classic.csproj`, which uses GOZA.Dock 3.0.9. Keep the host's `ClassicTheme` and `.WithClassicFont()`, then opt each shell in:

```xml
<DockShell ClassicDock.IsEnabled="True">
    <!-- Your DockRegion / DockSplitter layout -->
</DockShell>
```

`ClassicDock` is registered in Avalonia's default XAML namespace; no extra namespace prefix is required. Leave `ColorTheme` unset so VS Code colors do not override the classic palette. Run `samples/Classic.Dock.Demo.Desktop` for the standalone Crystal workspace with Windows 98 tabs, or select **Dock** in the main demo. See [the Dock guide](docs/guides/dock.md).

### Color Scheme

To customize the color scheme, set the `RequestedThemeVariant` in `App.axaml` or for individual controls:
```xml
RequestedThemeVariant="{x:Static ClassicTheme.Brick}"
```

All built-in classic variants inherit from Light. There is no complete independent dark classic palette; selecting Avalonia Dark can mix dark auxiliary resources with the default classic palette. Prefer an explicit classic variant.

### Font Usage

The default embedded UI font is Tahoma regular/bold from the original Browser
font assets. Normal UI/caption text uses the original 11 DIPs. Liberation Sans
remains bundled for missing characters. Tahoma retains its Microsoft license.
Neither family covers Chinese; desktop font fallback can provide it, while a
Chinese Browser application needs a suitable embedded fallback font.

Register the embedded fonts in every application host:

```csharp
AppBuilder.Configure<App>().WithClassicFont();
```

The samples already register these fonts. Attribution, sources and licenses
are in `src/Classic.Avalonia.Theme/Fonts/README.md` and are packaged with the theme.

#### Font aliasing

Classic.Avalonia defaults to grayscale antialiasing with Strong hinting and aligned baselines, keeping small Tahoma text readable. For optional unsmoothed retro rendering, set `FontAliasing` to true:

```
<classic:ClassicTheme FontAliasing="True" />
```

## NativeAOT

All three libraries declare `IsAotCompatible=true`. A dedicated library-only host checks all library members with NativeAOT and executes the same control, font, palette, picker and dialog checks in a normally trimmed native executable. AOT/trim diagnostics are treated as errors, without broad warning suppression. See [NativeAOT support and reproduction](docs/guides/native-aot.md).

NativeAOT compatibility is separate from statically linking Avalonia's native graphics dependencies into one executable. StaticLink is an application deployment option, not a theme dependency.

## Demo Architecture

The shared `samples/Classic.Demo` application uses Crystal.Avalonia's
`CrystalApplication`, module registration, dependency injection and automatic
View/ViewModel wiring, with separate `Views` and `ViewModels` folders. Desktop,
Browser, Android and iOS hosts share the same application. See
[the sample guide](samples/Classic.Demo/README.md) and
[verification results](tests/README.md).

## Custom Controls

`Classic.Avalonia` introduces several custom controls with a classic appearance and behavior.

These controls (excluding `ClassicWindow`) are part of the `Classic.CommonControls.Avalonia` assembly and can be used independently of `Classic.Avalonia.Theme`. Who knows, maybe a Luna theme is coming next?

> #### How to use `Classic.CommonControls.Avalonia` without Classic theme
> You can use new controls **without** classic theming, i.e. if Classic theme is only one of many themes your app supports, but you still want to use ToolBar or MessageBox control. Just include the following style:
> ```
> <StyleInclude Source="avares://Classic.CommonControls.Avalonia/Themes/Fluent.axaml" />
> ```
> **!!** Include it **ONLY** if you want to use new controls with `<FluentTheme />`. Don't do it if you use `<ClassicTheme />`

### ClassicWindow

By default, windows don't have the classic titlebar/chrome. To enable a classic titlebar, inherit from `ClassicWindow` instead of the `Window` class.

If inheriting from `ClassicWindow` isn't feasible (for example, if Classic is just one of multiple themes), you can apply the `ClassicWindow` theme like this:
```xml
Theme="{StaticResource ClassicWindow}"
```

**Note**: On Windows 11, the default `Window` class results in rounded corners, which may detract from the classic look. Inherit from `ClassicWindow` to avoid this.

#### ❓ TitleBar Icon Size problem

Avalonia accepts .ico files as window icons, but the way it handles them varies across platforms. On Windows, Avalonia loads and stores the actual .ico file.
Because of that, ClassicTheme can pick up the correct icon size and display it in the title bar. However, on macOS and Linux, where .ico is not a native format, 
Avalonia converts the .ico file to a bitmap internally, which causes it to lose the multiple sizes embedded in the original file. As a result, ClassicTheme no longer
has access to the 16×16 icon needed for the title bar. This leads to a suboptimal appearance of the title-bar icon (blurry due to downscaling a larger image).

To fix this issue, you can provide a 16×16 PNG icon explicitly:
```xml
<classic:ClassicWindow ...
    ClassicWindow.SmallIcon="/path_to_16x16_icon.png">
```

If you want consistent appearance across all platforms, please provide this small 16×16 icon, even if you already use an .ico file for the main Icon property.

### MessageBox

For a classic Windows-style MessageBox:
```csharp
var result = await MessageBox.ShowDialog(parentWindow, "This is a message box", "Title", MessageBoxButtons.Ok, MessageBoxIcon.Information);
```

The theme library does not play sounds by default. Desktop samples opt into an internal `UseMessageBoxSounds()` helper from `samples/Shared`; it is not a public theme API. Error/Warning use chord.wav, Information/Question use ding.wav, and other controls are silent. Browser and mobile sample audio is not implemented.

![MessageBox example](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/messagebox.png)

*(If you can't hear the screenshot above, you might be too young!)*

### ToolBar

MSDN defines a toolbar as "a control containing one or more buttons."

Use `<commonControls:ToolBar>` to host any controls. To add buttons, use `<commonControls:ToolBarButton>` with properties such as `Text`, `SmallIcon` (16px), or `LargeIcon` (24px). Set `IsToggleButton` to enable toggle functionality.

The ToolBar supports small and large sizes, various text placements (Down, Right, or NoText), and can automatically grayscale icons (`GrayscaleIcons`).

![ToolBar Example](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/toolbar.png)

### ListView

A classic ListView which is a ListBox with a predefined template - an icon with text. Supports three views: Icon, SmallIcon, List.

Use `<commonControls:ListView>` with `<commonControls:ListViewItem>` children. If you want to bind a custom objects, use styles to bind text and icon (don't use ItemTemplate):
```
<commonControls:ListView.Styles>
    <Style Selector="commonControls|ListViewItem">
        <Setter Property="SmallIcon" Value="{Binding SmallIcon}" />
        <Setter Property="LargeIcon" Value="{Binding LargeIcon}" />
        <Setter Property="Text" Value="{Binding Text}" />
    </Style>
</commonControls:ListView.Styles>
```

![ToolBar Example](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/listview_icons.gif)


### AboutDialog

For a classic Windows 9x-style 'About' dialog:
```csharp
var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://YourAssembly/YourPathToIcon.png")));
await AboutDialog.ShowDialog(parentWindow, new AboutDialogOptions
{
    Title = "Notepad",
    Copyright = "Copyright (C) 1985-1999",
    Icon = bitmap
});
```

![AboutDialog Example](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/aboutdialog.png)

### FontDialog

For a classic Windows Font picker dialog:
```csharp
var font = await FontDialog.ShowDialog(parentWindow);
if (font != null)
{
    Font = font.Family;
    FontStyle = font.Style;
    FontWeight = font.Weight;
    FontSize = font.Size;
}
```

![FontDialog Example](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/fontdialog.png)

## Example Screenshots

![Avalonia Visual Basic](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_basic.gif)
![Avalonia Internet Explorer](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer1.png)
![Avalonia Notepad](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/notepad.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer2.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer3.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer4.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer5.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer6.png)
![Avalonia Internet Explorer Internet Options](https://raw.githubusercontent.com/BAndysc/Classic.Avalonia/refs/heads/master/samples/examples/avalonia_explorer7.png)
