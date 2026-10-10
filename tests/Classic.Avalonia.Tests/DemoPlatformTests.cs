using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Classic.CommonControls.Dialogs;
using Classic.Demo.Views;
using Xunit;

namespace Classic.Avalonia.Tests;

public class DemoPlatformTests
{
    [AvaloniaFact]
    public void Narrow_view_switches_categories_and_restores_desktop_navigation()
    {
        var view = new MainView();
        var window = new Window { Width = 390, Height = 700, Content = view };
        window.Show();
        try
        {
            window.UpdateLayout();
            var picker = view.FindControl<ComboBox>("CompactNavigation")!;
            var tabs = view.FindControl<TabControl>("DemoCategories")!;
            Assert.True(picker.IsVisible);
            picker.SelectedIndex = 4;
            window.UpdateLayout();
            Assert.Equal(4, tabs.SelectedIndex);
            foreach (var category in tabs.Items.OfType<TabItem>())
            {
                picker.SelectedIndex = tabs.Items.IndexOf(category);
                global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.True(Assert.IsType<ScrollViewer>(category.Content).Bounds.Width > 0, $"{category.Header}: picker={picker.SelectedIndex}, tabs={tabs.SelectedIndex}, same={ReferenceEquals(category.Content, tabs.SelectedContent)}, view={view.Bounds}, category={((Control)category.Content!).Bounds}");
            }
            window.Width = 960;
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.False(picker.IsVisible);
            Assert.Equal(Dock.Left, tabs.TabStripPlacement);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Inline_dialogs_render_block_background_and_complete_on_dismiss_or_detach()
    {
        var view = new MainView();
        var window = new Window { Width = 390, Height = 700, Content = view };
        window.Show();
        try
        {
            foreach (var dialog in new Control[] {
                new MessageBox { Text = "Browser and mobile dialog", Buttons = MessageBoxButtons.YesNoCancel },
                new AboutDialog { DataContext = new AboutDialogViewModel(new AboutDialogOptions { Title = "Classic Demo" }) },
                new FontDialog { DataContext = new FontDialogViewModel() } })
            {
                var pending = view.ShowInlineDialogAsync(dialog, "Dialog");
                window.UpdateLayout();
                Assert.False(view.FindControl<DockPanel>("MainContent")!.IsEnabled);
                Assert.True(view.FindControl<Border>("DialogOverlay")!.IsVisible);
                Assert.True(dialog.Bounds.Width > 0);
                Assert.NotEmpty(dialog.GetVisualDescendants());
                view.DismissInlineDialog();
                Assert.True(pending.IsCompletedSuccessfully);
                Assert.Null(view.FindControl<ContentControl>("DialogContent")!.Content);
            }
            var detached = view.ShowInlineDialogAsync(new TextBlock { Text = "Closing" }, "Closing");
            window.Content = null;
            Assert.True(detached.IsCompletedSuccessfully);
        }
        finally { window.Close(); }
    }
}
