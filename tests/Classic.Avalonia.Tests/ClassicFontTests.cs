using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace Classic.Avalonia.Tests;

public class ClassicFontTests
{
    [AvaloniaFact]
    public void Default_ui_uses_tahoma_with_real_regular_and_bold_faces_and_print_embedding()
    {
        var family = new FontFamily("fonts:Classic#Tahoma");
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var glyphs));
            Assert.Equal("Tahoma", glyphs.FamilyName);
            Assert.Equal(weight, glyphs.Weight);
            foreach (var character in Enumerable.Range(32, 95).Select(c => (char)c))
                Assert.True(glyphs.CharacterToGlyphMap.TryGetGlyph(character, out var glyph) && glyph != 0, $"Missing '{character}' in {glyphs.FamilyName}, {weight}");
            var html = new global::Notepad.PrintSettings().CreateHtml(new global::Notepad.TextDocument { Text = "Windows 98" }, family.Name, 11, false, weight);
            Assert.Contains("data:font/ttf;base64,", html);
            Assert.Contains("format('truetype')", html);
            Assert.DoesNotContain("fonts:Classic#", html);
        }
        var label = new TextBlock { Text = "Windows 98" };
        var window = new Classic.Demo.Views.MainWindow { Content = label };
        window.Show();
        try
        {
            Assert.Equal("Tahoma", label.FontFamily.Name);
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(label.FontFamily), out var defaultGlyphs));
            Assert.Equal("Tahoma", defaultGlyphs.FamilyName);
            Assert.Equal(11, label.FontSize);
            Assert.True(FontManager.Current.TryMatchCharacter(',', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                label.FontFamily, null, out var fallback));
            Assert.True(FontManager.Current.TryGetGlyphTypeface(fallback, out var glyphs));
            Assert.True(glyphs.CharacterToGlyphMap.TryGetGlyph(',', out var comma) && comma != 0);
        }
        finally { window.Close(); }
    }
}
