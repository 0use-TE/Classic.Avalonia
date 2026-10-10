using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notepad;

public enum TextEncoding { Utf8, Utf8Bom, Utf16LittleEndian, Utf16BigEndian, Gb18030, Western1252, SystemAnsi }

public sealed class TextDocument
{
    private string _savedText = "";
    private TextEncoding _savedEncoding;
    private string _savedNewLine = "\r\n";
    public string Text { get; set; } = "";
    public string? FileName { get; private set; }
    public TextEncoding EncodingKind { get; set; }
    public int LegacyCodePage { get; private set; } = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
    public string NewLine { get; set; } = "\r\n";
    public bool IsModified => Text != _savedText || EncodingKind != _savedEncoding || NewLine != _savedNewLine;
    public string DisplayName => FileName == null ? "Untitled" : Path.GetFileName(FileName);

    static TextDocument() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public void New()
    {
        Text = "";
        FileName = null;
        EncodingKind = TextEncoding.Utf8;
        NewLine = "\r\n";
        MarkSaved(null);
    }

    public void Load(byte[] bytes, string fileName)
    {
        Encoding encoding;
        int offset;
        if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            (encoding, offset, EncodingKind) = (new UTF8Encoding(false, true), 3, TextEncoding.Utf8Bom);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            (encoding, offset, EncodingKind) = (new UnicodeEncoding(false, false, true), 2, TextEncoding.Utf16LittleEndian);
        else if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            (encoding, offset, EncodingKind) = (new UnicodeEncoding(true, false, true), 2, TextEncoding.Utf16BigEndian);
        else
            (encoding, offset, EncodingKind) = (new UTF8Encoding(false, true), 0, TextEncoding.Utf8);
        string text;
        try { text = encoding.GetString(bytes, offset, bytes.Length - offset); }
        catch (DecoderFallbackException) when (offset == 0)
        {
            // BOM-less legacy files use the current Windows locale's ANSI code page.
            var codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            encoding = Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            text = encoding.GetString(bytes);
            LegacyCodePage = codePage;
            EncodingKind = codePage == 1252 ? TextEncoding.Western1252 : TextEncoding.SystemAnsi;
        }
        Text = text;
        NewLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : text.Contains('\n') ? "\n" : text.Contains('\r') ? "\r" : "\r\n";
        MarkSaved(fileName);
    }

    public byte[] Encode()
    {
        var encoding = EncodingKind == TextEncoding.SystemAnsi
            ? Encoding.GetEncoding(LegacyCodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
            : GetEncoding(EncodingKind);
        var normalized = Text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\n", NewLine, StringComparison.Ordinal);
        return encoding.GetPreamble().Concat(encoding.GetBytes(normalized)).ToArray();
    }

    public static Encoding GetEncoding(TextEncoding kind) => kind switch
    {
        TextEncoding.Utf8 => new UTF8Encoding(false, true),
        TextEncoding.Utf8Bom => new UTF8Encoding(true, true),
        TextEncoding.Utf16LittleEndian => new UnicodeEncoding(false, true, true),
        TextEncoding.Utf16BigEndian => new UnicodeEncoding(true, true, true),
        TextEncoding.Gb18030 => Encoding.GetEncoding(54936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
        TextEncoding.Western1252 => Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
        TextEncoding.SystemAnsi => Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public void MarkSaved(string? fileName)
    {
        FileName = fileName;
        _savedText = Text;
        _savedEncoding = EncodingKind;
        _savedNewLine = NewLine;
    }

    public static async Task WriteAtomicallyAsync(string path, byte[] bytes)
    {
        var fullPath = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }
            File.Move(temporary, fullPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public readonly record struct TextMatch(int Start, int Length);

public static class TextSearch
{
    public static TextMatch? Find(string text, string query, int start, bool matchCase, bool wholeWord, bool backwards = false)
    {
        if (query.Length == 0 || text.Length < query.Length) return null;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matches = new System.Collections.Generic.List<int>();
        for (var index = 0; index <= text.Length - query.Length;)
        {
            index = text.IndexOf(query, index, comparison);
            if (index < 0) break;
            if (!wholeWord || ((index == 0 || !IsWord(text[index - 1])) && (index + query.Length == text.Length || !IsWord(text[index + query.Length]))))
                matches.Add(index);
            index++;
        }
        if (matches.Count == 0) return null;
        var candidate = backwards ? matches.LastOrDefault(index => index < start, -1) : matches.FirstOrDefault(index => index >= start, -1);
        if (candidate < 0) candidate = backwards ? matches[^1] : matches[0];
        return new TextMatch(candidate, query.Length);
    }

    public static (string Text, int Count) ReplaceAll(string text, string query, string replacement, bool matchCase, bool wholeWord)
    {
        if (query.Length == 0) return (text, 0);
        var result = new StringBuilder();
        var position = 0;
        var count = 0;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        for (var index = 0; index <= text.Length - query.Length;)
        {
            index = text.IndexOf(query, index, comparison);
            if (index < 0) break;
            if (wholeWord && ((index > 0 && IsWord(text[index - 1])) || (index + query.Length < text.Length && IsWord(text[index + query.Length]))))
            {
                index++;
                continue;
            }
            result.Append(text, position, index - position).Append(replacement);
            position = index + query.Length;
            index = position;
            count++;
        }
        result.Append(text, position, text.Length - position);
        return (result.ToString(), count);
    }

    private static bool IsWord(char value) => char.IsLetterOrDigit(value) || value == '_';
}
