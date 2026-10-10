using System;
using Avalonia;
using Avalonia.Controls;
using Classic.CommonControls.Utils;

namespace Classic.Avalonia.Theme;

public class ClassicWindow : Window
{
    protected override Type StyleKeyOverride => typeof(ClassicWindow);
    public static readonly AttachedProperty<WindowIcon?> SmallIconProperty = AvaloniaProperty.RegisterAttached<ClassicWindow, Window, WindowIcon?>("SmallIcon");
    public static WindowIcon? GetSmallIcon(AvaloniaObject element) => element.GetValue(SmallIconProperty);
    public static void SetSmallIcon(AvaloniaObject element, WindowIcon? value) => element.SetValue(SmallIconProperty, value);

    /// <summary>Opts into the same client title-bar template on any desktop backend.</summary>
    public static readonly StyledProperty<bool> UseClientSideDecorationsProperty =
        AvaloniaProperty.Register<ClassicWindow, bool>(nameof(UseClientSideDecorations));
    public bool UseClientSideDecorations
    {
        get => GetValue(UseClientSideDecorationsProperty);
        set => SetValue(UseClientSideDecorationsProperty, value);
    }

    static ClassicWindow() => UseClientSideDecorationsProperty.Changed.AddClassHandler<ClassicWindow>((window, change) =>
        window.Classes.Set("classic-client-decorations", change.GetNewValue<bool>()));
}

internal class ClassicWindowFactory : IWindowFactory
{
    public Window Create() => new ClassicWindow();
}
