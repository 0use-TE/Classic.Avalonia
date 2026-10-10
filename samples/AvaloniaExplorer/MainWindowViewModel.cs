using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Classic.CommonControls;

namespace AvaloniaExplorer;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event Action<Uri>? LoadUriRequest;

    private string url = "https://avaloniaui.net/";
    public string Url
    {
        get => url;
        set => SetField(ref url, value);
    }

    private string? navigationError;
    public string? NavigationError
    {
        get => navigationError;
        private set => SetField(ref navigationError, value);
    }

    private bool canGoBack, canGoForward;
    public bool CanGoBack
    {
        get => canGoBack;
        set => SetField(ref canGoBack, value);
    }
    public bool CanGoForward
    {
        get => canGoForward;
        set => SetField(ref canGoForward, value);
    }

    private int progress;
    public int Progress
    {
        get => progress;
        set => SetField(ref progress, value);
    }

    private ToolbarSize toolbarSize = ToolbarSize.Large;
    public ToolbarSize ToolbarSize
    {
        get => toolbarSize;
        set
        {
            SetField(ref toolbarSize, value);
            OnPropertyChanged(nameof(IsSmallIcons));
            OnPropertyChanged(nameof(IsLargeIcons));
        }
    }
    public bool IsSmallIcons
    {
        get => toolbarSize == ToolbarSize.Small;
        set { if (value) ToolbarSize = ToolbarSize.Small; }
    }
    public bool IsLargeIcons
    {
        get => toolbarSize == ToolbarSize.Large;
        set { if (value) ToolbarSize = ToolbarSize.Large; }
    }
    private ToolbarTextPlacement toolbarTextPlacement = ToolbarTextPlacement.Down;
    public ToolbarTextPlacement ToolbarTextPlacement
    {
        get => toolbarTextPlacement;
        set
        {
            SetField(ref toolbarTextPlacement, value);
            OnPropertyChanged(nameof(IsTextDown));
            OnPropertyChanged(nameof(IsTextRight));
            OnPropertyChanged(nameof(IsTextSelective));
            OnPropertyChanged(nameof(IsTextNo));
        }
    }
    public bool IsTextDown
    {
        get => toolbarTextPlacement == ToolbarTextPlacement.Down;
        set { if (value) ToolbarTextPlacement = ToolbarTextPlacement.Down; }
    }
    public bool IsTextRight
    {
        get => toolbarTextPlacement == ToolbarTextPlacement.Right;
        set { if (value) ToolbarTextPlacement = ToolbarTextPlacement.Right; }
    }
    public bool IsTextSelective
    {
        get => toolbarTextPlacement == ToolbarTextPlacement.RightPreferNoText;
        set { if (value) ToolbarTextPlacement = ToolbarTextPlacement.RightPreferNoText; }
    }
    public bool IsTextNo
    {
        get => toolbarTextPlacement == ToolbarTextPlacement.NoText;
        set { if (value) ToolbarTextPlacement = ToolbarTextPlacement.NoText; }
    }

    private PanelType openedPanel;
    public PanelType OpenedPanel
    {
        get => openedPanel;
        set
        {
            SetField(ref openedPanel, value);
            OnPropertyChanged(nameof(IsSearchOpened));
            OnPropertyChanged(nameof(IsHistoryOpened));
            OnPropertyChanged(nameof(IsFavoritesOpened));
        }
    }
    public bool IsSearchOpened
    {
        get => openedPanel == PanelType.Search;
        set
        {
            if (value)
                OpenedPanel = PanelType.Search;
            else
                OpenedPanel = PanelType.None;
        }
    }
    public bool IsFavoritesOpened
    {
        get => openedPanel == PanelType.Favorites;
        set
        {
            if (value)
                OpenedPanel = PanelType.Favorites;
            else
                OpenedPanel = PanelType.None;
        }
    }
    public bool IsHistoryOpened
    {
        get => openedPanel == PanelType.History;
        set
        {
            if (value)
                OpenedPanel = PanelType.History;
            else
                OpenedPanel = PanelType.None;
        }
    }

    public void Go()
    {
        LoadLink(url);
    }

    public void LoadLink(string? link)
    {
        link = link?.Trim();
        if (string.IsNullOrEmpty(link))
        {
            NavigationError = "Enter a valid web or file address.";
            return;
        }

        if (!link.Contains("://", StringComparison.Ordinal))
            link = "https://" + link;
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeFile))
        {
            NavigationError = "Enter a valid web or file address.";
            return;
        }

        NavigationError = null;
        Url = uri.AbsoluteUri;
        LoadUriRequest?.Invoke(uri);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public enum PanelType
{
    None,
    Search,
    Favorites,
    History
}
