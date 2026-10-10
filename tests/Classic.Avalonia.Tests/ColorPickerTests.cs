using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Classic.Avalonia.Tests;

public class ColorPickerTests
{
    [AvaloniaFact]
    public void Spectrum_slider_updates_color_and_custom_color_toggle_controls_visibility()
    {
        var color = new ColorView { Color = Colors.Red };
        var window = new Window { Width = 600, Height = 600, Content = color };
        window.Show();
        try
        {
            window.UpdateLayout();
            var slider = color.GetVisualDescendants().OfType<ColorSlider>()
                .Single(c => c.Name == "ColorSpectrumThirdComponentSlider");
            slider.Value = (slider.Minimum + slider.Maximum) / 2;
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(color.Color.R, (byte)120, (byte)135);
            Assert.Equal(0, color.Color.G);
            Assert.Equal(0, color.Color.B);
            color.Color = Colors.Blue;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(color.HsvColor, slider.HsvColor);
            var toggle = color.GetVisualDescendants().OfType<ToggleButton>()
                .Single(c => c.Content is string s && s.StartsWith("Define Custom Colors"));
            toggle.IsChecked = false;
            Dispatcher.UIThread.RunJobs();
            Assert.False(color.IsColorSpectrumVisible);
            toggle.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(color.IsColorSpectrumVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Rgb_editors_update_color_and_follow_external_color_changes()
    {
        var color = new ColorView { Color = Colors.Blue };
        var window = new Window { Width = 600, Height = 600, Content = color };
        window.Show();
        try
        {
            window.UpdateLayout();
            var red = color.GetVisualDescendants().OfType<NumericUpDown>().Single(c => c.Name == "RedNumericUpDown");
            var green = color.GetVisualDescendants().OfType<NumericUpDown>().Single(c => c.Name == "GreenNumericUpDown");
            var blue = color.GetVisualDescendants().OfType<NumericUpDown>().Single(c => c.Name == "BlueNumericUpDown");
            Assert.Equal(255m, blue.Value);
            red.Value = 128;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.FromRgb(128, 0, 255), color.Color);
            color.Color = Colors.Lime;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0m, red.Value);
            Assert.Equal(255m, green.Value);
            Assert.Equal(0m, blue.Value);
        }
        finally { window.Close(); }
    }
}
