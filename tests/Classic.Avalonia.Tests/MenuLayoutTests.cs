using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Classic.Avalonia.Tests;

public class MenuLayoutTests
{
    [AvaloniaTheory]
    [InlineData(11)]
    [InlineData(24)]
    public void Format_popup_and_word_wrap_text_do_not_overlap_the_menu_bar(double fontSize)
    {
        var word = new MenuItem { Header = "_Word Wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
        var font = new MenuItem { Header = "_Font..." };
        var format = new MenuItem { Header = "F_ormat", Items = { word, font } };
        var menu = new Menu { FontSize = fontSize, Items = { new MenuItem { Header = "_Edit" }, format } };
        var window = new Window { Width = 500, Height = 300,
            Content = new DockPanel { Children = { menu, new TextBox() } } };
        DockPanel.SetDock(menu, Dock.Top);
        window.Show();
        try
        {
            window.UpdateLayout();
            var popup = format.GetVisualDescendants().OfType<Popup>().Single();
            popup.ShouldUseOverlayLayer = true;
            format.IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.True(popup.IsOpen);
            Assert.True(popup.IsUsingOverlayLayer);
            var popupOrigin = popup.Child!.TranslatePoint(default, window)!.Value;
            var formatOrigin = format.TranslatePoint(default, window)!.Value;
            Assert.True(popupOrigin.Y >= formatOrigin.Y + format.Bounds.Height,
                $"Popup top {popupOrigin.Y} overlaps menu bottom {formatOrigin.Y + format.Bounds.Height}");
            foreach (var item in new[] { format, word, font })
            {
                var text = item.GetVisualDescendants().OfType<AccessText>().First();
                var options = text.GetVisualAncestors().Prepend(text).Reverse()
                    .Aggregate(default(TextOptions), (current, visual) => TextOptions.GetTextOptions(visual).MergeWith(current));
                Assert.Equal(TextRenderingMode.Antialias, options.TextRenderingMode);
                Assert.Equal(TextHintingMode.Strong, options.TextHintingMode);
                Assert.Equal(BaselinePixelAlignment.Aligned, options.BaselinePixelAlignment);
                var origin = text.TranslatePoint(default, item)!.Value;
                Assert.True(origin.Y >= 0);
                Assert.True(origin.Y + text.Bounds.Height <= item.Bounds.Height,
                    $"{item.Header}: text bottom {origin.Y + text.Bounds.Height}, row height {item.Bounds.Height}");
            }
            Assert.True(word.Bounds.Bottom <= font.Bounds.Top);
        }
        finally { format.IsSubMenuOpen = false; window.Close(); }
    }
}
