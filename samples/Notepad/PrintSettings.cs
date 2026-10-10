using System.Globalization;
using System.Net;
using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Media;

namespace Notepad;

public sealed class PrintSettings
{
    public string Paper { get; set; } = "A4";
    public decimal MarginMillimeters { get; set; } = 15;

    public string CreateHtml(TextDocument document, string fontFamily, double fontSize, bool wrap,
        FontWeight weight = FontWeight.Normal, FontStyle style = FontStyle.Normal)
    {
        var margin = MarginMillimeters.ToString(CultureInfo.InvariantCulture);
        var size = (fontSize * 0.75).ToString("0.##", CultureInfo.InvariantCulture);
        var family = fontFamily.Replace("'", "", System.StringComparison.Ordinal).Replace("\"", "", System.StringComparison.Ordinal)
            .Replace("<", "", System.StringComparison.Ordinal).Replace(">", "", System.StringComparison.Ordinal)
            .Replace("\r", "", System.StringComparison.Ordinal).Replace("\n", "", System.StringComparison.Ordinal);
        var embedded = "";
        if (family == "Tahoma" || family.StartsWith("fonts:Classic#Tahoma", StringComparison.Ordinal))
        {
            using var font = AssetLoader.Open(new Uri("avares://Classic.Avalonia.Theme/Fonts/Tahoma" + (weight >= FontWeight.Bold ? "-Bold" : "") + ".ttf"));
            using var bytes = new MemoryStream();
            font.CopyTo(bytes);
            embedded = "@font-face{font-family:'Tahoma';font-weight:" + (int)weight + ";src:url(data:font/ttf;base64," + Convert.ToBase64String(bytes.ToArray()) + ") format('truetype')}";
            family = "Tahoma";
        }
        if (family == "Liberation Sans" || family.StartsWith("fonts:Classic#Liberation Sans", StringComparison.Ordinal))
        {
            using var font = AssetLoader.Open(new Uri("avares://Classic.Avalonia.Theme/Fonts/LiberationSans-" + (weight >= FontWeight.Bold ? "Bold" : "Regular") + ".ttf"));
            using var bytes = new MemoryStream();
            font.CopyTo(bytes);
            embedded = "@font-face{font-family:'Liberation Sans';font-weight:" + (int)weight + ";src:url(data:font/ttf;base64," + Convert.ToBase64String(bytes.ToArray()) + ") format('truetype')}";
            family = "Liberation Sans";
        }
        return "<!doctype html><html><head><meta charset=\"utf-8\"><title>" + WebUtility.HtmlEncode(document.DisplayName) +
            "</title><style>" + embedded + "@page{size:" + (Paper == "Letter" ? "Letter" : "A4") + ";margin:" + margin + "mm}" +
            "body{margin:0;color:#000;background:#fff}pre{font-family:'" + WebUtility.HtmlEncode(family) +
            "',monospace;font-size:" + size + "pt;font-weight:" + (int)weight + ";font-style:" + (style == FontStyle.Normal ? "normal" : "italic") + ";white-space:" + (wrap ? "pre-wrap;overflow-wrap:anywhere" : "pre") +
            ";margin:0}button{margin:8px}@media print{button{display:none}}</style></head><body>" +
            "<button onclick=\"window.print()\">Print</button><pre>" + WebUtility.HtmlEncode(document.Text) +
            "</pre><script>window.addEventListener('load',async()=>{await document.fonts.ready;window.print()})</script></body></html>";
    }
}
