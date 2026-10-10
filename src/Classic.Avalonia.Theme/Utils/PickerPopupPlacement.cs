using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;

namespace Classic.Avalonia.Theme.Utils;

/// <summary>Positions classic picker dropdowns below the editor after the picker sets its wheel offset.</summary>
public class PickerPopupPlacement : AvaloniaObject
{
    public static readonly AttachedProperty<bool> BelowEditorProperty =
        AvaloniaProperty.RegisterAttached<PickerPopupPlacement, Popup, bool>("BelowEditor");
    public static bool GetBelowEditor(Popup popup) => popup.GetValue(BelowEditorProperty);
    public static void SetBelowEditor(Popup popup, bool value) => popup.SetValue(BelowEditorProperty, value);

    static PickerPopupPlacement() => BelowEditorProperty.Changed.AddClassHandler<Popup>((popup, args) =>
    {
        popup.Opened -= OnOpened;
        if (args.NewValue is true) popup.Opened += OnOpened;
    });

    private static void OnOpened(object? sender, System.EventArgs args)
    {
        var popup = (Popup)sender!;
        Dispatcher.UIThread.Post(() =>
        {
            if (!popup.IsOpen || !GetBelowEditor(popup)) return;
            ResetPendingSelection(popup);
            popup.Placement = PlacementMode.BottomEdgeAlignedLeft;
            popup.VerticalOffset = 0;
            popup.HorizontalOffset = 0;
            popup.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.All;
        });
    }

    private static void ResetPendingSelection(Popup popup)
    {
        // Reopening with the same committed value does not reinitialize Avalonia's reused panels.
        if (popup.Child is not Control presenter) return;
        foreach (var panel in presenter.GetVisualDescendants().OfType<DateTimePickerPanel>()
                     .OrderBy(p => p.PanelType))
        {
            int? value = presenter switch
            {
                DatePickerPresenter date => panel.PanelType switch
                {
                    DateTimePickerPanelType.Year => date.Date.Year,
                    DateTimePickerPanelType.Month => date.Date.Month,
                    DateTimePickerPanelType.Day => date.Date.Day,
                    _ => null
                },
                TimePickerPresenter time => panel.PanelType switch
                {
                    DateTimePickerPanelType.Hour => time.ClockIdentifier == "12HourClock"
                        ? (time.Time.Hours + 11) % 12 + 1 : time.Time.Hours,
                    DateTimePickerPanelType.Minute => time.Time.Minutes,
                    DateTimePickerPanelType.Second => time.Time.Seconds,
                    DateTimePickerPanelType.TimePeriod => time.Time.Hours < 12 ? 0 : 1,
                    _ => null
                },
                _ => null
            };
            if (value is int selected && selected != panel.SelectedValue)
                panel.SelectedValue = selected;
        }
    }
}
