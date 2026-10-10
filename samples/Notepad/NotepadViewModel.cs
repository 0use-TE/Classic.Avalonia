using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Classic.CommonControls.Dialogs;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Notepad;

public class NotepadViewModel : INotifyPropertyChanged
{
    public TextDocument Document { get; private set; } = new();
    public string Text
    {
        get => Document.Text;
        set { if (Document.Text == (value ?? "")) return; Document.Text = value ?? ""; RefreshDocument(); }
    }
    public string Title => (Document.IsModified ? "*" : "") + Document.DisplayName + " - Notepad";
    public string FileStatus => (Document.EncodingKind == TextEncoding.SystemAnsi ? "ANSI " + Document.LegacyCodePage : Document.EncodingKind.ToString()) + " | " + (Document.NewLine == "\r\n" ? "CRLF" : Document.NewLine == "\n" ? "LF" : "CR");
    private string _position = "Ln 1, Col 1";
    public string Position { get => _position; set => SetField(ref _position, value); }
    public FontFamily Font { get; private set; } = new("fonts:Classic#Liberation Sans, Tahoma, $Default");
    public FontStyle FontStyle { get; private set; }
    public FontWeight FontWeight { get; private set; } = FontWeight.Normal;
    public double FontSize { get; private set; } = 13;

    public void SetDocument(TextDocument document)
    {
        Document = document;
        RefreshDocument();
    }
    public void RefreshDocument()
    {
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(FileStatus));
    }
    public async Task SelectFont(Window parent)
    {
        var font = await FontDialog.ShowDialog(parent, new FontDialogResult(Font, FontStyle, FontWeight, FontSize));
        if (font == null) return;
        Font = font.Family;
        FontStyle = font.Style;
        FontWeight = font.Weight;
        FontSize = font.Size;
        OnPropertyChanged(nameof(Font));
        OnPropertyChanged(nameof(FontStyle));
        OnPropertyChanged(nameof(FontWeight));
        OnPropertyChanged(nameof(FontSize));
    }
    public async Task About(Window parent)
    {
        using var icon = new Bitmap(AssetLoader.Open(new Uri("avares://Notepad/icon32.png")));
        await AboutDialog.ShowDialog(parent, new AboutDialogOptions
        {
            Title = "Notepad", Copyright = "Classic.Avalonia Notepad", Icon = icon
        });
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
