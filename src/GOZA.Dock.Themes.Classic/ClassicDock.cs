using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Styling;
using GOZA.Dock.Controls;
using Avalonia.Media;
using Classic.CommonControls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GOZA.Dock.Themes.Classic;

/// <summary>Opt-in classic chrome for an individual DockShell. Requires ClassicTheme in the host.</summary>
public sealed class ClassicDock : AvaloniaObject
{
    public static readonly AttachedProperty<bool> DropOutlineProperty =
        AvaloniaProperty.RegisterAttached<ClassicDock, Border, bool>("DropOutline");

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ClassicDock, DockShell, bool>("IsEnabled");

    /// <summary>Use Windows 98 tab chrome. False preserves GOZA's TabPresentation templates.</summary>
    public static readonly AttachedProperty<bool> UseClassicTabsProperty =
        AvaloniaProperty.RegisterAttached<ClassicDock, DockShell, bool>("UseClassicTabs", true);

    private static readonly AttachedProperty<ClassicDockStyles?> InstalledStylesProperty =
        AvaloniaProperty.RegisterAttached<ClassicDock, DockShell, ClassicDockStyles?>("InstalledStyles");

    static ClassicDock()
    {
        UseClassicTabsProperty.Changed.AddClassHandler<DockShell>((shell, _) => UpdateTabClass(shell));
        DropOutlineProperty.Changed.AddClassHandler<Border>((border, change) =>
        {
            // Keep Dock's required Border part; each target owns its own outline.
            border.Child = change.GetNewValue<bool>() ? new Avalonia.Controls.Shapes.Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                Margin = new Thickness(1),
                IsHitTestVisible = false
            } : null;
        });
        IsEnabledProperty.Changed.AddClassHandler<DockShell>((shell, change) =>
        {
            UpdateTabClass(shell);
            if (change.GetNewValue<bool>())
            {
                if (shell.GetValue(InstalledStylesProperty) is not null)
                    return;
                var styles = new ClassicDockStyles();
                // Shell-local resources take precedence over Dock's self-contained default palette.
                shell.Resources.MergedDictionaries.Add(styles.Palette);
                shell.Styles.Add(styles);
                foreach (var border in shell.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "PART_DropHint"))
                    border.SetCurrentValue(DropOutlineProperty, true);
                styles.ObservePalette(shell);
                shell.SetValue(InstalledStylesProperty, styles);
            }
            else if (shell.GetValue(InstalledStylesProperty) is { } styles)
            {
                foreach (var border in shell.GetVisualDescendants().OfType<Border>().Where(GetDropOutline))
                    border.SetCurrentValue(DropOutlineProperty, false);
                shell.Styles.Remove(styles);
                styles.StopObservingPalette();
                shell.Resources.MergedDictionaries.Remove(styles.Palette);
                shell.ClearValue(InstalledStylesProperty);
            }
        });
    }

    public static bool GetIsEnabled(DockShell shell) => shell.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DockShell shell, bool value) => shell.SetValue(IsEnabledProperty, value);
    public static bool GetUseClassicTabs(DockShell shell) => shell.GetValue(UseClassicTabsProperty);
    public static void SetUseClassicTabs(DockShell shell, bool value) => shell.SetValue(UseClassicTabsProperty, value);
    private static void UpdateTabClass(DockShell shell) =>
        shell.Classes.Set("classic-dock-tabs", GetIsEnabled(shell) && GetUseClassicTabs(shell));
    public static bool GetDropOutline(Border border) => border.GetValue(DropOutlineProperty);
    public static void SetDropOutline(Border border, bool value) => border.SetValue(DropOutlineProperty, value);
}

internal sealed class ClassicDockStyles : Styles
{
    private readonly List<(object Key, SolidColorBrush[] Brushes)> colors = [];
    private DockShell? owner;
    public IResourceDictionary Palette { get; } = new ClassicDockPalette();

    public ClassicDockStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ObservePalette(DockShell shell)
    {
        owner = shell;
        shell.ActualThemeVariantChanged += OnThemeVariantChanged;
        shell.ResourcesChanged += OnResourcesChanged;
        Watch(shell, SystemColors.WindowColorKey, ["editor.background"]);
        Watch(shell, SystemColors.WindowTextColorKey, ["editor.foreground"]);
        Watch(shell, SystemColors.ControlColorKey, ["editorGroupHeader.tabsBackground", "surface.background", "tab.inactiveBackground", "modernEditorTab.inactiveBackground", "toolbar.hoverBackground", "toolbar.activeBackground", "titleBar.activeBackground", "DockDragGhostBackgroundBrush"]);
        Watch(shell, SystemColors.ControlLightLightColorKey, ["tab.activeBackground", "tab.activeBorder", "modernEditorTab.activeBackground", "modernEditorTab.activeHoverBackground"]);
        Watch(shell, SystemColors.ControlDarkColorKey, ["editorGroup.border", "editorGroupHeader.tabsBorder", "surface.border", "tab.border", "sash.hoverBorder", "toolbar.hoverOutline", "titleBar.border"]);
        Watch(shell, SystemColors.HighlightColorKey, ["tab.activeBorderTop"]);
        Watch(shell, SystemColors.ControlTextColorKey, ["surface.foreground", "tab.activeForeground", "tab.inactiveForeground", "tab.hoverForeground", "modernEditorTab.activeForeground", "modernEditorTab.hoverForeground", "icon.foreground", "titleBar.activeForeground", "DockDragGhostForegroundBrush"]);
        Watch(shell, SystemColors.HighlightColorKey, ["focusBorder"]);
        Watch(shell, SystemColors.ControlDarkDarkColorKey, ["DockDragGhostBorderBrush"]);
        RefreshPalette();
    }

    private void Watch(DockShell shell, object colorKey, string[] brushKeys)
    {
        var brushes = brushKeys.Select(key => (SolidColorBrush)Palette[key]!).ToArray();
        colors.Add((colorKey, brushes));
    }

    public void StopObservingPalette()
    {
        if (owner is { } shell)
        {
            shell.ActualThemeVariantChanged -= OnThemeVariantChanged;
            shell.ResourcesChanged -= OnResourcesChanged;
        }
        owner = null;
        colors.Clear();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs change) =>
        Dispatcher.UIThread.Post(RefreshPalette);

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs change) =>
        Dispatcher.UIThread.Post(RefreshPalette);

    private void RefreshPalette()
    {
        if (owner is not { } shell)
            return;
        foreach (var (key, brushes) in colors)
            if (shell.TryFindResource(key, out var value) && value is Color color)
                foreach (var brush in brushes)
                    brush.Color = color;
        // Some classic palettes make ControlLight identical to Control; blend a visible hover tint.
        if (shell.TryFindResource(SystemColors.ControlColorKey, out var baseValue) && baseValue is Color baseColor
            && shell.TryFindResource(SystemColors.ControlLightLightColorKey, out var lightValue) && lightValue is Color lightColor)
        {
            var hover = Color.FromRgb((byte)((baseColor.R + lightColor.R) / 2),
                (byte)((baseColor.G + lightColor.G) / 2), (byte)((baseColor.B + lightColor.B) / 2));
            ((SolidColorBrush)Palette["tab.hoverBackground"]!).Color = hover;
            ((SolidColorBrush)Palette["modernEditorTab.hoverBackground"]!).Color = hover;
        }
    }
}

internal sealed class ClassicDockPalette : ResourceDictionary
{
    public ClassicDockPalette() => AvaloniaXamlLoader.Load(this);
}
