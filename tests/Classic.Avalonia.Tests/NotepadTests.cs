using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using global::Notepad;
using Xunit;
using Classic.CommonControls.Dialogs;
using Avalonia.Threading;

namespace Classic.Avalonia.Tests;

public class NotepadTests
{
    [Fact]
    public void Legacy_files_preserve_the_detected_code_page_when_saved()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ja-JP");
            var bytes = TextDocument.GetEncoding(TextEncoding.SystemAnsi).GetBytes("日本語");
            var document = new TextDocument();
            document.Load(bytes, "Japanese.txt");
            Assert.Equal(TextEncoding.SystemAnsi, document.EncodingKind);
            Assert.Equal(932, document.LegacyCodePage);
            Assert.Equal(bytes, document.Encode());
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    [AvaloniaFact]
    public async Task New_document_cancel_preserves_text_and_discard_clears_undo_history()
    {
        var window = new global::Notepad.MainWindow();
        window.Show();
        var editor = window.FindControl<TextBox>("Editor")!;
        try
        {
            editor.SelectedText = "unsaved work";
            var newDocument = window.ExecuteAsync("New");
            Dispatcher.UIThread.RunJobs();
            var dialog = Assert.Single(window.OwnedWindows);
            Assert.IsType<MessageBox>(dialog.Content).PickOption(MessageBoxResult.Cancel);
            await newDocument;
            Assert.Equal("unsaved work", editor.Text);
            newDocument = window.ExecuteAsync("New");
            Dispatcher.UIThread.RunJobs();
            dialog = Assert.Single(window.OwnedWindows);
            Assert.IsType<MessageBox>(dialog.Content).PickOption(MessageBoxResult.No);
            await newDocument;
            Assert.True(string.IsNullOrEmpty(editor.Text));
            Assert.False(editor.CanUndo);
            Assert.DoesNotContain("*", window.Title!);
        }
        finally { editor.Text = ""; window.Close(); }
    }

    [Theory]
    [InlineData(TextEncoding.Utf8)]
    [InlineData(TextEncoding.Utf8Bom)]
    [InlineData(TextEncoding.Utf16LittleEndian)]
    [InlineData(TextEncoding.Utf16BigEndian)]
    public void Files_preserve_encoding_line_endings_and_dirty_state(TextEncoding encoding)
    {
        var original = new TextDocument { Text = "Hello 中文\nsecond line", EncodingKind = encoding, NewLine = "\n" };
        var loaded = new TextDocument();
        loaded.Load(original.Encode(), "example.txt");
        Assert.Equal(original.Text, loaded.Text);
        Assert.Equal(encoding, loaded.EncodingKind);
        Assert.Equal("\n", loaded.NewLine);
        Assert.False(loaded.IsModified);
        loaded.Text += "!";
        Assert.True(loaded.IsModified);
        loaded.Text = original.Text;
        Assert.False(loaded.IsModified);
        loaded.EncodingKind = encoding == TextEncoding.Utf8 ? TextEncoding.Utf8Bom : TextEncoding.Utf8;
        Assert.True(loaded.IsModified);
    }

    [Fact]
    public void Legacy_encoding_does_not_silently_replace_unsupported_characters()
    {
        var document = new TextDocument { Text = "中文", EncodingKind = TextEncoding.Western1252 };
        Assert.Throws<EncoderFallbackException>(() => document.Encode());
        document.EncodingKind = TextEncoding.Gb18030;
        Assert.Equal("中文", TextDocument.GetEncoding(TextEncoding.Gb18030).GetString(document.Encode()));
    }

    [Fact]
    public void Search_wraps_in_both_directions_and_respects_whole_words_and_case()
    {
        const string text = "cat scatter CAT 中文 cat";
        Assert.Equal(new TextMatch(0, 3), TextSearch.Find(text, "cat", text.Length, false, true));
        Assert.Equal(new TextMatch(19, 3), TextSearch.Find(text, "cat", 0, false, true, true));
        Assert.Equal(new TextMatch(12, 3), TextSearch.Find(text, "CAT", 0, true, true));
        Assert.Null(TextSearch.Find(text, "", 0, false, false));
        Assert.Null(TextSearch.Find(text, "missing", 0, false, false));
        Assert.Equal(("dog scatter dog 中文 dog", 3), TextSearch.ReplaceAll(text, "cat", "dog", false, true));
        Assert.Equal(("catcat", 1), TextSearch.ReplaceAll("cat", "cat", "catcat", false, false));
    }

    [Fact]
    public async Task Saving_replaces_the_file_without_leaving_temporary_files()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Classic.Notepad.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "example.txt");
            await File.WriteAllTextAsync(path, "a longer previous document", TestContext.Current.CancellationToken);
            await TextDocument.WriteAtomicallyAsync(path, Encoding.UTF8.GetBytes("short"));
            Assert.Equal("short", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Print_output_escapes_document_content_and_applies_page_settings()
    {
        var settings = new PrintSettings { Paper = "Letter", MarginMillimeters = 20 };
        var html = settings.CreateHtml(new TextDocument { Text = "<script>unsafe</script> & 中文" }, "Liberation Sans", 16, true);
        Assert.Contains("&lt;script&gt;unsafe&lt;/script&gt; &amp; 中文", html);
        Assert.Contains("size:Letter;margin:20mm", html);
        Assert.Contains("font-size:12pt", html);
        Assert.Contains("white-space:pre-wrap", html);
    }

    [AvaloniaFact]
    public void Bundled_english_font_provides_regular_and_bold_faces()
    {
        var family = new FontFamily("fonts:Classic#Liberation Sans");
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var glyphs));
            Assert.Equal("Liberation Sans", glyphs.FamilyName);
            Assert.Equal(weight, glyphs.Weight);
            foreach (var character in "Classic 98: 0123456789")
                Assert.True(glyphs.CharacterToGlyphMap.TryGetGlyph(character, out var glyph) && glyph != 0);
        }
        var html = new PrintSettings().CreateHtml(new TextDocument { Text = "Hello 中文" }, family.Name, 13, false, FontWeight.Bold);
        Assert.Contains("data:font/ttf;base64,", html);
        Assert.Contains("font-weight:700", html);
        Assert.DoesNotContain("fonts:Classic#", html);
    }

    [AvaloniaFact]
    public async Task Notepad_window_initializes_edits_and_reports_the_actual_caret()
    {
        var window = new global::Notepad.MainWindow();
        window.Show();
        var editor = window.FindControl<TextBox>("Editor")!;
        try
        {
            editor.Text = "first\r\n中文";
            editor.CaretIndex = editor.Text.Length;
            window.UpdateLayout();
            var model = Assert.IsType<NotepadViewModel>(window.DataContext);
            Assert.Equal("Ln 2, Col 3", model.Position);
            Assert.StartsWith("*Untitled", window.Title);
            editor.SelectionStart = 0;
            editor.SelectionEnd = 5;
            await window.ExecuteAsync("Delete");
            Assert.Equal("\r\n中文", editor.Text);
            await window.ExecuteAsync("Encoding:Utf8Bom");
            Assert.Contains("Utf8Bom", model.FileStatus);
        }
        finally
        {
            editor.Text = "";
            await window.ExecuteAsync("Encoding:Utf8");
            window.Close();
        }
    }
}
