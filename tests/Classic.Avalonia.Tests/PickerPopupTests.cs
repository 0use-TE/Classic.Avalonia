using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Xunit;

namespace Classic.Avalonia.Tests;

public class PickerPopupTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Read_only_picker_values_open_on_left_click_but_not_right_click_or_disabled(bool useTime)
    {
        Control picker = useTime ? new TimePicker() : new DatePicker();
        var window = new Window { Width = 700, Height = 600, Content = picker };
        window.Show();
        try
        {
            window.UpdateLayout();
            var value = picker.GetVisualDescendants().OfType<Grid>()
                .Single(g => g.Name == (useTime ? "PART_FlyoutButtonContentGrid" : "PART_ButtonContentGrid"));
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            var point = value.TranslatePoint(new Point(value.Bounds.Width / 2, value.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Right);
            window.MouseUp(point, MouseButton.Right);
            Assert.False(popup.IsOpen);
            Click(value);
            Assert.True(popup.IsOpen);
            popup.IsOpen = false;
            picker.IsEnabled = false;
            Click(value);
            Assert.False(popup.IsOpen);
            picker.IsEnabled = true;
            Click(value);
            Assert.True(popup.IsOpen);
            popup.IsOpen = false;
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Picker_selected_values_keep_highlight_when_focus_moves_to_ok()
    {
        var date = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero) };
        var time = new TimePicker { SelectedTime = new TimeSpan(9, 15, 0) };
        var window = new Window { Width = 700, Height = 600, Content = new StackPanel { Children = { date, time } } };
        window.Show();
        try
        {
            foreach (var picker in new Control[] { date, time })
            {
                window.UpdateLayout();
                Click(picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton"));
                var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
                var presenter = popup.Child!;
                presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AcceptButton").Focus();
                Dispatcher.UIThread.RunJobs();
                foreach (var panel in presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsVisible))
                {
                    var selected = Assert.Single(panel.Children.OfType<ListBoxItem>(), item => item.IsSelected);
                    var border = selected.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Bd");
                    Assert.True(picker.TryFindResource(Classic.CommonControls.SystemColors.HighlightBrushKey, out var highlight));
                    Assert.Equal(Assert.IsAssignableFrom<global::Avalonia.Media.ISolidColorBrush>(highlight).Color, Assert.IsAssignableFrom<global::Avalonia.Media.ISolidColorBrush>(border.Background).Color);
                    Assert.Equal(global::Avalonia.Media.Colors.White, Assert.IsAssignableFrom<global::Avalonia.Media.ISolidColorBrush>(selected.Foreground).Color);
                }
                popup.IsOpen = false;
            }
        }
        finally { window.Close(); }
    }

    private static void Click(Control control)
    {
        var root = TopLevel.GetTopLevel(control)!;
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), root)!.Value;
        root.MouseMove(point);
        root.MouseDown(point, MouseButton.Left);
        root.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        root.UpdateLayout();
    }

    [AvaloniaFact]
    public void Single_view_root_provides_overlay_popups_for_all_pickers()
    {
        Control[] pickers = [new CalendarDatePicker(), new DatePicker(), new TimePicker()];
        using var root = new EmbeddableControlRoot(global::Avalonia.Controls.Platform.PlatformManager.CreateWindow())
        {
            Content = new StackPanel { Children = { pickers[0], pickers[1], pickers[2] } }
        };
        root.Prepare();
        root.Measure(new Size(390, 700));
        root.Arrange(new Rect(0, 0, 390, 700));
        foreach (var picker in pickers)
        {
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            popup.ShouldUseOverlayLayer = true;
            picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name is "PART_Button" or "PART_FlyoutButton")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(popup.IsOpen);
            Assert.True(popup.IsUsingOverlayLayer);
            popup.IsOpen = false;
        }
    }

    [AvaloniaFact]
    public void Demo_date_time_page_dropdowns_receive_pointer_clicks_inside_scroller()
    {
        var view = new Classic.Demo.Views.MainView();
        var window = new Window { Width = 960, Height = 720, Content = view };
        window.Show();
        try
        {
            var categories = view.FindControl<TabControl>("DemoCategories")!;
            categories.SelectedItem = categories.Items.OfType<TabItem>().Single(tab => tab.Header?.ToString() == "Date / time");
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var pickers = view.GetVisualDescendants().Where(control => control is CalendarDatePicker or DatePicker or TimePicker).OfType<Control>().ToArray();
            Assert.Equal(3, pickers.Length);
            foreach (var picker in pickers)
            {
                Click(picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name is "PART_Button" or "PART_FlyoutButton"));
                var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
                Assert.True(popup.IsOpen);
                popup.IsOpen = false;
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Picker_changes_commit_on_ok_and_are_discarded_on_cancel()
    {
        var initialDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
        var initialTime = new TimeSpan(9, 15, 0);
        var date = new DatePicker { SelectedDate = initialDate };
        var time = new TimePicker { SelectedTime = initialTime };
        var window = new Window { Width = 700, Height = 600,
            Content = new StackPanel { Margin = new Thickness(30), Children = { date, time } } };
        window.Show();
        try
        {
            window.UpdateLayout();
            foreach (var picker in new Control[] { date, time })
            {
                var button = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
                var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
                foreach (var accept in new[] { false, true })
                {
                    Click(button);
                    var presenter = popup.Child!;
                    var selector = presenter.GetVisualDescendants().OfType<DateTimePickerPanel>()
                        .Single(panel => panel.Name == (picker is DatePicker ? "PART_DaySelector" : "PART_MinuteSelector"));
                    Assert.Equal(picker is DatePicker ? 10 : 15, selector.SelectedValue);
                    var target = selector.Children.OfType<ListBoxItem>()
                        .Single(item => item.Content?.ToString() == (picker is DatePicker ? "12" : "17"));
                    var root = TopLevel.GetTopLevel(target)!;
                    var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), root)!.Value;
                    root.MouseMove(point);
                    Dispatcher.UIThread.RunJobs();
                    Assert.False(target.IsSelected, $"{picker.GetType().Name}, accept={accept}, selected={selector.SelectedValue}, content={target.Content}");
                    var border = target.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Bd");
                    Assert.True(picker.TryFindResource("ThemeControlHighlightLowBrush", out var hover));
                    Assert.Equal(Assert.IsAssignableFrom<global::Avalonia.Media.ISolidColorBrush>(hover).Color,
                        Assert.IsAssignableFrom<global::Avalonia.Media.ISolidColorBrush>(border.Background).Color);
                    Click(target);
                    Assert.Equal(picker is DatePicker ? 12 : 17, selector.SelectedValue);
                    var selected = Assert.Single(selector.Children.OfType<ListBoxItem>(), item => item.IsSelected);
                    Assert.InRange(System.Math.Abs(selected.Bounds.Center.Y - selector.Bounds.Height / 2), 0, 1);
                    Assert.True(popup.IsOpen);
                    Assert.Equal(initialDate, date.SelectedDate);
                    Assert.Equal(initialTime, time.SelectedTime);
                    Click(presenter.GetVisualDescendants().OfType<Button>()
                        .Single(b => b.Name == (accept ? "PART_AcceptButton" : "PART_DismissButton")));
                    Assert.False(popup.IsOpen);
                    Assert.Equal(picker is DatePicker && accept ? 12 : 10, date.SelectedDate!.Value.Day);
                    Assert.Equal(picker is TimePicker && accept ? new TimeSpan(9, 17, 0) : initialTime, time.SelectedTime);
                }
                date.SelectedDate = initialDate;
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Empty_picker_dropdowns_open_with_real_pointer_input_and_disabled_pickers_do_not()
    {
        Control[] pickers = [new CalendarDatePicker(), new DatePicker(), new TimePicker()];
        var window = new Window { Width = 700, Height = 600,
            Content = new StackPanel { Margin = new global::Avalonia.Thickness(30), Children = { pickers[0], pickers[1], pickers[2] } } };
        window.Show();
        try
        {
            window.UpdateLayout();
            foreach (var picker in pickers)
            {
                var button = picker.GetVisualDescendants().OfType<Button>()
                    .Single(b => b.Name is "PART_Button" or "PART_FlyoutButton");
                var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
                var point = button.TranslatePoint(new global::Avalonia.Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
                window.MouseMove(point);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.True(popup.IsOpen, $"{picker.GetType().Name} did not open on a pointer click.");
                Assert.True(popup.Child!.Bounds.Width > 0);
                Assert.True(popup.Child.Bounds.Height > 0);
                popup.IsOpen = false;
                picker.IsEnabled = false;
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.False(popup.IsOpen);
                picker.IsEnabled = true;
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Date_and_time_dropdowns_anchor_to_editor_and_keep_confirm_cancel_behavior()
    {
        var time = new TimePicker { SelectedTime = new TimeSpan(9, 15, 0) };
        var date = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero) };
        var window = new Window { Width = 700, Height = 600,
            Content = new StackPanel { Margin = new global::Avalonia.Thickness(100, 40, 0, 0), Children = { time, date } } };
        window.Show();
        try
        {
            window.UpdateLayout();
            foreach (var picker in new Control[] { time, date })
            {
                var button = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
                Assert.True(popup.IsOpen);
                Assert.Same(picker, popup.PlacementTarget);
                Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, popup.Placement);
                Assert.Equal(0, popup.VerticalOffset);
                var presenter = Assert.IsAssignableFrom<Control>(popup.Child);
                Assert.Contains(presenter.GetVisualDescendants(), c => c is ClassicBorderDecorator);
                var dismiss = presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_DismissButton");
                dismiss.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(popup.IsOpen);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                presenter.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AcceptButton")
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(popup.IsOpen);
            }
            Assert.Equal(new TimeSpan(9, 15, 0), time.SelectedTime);
            Assert.Equal(10, date.SelectedDate!.Value.Day);
        }
        finally { window.Close(); }
    }
}
