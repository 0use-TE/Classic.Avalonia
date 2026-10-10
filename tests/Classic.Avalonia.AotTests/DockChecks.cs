using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using GOZA.Dock;
using GOZA.Dock.Controls;
using GOZA.Dock.Themes.Classic;

namespace Classic.Avalonia.AotTests;

internal static partial class Program
{
    private sealed record DockSmokeTab(string Id, string Header) : IDockTabItem;

    private static void RunDockChecks()
    {
        var tabs = new[] { new DockSmokeTab("first", "First"), new DockSmokeTab("second", "Second") };
        var region = new DockRegion { ItemsSource = tabs };
        var shell = new DockShell { Content = new Grid { Children = { region } } };
        shell.DataTemplates.Add(new FuncDataTemplate<DockSmokeTab>((tab, _) => new TextBlock { Text = tab.Header }));
        ClassicDock.SetIsEnabled(shell, true);
        var window = Show(shell);
        try
        {
            foreach (var placement in Enum.GetValues<DockTabStripPlacement>())
            {
                region.TabStripPlacement = placement;
                Pump(window);
                var items = region.GetVisualDescendants().OfType<TabStripItem>().ToArray();
                Require(items.Length == 2, "Dock tabs realized in " + placement);
                Require(items.All(item => item.GetVisualDescendants().OfType<ClassicBorderDecorator>()
                    .Any(border => border.Name == "PART_ClassicTabBorder")), "Dock compiled classic tab template");
            }
            region.SelectedItem = tabs[1];
            Pump(window);
            Require(region.ActiveContent is TextBlock { Text: "Second" }, "Dock content selection");
            foreach (var variant in ClassicTheme.AllVariants)
            {
                window.RequestedThemeVariant = variant;
                Pump(window);
                Require(shell.TryFindResource("surface.background", out var brush) && brush is ISolidColorBrush dockBrush
                    && shell.TryFindResource(SystemColors.ControlColorKey, out var value) && value is Color color
                    && dockBrush.Color == color, "Dock palette " + variant);
            }
            Require(shell.MaximizeRegion(region), "Dock maximize");
            Pump(window);
            Require(shell.RestoreMaximizedRegion(), "Dock restore");
            ClassicDock.SetIsEnabled(shell, false);
            Pump(window);
            Require(!ClassicDock.GetIsEnabled(shell), "Dock remove adapter");
        }
        finally { window.Close(); }
    }
}
