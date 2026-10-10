using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Classic.Avalonia.Theme.Utils;

internal class FontUtils
{
    public static readonly AttachedProperty<bool?> FontAliasingProperty = AvaloniaProperty.RegisterAttached<FontUtils, Control, bool?>("FontAliasing");

    public static bool? GetFontAliasing(AvaloniaObject element) => element.GetValue(FontAliasingProperty);

    public static void SetFontAliasing(AvaloniaObject element, bool? value) => element.SetValue(FontAliasingProperty, value);

    static FontUtils()
    {
        FontAliasingProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            var aliased = e.GetNewValue<bool?>() is true;
            TextOptions.SetTextRenderingMode(c, aliased ? TextRenderingMode.Alias : TextRenderingMode.Antialias);
            // Grid fitting preserves small Tahoma strokes; grayscale smoothing preserves fractional spacing.
            TextOptions.SetTextHintingMode(c, TextHintingMode.Strong);
            TextOptions.SetBaselinePixelAlignment(c, BaselinePixelAlignment.Aligned);
        });
    }
}
