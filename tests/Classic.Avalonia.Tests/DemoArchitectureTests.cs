using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Threading;
using Classic.Demo.ViewModels;
using Classic.Demo.Views;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Classic.Avalonia.Tests;

public class DemoArchitectureTests
{
    [AvaloniaFact]
    public void Crystal_event_to_command_updates_visible_click_count_and_passes_spin_args()
    {
        var view = new MainView();
        var model = Assert.IsType<MainViewModel>(view.DataContext);
        var window = new Window { Content = view, Width = 960, Height = 700 };
        window.Show();
        try
        {
            model.SelectedCategoryIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var button = view.FindControl<Button>("CommandDemoButton")!;
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, global::Avalonia.Input.MouseButton.Left);
            window.MouseUp(point, global::Avalonia.Input.MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, model.ClickCount);
            Assert.Equal("Clicks: 1", view.FindControl<TextBlock>("CommandClickCount")!.Text);
            var spinner = view.FindControl<ButtonSpinner>("CommandDemoSpinner")!;
            spinner.RaiseEvent(new SpinEventArgs(SpinDirection.Increase) { RoutedEvent = Spinner.SpinEvent });
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Moderate Avalonia", model.SpinnerText);
            Assert.Equal(model.SpinnerText, spinner.Content);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Demo_date_and_time_values_start_populated()
    {
        var view = new MainView();
        var model = Assert.IsType<MainViewModel>(view.DataContext);
        Assert.Equal(DateTime.Today, model.CalendarDate);
        Assert.Equal(DateTime.Today, model.SelectedDate!.Value.Date);
        Assert.NotNull(model.SelectedTime);
    }

    [AvaloniaFact]
    public void Crystal_auto_wires_shell_and_resolves_module_dependencies_per_view()
    {
        var app = Assert.IsAssignableFrom<CrystalApplication>(Application.Current);
        var first = Assert.IsType<MainViewModel>(new MainView().DataContext);
        var second = Assert.IsType<MainViewModel>(new MainView().DataContext);
        Assert.NotSame(first, second);
        Assert.NotSame(first.DataDisplay, second.DataDisplay);
        Assert.Equal(3, first.People.Count);
        first.People.Clear();
        Assert.Equal(3, second.People.Count);
        Assert.IsType<DataDisplayViewModel>(app.Mvvm.ServiceProvider!.GetRequiredService<DataDisplayViewModel>());

        var window = new MainWindow();
        Assert.IsType<MainViewModel>(Assert.IsType<MainView>(window.Content).DataContext);
    }

    [AvaloniaFact]
    public void Model_first_view_location_preserves_the_supplied_model()
    {
        var app = Assert.IsAssignableFrom<CrystalApplication>(Application.Current);
        var model = app.Mvvm.ServiceProvider!.GetRequiredService<MainViewModel>();
        var template = Assert.Single(app.DataTemplates, t => t.Match(model));
        var view = Assert.IsType<MainView>(template.Build(model));
        Assert.Same(model, view.DataContext);
    }

    [AvaloniaFact]
    public void Category_selection_synchronizes_model_desktop_tabs_and_compact_picker()
    {
        var view = new MainView();
        var model = Assert.IsType<MainViewModel>(view.DataContext);
        var window = new Window { Content = view, Width = 390, Height = 700 };
        window.Show();
        try
        {
            var tabs = view.FindControl<TabControl>("DemoCategories")!;
            var picker = view.FindControl<ComboBox>("CompactNavigation")!;
            model.SelectedCategoryIndex = 3;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(3, tabs.SelectedIndex);
            Assert.Equal(3, picker.SelectedIndex);
            tabs.SelectedIndex = 6;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(6, model.SelectedCategoryIndex);
            Assert.Equal(6, picker.SelectedIndex);
            picker.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, model.SelectedCategoryIndex);
            Assert.Equal(2, tabs.SelectedIndex);
        }
        finally { window.Close(); }
    }
}
