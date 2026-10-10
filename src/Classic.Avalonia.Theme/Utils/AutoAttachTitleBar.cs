using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Input;

namespace Classic.Avalonia.Theme.Utils;

[TemplatePart(PART_CaptionButtons, typeof(CaptionButtonsEx), IsRequired = true)]
[PseudoClasses(pseudoClassMinimized, pseudoClassNormal, pseudoClassMaximized, pseudoClassFullscreen, pseudoClassActive)]
internal class AutoAttachTitleBar : TemplatedControl
{
    public static readonly StyledProperty<Window?> HostWindowProperty =
        AvaloniaProperty.Register<AutoAttachTitleBar, Window?>(nameof(HostWindow));

    public static readonly StyledProperty<bool> ShowCaptionButtonsProperty =
        AvaloniaProperty.Register<AutoAttachTitleBar, bool>(nameof(ShowCaptionButtons), true);

    public static readonly StyledProperty<string?> CaptionTitleProperty =
        AvaloniaProperty.Register<AutoAttachTitleBar, string?>(nameof(CaptionTitle));
    public static readonly StyledProperty<WindowIcon?> CaptionIconProperty =
        AvaloniaProperty.Register<AutoAttachTitleBar, WindowIcon?>(nameof(CaptionIcon));
    public static readonly StyledProperty<WindowIcon?> CaptionSmallIconProperty =
        AvaloniaProperty.Register<AutoAttachTitleBar, WindowIcon?>(nameof(CaptionSmallIcon));

    public string? CaptionTitle { get => GetValue(CaptionTitleProperty); private set => SetValue(CaptionTitleProperty, value); }
    public WindowIcon? CaptionIcon { get => GetValue(CaptionIconProperty); private set => SetValue(CaptionIconProperty, value); }
    public WindowIcon? CaptionSmallIcon { get => GetValue(CaptionSmallIconProperty); private set => SetValue(CaptionSmallIconProperty, value); }

    private void UpdateCaption(Window window)
    {
        CaptionTitle = window.Title;
        CaptionIcon = window.Icon;
        CaptionSmallIcon = ClassicWindow.GetSmallIcon(window);
    }

    public Window? HostWindow
    {
        get => GetValue(HostWindowProperty);
        private set => SetValue(HostWindowProperty, value);
    }

    public bool ShowCaptionButtons
    {
        get => GetValue(ShowCaptionButtonsProperty);
        set => SetValue(ShowCaptionButtonsProperty, value);
    }

    private const string PART_CaptionButtons = "PART_CaptionButtons";
    
    private const string pseudoClassMinimized = ":minimized";
    private const string pseudoClassNormal = ":normal";
    private const string pseudoClassMaximized = ":maximized";
    private const string pseudoClassFullscreen = ":fullscreen";
    private const string pseudoClassActive = ":active";
    
    private IDisposable? _disposables;
    private CaptionButtonsEx? _captionButtons;

    // Drawn decorations are siblings of the Window inside Avalonia's presentation host.
    private Window? FindWindow() => TopLevel.GetTopLevel(this) as Window ??
        this.GetVisualAncestors().SelectMany(visual => visual.GetVisualChildren()).OfType<Window>().FirstOrDefault();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Drawn decorations handle their own input. The optional client template
        // uses Avalonia's window API rather than native handles on every backend.
        if (!e.Handled && ShowCaptionButtons && HostWindow is { WindowDecorations: WindowDecorations.None } window &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.ClickCount == 2 && window.CanResize)
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else
                window.BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _captionButtons?.Detach();

        _captionButtons = e.NameScope.Get<CaptionButtonsEx>(PART_CaptionButtons);

        if (FindWindow() is Window window)
        {
            HostWindow = window;
            UpdateCaption(window);
            if (ShowCaptionButtons)
                _captionButtons?.Attach(window);
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (FindWindow() is Window window)
        {
            HostWindow = window;
            UpdateCaption(window);
            if (ShowCaptionButtons)
                _captionButtons?.Attach(window);
            _disposables?.Dispose();
            _disposables = new CompositeDisposable()
            {
                window.GetObservable(Window.TitleProperty).Subscribe(title => CaptionTitle = title),
                window.GetObservable(Window.IconProperty).Subscribe(icon => CaptionIcon = icon),
                window.GetObservable(ClassicWindow.SmallIconProperty).Subscribe(icon => CaptionSmallIcon = icon),
                window.GetObservable(Window.WindowStateProperty)
                    .Subscribe(x =>
                    {
                        PseudoClasses.Set(pseudoClassMinimized, x == WindowState.Minimized);
                        PseudoClasses.Set(pseudoClassNormal, x == WindowState.Normal);
                        PseudoClasses.Set(pseudoClassMaximized, x == WindowState.Maximized);
                        PseudoClasses.Set(pseudoClassFullscreen, x == WindowState.FullScreen);
                    }),
                window.GetObservable(WindowBase.IsActiveProperty)
                    .Subscribe(x =>
                    {
                        PseudoClasses.Set(pseudoClassActive, x);
                    }),
            };
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _disposables?.Dispose();
        _disposables = null;
        HostWindow = null;
        CaptionTitle = null;
        CaptionIcon = null;
        CaptionSmallIcon = null;

        _captionButtons?.Detach();
    }
}
