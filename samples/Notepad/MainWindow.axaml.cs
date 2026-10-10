using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Classic.Avalonia.Theme;
using Classic.CommonControls.Dialogs;

namespace Notepad;

public partial class MainWindow : ClassicWindow
{
    private readonly NotepadViewModel _model = new();
    private readonly PrintSettings _printSettings = new();
    private IStorageFile? _file;
    private FindWindow? _findWindow;
    private string _lastQuery = "";
    private bool _matchCase, _wholeWord, _busy, _allowClose, _prompting;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _model;
        Editor.PropertyChanged += (_, change) =>
        {
            if (change.Property == TextBox.CaretIndexProperty || change.Property == TextBox.TextProperty)
                UpdatePosition();
        };
        AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel);
        Closing += HandleClosing;
        Closed += (_, _) => _findWindow?.Close();
        Opened += (_, _) => Editor.Focus();
        UpdatePosition();
    }

    private async void MenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string action })
        {
            e.Handled = true;
            await ExecuteAsync(action);
        }
    }

    private async void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var primary = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        var shortcut = (e.KeyModifiers & primary) != 0;
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        string? action = shortcut ? e.Key switch
        {
            Key.N => "New", Key.O => "Open", Key.S => shift ? "SaveAs" : "Save",
            Key.P => "Print", Key.F => "Find", Key.H => "Replace", Key.G => "GoTo",
            _ => null
        } : e.Key == Key.F3 ? (shift ? "FindPrevious" : "FindNext") : e.Key == Key.F5 ? "DateTime" : null;
        // TextBox owns the standard undo/redo and clipboard shortcuts.
        if (action != null)
        {
            e.Handled = true;
            await ExecuteAsync(action);
        }
    }

    public async Task ExecuteAsync(string action)
    {
        if (_busy) return;
        try
        {
            switch (action)
            {
                case "New":
                    if (await ConfirmDiscardAsync())
                    {
                        _file = null;
                        _model.SetDocument(new TextDocument());
                        ResetEditorHistory();
                    }
                    break;
                case "Open": await OpenAsync(); break;
                case "Save": await SaveAsync(false); break;
                case "SaveAs": await SaveAsync(true); break;
                case "Exit": Close(); break;
                case "Undo": Editor.Undo(); break;
                case "Redo": Editor.Redo(); break;
                case "Cut": Editor.Cut(); break;
                case "Copy": Editor.Copy(); break;
                case "Paste": Editor.Paste(); break;
                case "Delete": Editor.SelectedText = ""; break;
                case "SelectAll": Editor.SelectAll(); break;
                case "DateTime": Editor.SelectedText = DateTime.Now.ToString("t", CultureInfo.CurrentCulture) + " " + DateTime.Now.ToString("d", CultureInfo.CurrentCulture); break;
                case "Find": ShowFind(false); break;
                case "Replace": ShowFind(true); break;
                case "FindNext": await FindAsync(false); break;
                case "FindPrevious": await FindAsync(true); break;
                case "GoTo": await GoToAsync(); break;
                case "WordWrap":
                    Editor.TextWrapping = WordWrapMenu.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
                    ScrollViewer.SetHorizontalScrollBarVisibility(Editor, WordWrapMenu.IsChecked ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
                    break;
                case "Font": await _model.SelectFont(this); break;
                case "PageSetup": await PageSetupAsync(); break;
                case "Print": await PrintAsync(); break;
                case "Help": await ShowHelpAsync(); break;
                case "About": await _model.About(this); break;
                default:
                    if (action.StartsWith("Encoding:", StringComparison.Ordinal))
                        _model.Document.EncodingKind = Enum.Parse<TextEncoding>(action[9..]);
                    else if (action.StartsWith("NewLine:", StringComparison.Ordinal))
                        _model.Document.NewLine = action[8..] switch { "LF" => "\n", "CR" => "\r", _ => "\r\n" };
                    _model.RefreshDocument();
                    break;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            await ErrorAsync(exception.Message);
        }
    }

    private void ResetEditorHistory()
    {
        Editor.IsUndoEnabled = false;
        Editor.IsUndoEnabled = true;
        Editor.CaretIndex = 0;
        Editor.SelectionStart = Editor.SelectionEnd = 0;
        Editor.Focus();
        UpdatePosition();
    }

    private async Task OpenAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open", AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Text documents") { Patterns = new[] { "*.txt", "*.log", "*.csv", "*.md" } }, FilePickerFileTypes.All }
        });
        if (files.Count == 0) return;
        SetBusy(true);
        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var bytes = new MemoryStream();
            await stream.CopyToAsync(bytes);
            var document = new TextDocument();
            document.Load(bytes.ToArray(), files[0].TryGetLocalPath() ?? files[0].Name);
            _file = files[0];
            _model.SetDocument(document);
            ResetEditorHistory();
        }
        finally { SetBusy(false); }
    }

    private async Task<bool> SaveAsync(bool saveAs)
    {
        var target = _file;
        if (saveAs || target == null)
            target = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save As", SuggestedFileName = _file?.Name ?? "Untitled.txt", DefaultExtension = "txt",
                ShowOverwritePrompt = true,
                FileTypeChoices = new[] { new FilePickerFileType("Text documents") { Patterns = new[] { "*.txt" } }, FilePickerFileTypes.All }
            });
        if (target == null) return false;
        SetBusy(true);
        try
        {
            // Encode before opening/truncating any target so unsupported characters cannot destroy an existing file.
            var bytes = _model.Document.Encode();
            if (target.TryGetLocalPath() is { } path)
                await TextDocument.WriteAtomicallyAsync(path, bytes);
            else
            {
                await using var stream = await target.OpenWriteAsync();
                if (stream.CanSeek) stream.SetLength(0);
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }
            _file = target;
            _model.Document.MarkSaved(target.TryGetLocalPath() ?? target.Name);
            _model.RefreshDocument();
            return true;
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        MainMenu.IsEnabled = !busy;
        Editor.IsReadOnly = busy;
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (!_model.Document.IsModified) return true;
        var result = await MessageBox.ShowDialog(this, "Do you want to save changes to " + _model.Document.DisplayName + "?", "Notepad", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return result == MessageBoxResult.No || result == MessageBoxResult.Yes && await SaveAsync(false);
    }

    private async void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose) return;
        if (_busy || _prompting) { e.Cancel = true; return; }
        if (!_model.Document.IsModified) return;
        e.Cancel = true;
        _prompting = true;
        try
        {
            if (await ConfirmDiscardAsync())
            {
                _allowClose = true;
                Close();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            await ErrorAsync(exception.Message);
        }
        finally { _prompting = false; }
    }

    private void ShowFind(bool replace)
    {
        _findWindow?.Close();
        var dialog = new FindWindow(replace, FindAsync, ReplaceAsync);
        dialog.Query.Text = Editor.SelectedText.Length > 0 ? Editor.SelectedText : _lastQuery;
        dialog.MatchCase.IsChecked = _matchCase;
        dialog.WholeWord.IsChecked = _wholeWord;
        _findWindow = dialog;
        dialog.Closed += (_, _) => { ReadFindOptions(dialog); if (_findWindow == dialog) _findWindow = null; };
        dialog.Show(this);
    }

    private void ReadFindOptions(FindWindow? dialog = null)
    {
        dialog ??= _findWindow;
        if (dialog == null) return;
        _lastQuery = dialog.Query.Text ?? "";
        _matchCase = dialog.MatchCase.IsChecked == true;
        _wholeWord = dialog.WholeWord.IsChecked == true;
    }

    private async Task FindAsync(bool backwards)
    {
        ReadFindOptions();
        if (_lastQuery.Length == 0) { ShowFind(false); return; }
        var start = backwards ? Math.Min(Editor.SelectionStart, Editor.SelectionEnd) : Math.Max(Editor.SelectionStart, Editor.SelectionEnd);
        var match = TextSearch.Find(Editor.Text ?? "", _lastQuery, start, _matchCase, _wholeWord, backwards);
        if (match is { } found)
        {
            Editor.CaretIndex = found.Start + found.Length;
            Editor.SelectionStart = found.Start;
            Editor.SelectionEnd = found.Start + found.Length;
            Editor.Focus();
        }
        else await MessageBox.ShowDialog(this, "Cannot find \"" + _lastQuery + "\".", "Notepad", MessageBoxButtons.Ok, MessageBoxIcon.Information);
    }

    private async Task ReplaceAsync(bool all)
    {
        ReadFindOptions();
        if (_lastQuery.Length == 0) return;
        var replacement = _findWindow?.Replacement.Text ?? "";
        if (all)
        {
            var result = TextSearch.ReplaceAll(Editor.Text ?? "", _lastQuery, replacement, _matchCase, _wholeWord);
            Editor.SelectAll();
            Editor.SelectedText = result.Text;
            await MessageBox.ShowDialog(this, "Replaced " + result.Count + " occurrence(s).", "Notepad", MessageBoxButtons.Ok, MessageBoxIcon.Information);
        }
        else
        {
            var selectedStart = Math.Min(Editor.SelectionStart, Editor.SelectionEnd);
            var selected = TextSearch.Find(Editor.Text ?? "", _lastQuery, selectedStart, _matchCase, _wholeWord);
            if (selected is { } found && found.Start == selectedStart && Editor.SelectedText.Length == found.Length)
                Editor.SelectedText = replacement;
            await FindAsync(false);
        }
    }

    private async Task GoToAsync()
    {
        var answer = await InputBox.ShowDialog(this, "Line number:", "Go To Line", "1");
        if (answer == null) return;
        if (!int.TryParse(answer, out var requested) || requested < 1) { await ErrorAsync("Enter a positive line number."); return; }
        var text = Editor.Text ?? "";
        var position = 0;
        var line = 1;
        while (line < requested && position < text.Length)
        {
            if (text[position] == '\r') { if (position + 1 < text.Length && text[position + 1] == '\n') position++; line++; }
            else if (text[position] == '\n') line++;
            position++;
        }
        if (line != requested) { await ErrorAsync("The line number is beyond the end of the document."); return; }
        Editor.SelectionStart = Editor.SelectionEnd = Editor.CaretIndex = position;
        Editor.Focus();
    }

    private void UpdatePosition()
    {
        var text = Editor.Text ?? "";
        var caret = Math.Clamp(Editor.CaretIndex, 0, text.Length);
        var line = 1;
        var column = 1;
        for (var i = 0; i < caret; i++)
        {
            if (text[i] == '\r') { if (i + 1 < caret && text[i + 1] == '\n') i++; line++; column = 1; }
            else if (text[i] == '\n') { line++; column = 1; }
            else if (!char.IsLowSurrogate(text[i])) column++;
        }
        _model.Position = $"Ln {line}, Col {column}";
    }

    private async Task PageSetupAsync()
    {
        var paper = new ComboBox { ItemsSource = new[] { "A4", "Letter" }, SelectedItem = _printSettings.Paper };
        var margin = new NumericUpDown { Minimum = 0, Maximum = 50, Value = _printSettings.MarginMillimeters, Increment = 1 };
        var fields = new StackPanel { Spacing = 8, Margin = new Thickness(10) };
        fields.Children.Add(new TextBlock { Text = "Paper size:" });
        fields.Children.Add(paper);
        fields.Children.Add(new TextBlock { Text = "Margins (mm):" });
        fields.Children.Add(margin);
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
        var ok = new Button { Content = "OK", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        buttons.Children.Add(ok); buttons.Children.Add(cancel); fields.Children.Add(buttons);
        var dialog = new ClassicWindow { Title = "Page Setup", Content = fields, Width = 300, CanResize = false, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
        ok.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        if (await dialog.ShowDialog<bool>(this))
        {
            _printSettings.Paper = paper.SelectedItem as string ?? "A4";
            _printSettings.MarginMillimeters = margin.Value ?? 15;
        }
    }

    private async Task PrintAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Classic.Avalonia.Notepad.Print");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".html");
        var html = _printSettings.CreateHtml(_model.Document, _model.Font.Name, _model.FontSize, WordWrapMenu.IsChecked, _model.FontWeight, _model.FontStyle);
        await File.WriteAllTextAsync(path, html, new UTF8Encoding(false));
        Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri) { UseShellExecute = true });
    }

    private Task ErrorAsync(string message) => MessageBox.ShowDialog(this, message, "Notepad", MessageBoxButtons.Ok, MessageBoxIcon.Error);
    private Task ShowHelpAsync() => MessageBox.ShowDialog(this,
        "New/Open/Save: Ctrl+N / Ctrl+O / Ctrl+S\nSave As: Ctrl+Shift+S\nFind/Replace: Ctrl+F / Ctrl+H\nNext/Previous match: F3 / Shift+F3\nGo to line: Ctrl+G\nTime/date: F5\n\nFormat selects encoding and line endings for the next save. Existing UTF-8/UTF-16 BOMs and line endings are preserved.\nPrint opens your browser's print dialog; choose a printer or Save as PDF. Page Setup controls paper and margins.",
        "Notepad Help", MessageBoxButtons.Ok, MessageBoxIcon.Information);
}
