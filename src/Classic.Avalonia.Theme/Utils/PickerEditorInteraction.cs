using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Classic.Avalonia.Theme.Utils;

/// <summary>Lets the read-only date/time value area open the picker's existing flyout.</summary>
public sealed class PickerEditorInteraction : AvaloniaObject
{
    public static readonly AttachedProperty<Button?> OpenButtonProperty =
        AvaloniaProperty.RegisterAttached<PickerEditorInteraction, Control, Button?>("OpenButton");

    public static Button? GetOpenButton(Control control) => control.GetValue(OpenButtonProperty);
    public static void SetOpenButton(Control control, Button? value) => control.SetValue(OpenButtonProperty, value);

    static PickerEditorInteraction()
    {
        OpenButtonProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            control.PointerPressed -= OnPressed;
            control.PointerReleased -= OnReleased;
            if (args.NewValue is Button)
            {
                control.PointerPressed += OnPressed;
                control.PointerReleased += OnReleased;
            }
        });
    }

    private static void OnPressed(object? sender, PointerPressedEventArgs args)
    {
        var editor = (Control)sender!;
        if (editor.IsEffectivelyEnabled && args.GetCurrentPoint(editor).Properties.IsLeftButtonPressed)
        {
            args.Pointer.Capture(editor);
            args.Handled = true;
        }
    }

    private static void OnReleased(object? sender, PointerReleasedEventArgs args)
    {
        var editor = (Control)sender!;
        if (args.Pointer.Captured != editor) return;
        args.Pointer.Capture(null);
        if (args.InitialPressMouseButton == MouseButton.Left && editor.IsEffectivelyEnabled &&
            new Rect(editor.Bounds.Size).Contains(args.GetPosition(editor)) &&
            GetOpenButton(editor) is { IsEffectivelyEnabled: true } button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            args.Handled = true;
        }
    }
}
