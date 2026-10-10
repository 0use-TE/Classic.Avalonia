using System;
using Avalonia;
using Avalonia.Media.Fonts;

namespace Classic.Avalonia.Theme;

public static class ClassicFontExtensions
{
    /// <summary>Registers Tahoma regular/bold and the Liberation Sans fallback faces.</summary>
    public static AppBuilder WithClassicFont(this AppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureFonts(manager => manager.AddFontCollection(
            new EmbeddedFontCollection(new Uri("fonts:Classic"),
                new Uri("avares://Classic.Avalonia.Theme/Fonts"))));
    }
}
