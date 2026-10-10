using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Styling;
using Classic.Avalonia.Theme;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOZA.Dock;

namespace Classic.DockDemo;

public sealed partial class DockDocument : ObservableObject, IDockTabItem
{
    public DockDocument(string id, string header, string text, bool closable = true)
    {
        Id = id;
        Header = header;
        this.text = text;
        IsClosable = closable;
    }
    public string Id { get; }
    public string Header { get; }
    public bool IsClosable { get; }
    public bool ReuseSurface => true;
    [ObservableProperty] private string text;
}

public sealed partial class MainViewModel : ObservableObject
{
    private int nextDocument = 2;
    public IReadOnlyList<ThemeVariant> Palettes => ClassicTheme.AllVariants;
    public IReadOnlyList<DockPanePresentation> PaneLayouts { get; } = Enum.GetValues<DockPanePresentation>();
    [ObservableProperty] private DockPanePresentation paneLayout = DockPanePresentation.ClassicSeams;
    public ObservableCollection<IDockTabItem> Documents { get; } =
    [
        new DockDocument("readme", "Readme.txt", "Classic workspace\n\nProject: GOZA.Dock\nTheme: Classic.Avalonia\nVersion: 3.0.9"),
        new DockDocument("notes", "Notes.txt", "Release notes\n\n- Classic tabs\n- Tahoma\n- Workspace tools")
    ];
    public ObservableCollection<IDockTabItem> Explorer { get; } =
    [new DockDocument("files", "Explorer", "Workspace\n  Readme.txt\n  Notes.txt\n  Assets\n  References", false)];
    public ObservableCollection<IDockTabItem> Output { get; } =
    [new DockDocument("output", "Output", "Workspace ready.\nGOZA.Dock 3.0.9 loaded.", false),
     new DockDocument("tasks", "Tasks", "No pending tasks.", false)];

    [ObservableProperty] private ThemeVariant palette = ClassicTheme.Standard;
    [ObservableProperty] private string status = "Ready";
    partial void OnPaletteChanged(ThemeVariant value)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = value;
    }

    [RelayCommand]
    private void NewDocument()
    {
        var number = ++nextDocument;
        Documents.Add(new DockDocument($"document-{number}", $"Untitled{number}.txt", ""));
        Status = $"Created Untitled{number}.txt";
    }
}
