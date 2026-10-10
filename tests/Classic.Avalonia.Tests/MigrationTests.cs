using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Chrome;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using Xunit;

namespace Classic.Avalonia.Tests;

public class MigrationTests
{
    private static Window Show(Control content)
    {
        var window = new Window { Width = 640, Height = 480, Content = content };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    [AvaloniaFact]
    public void Full_demo_window_initializes_and_displays_every_new_page()
    {
        var window = new Classic.Demo.Views.MainWindow();
        window.Show();
        try
        {
            window.UpdateLayout();
            var rootTabs = window.GetVisualDescendants().OfType<TabControl>().First();
            Assert.DoesNotContain(rootTabs.Items.OfType<TabItem>(), tab => tab.Header?.ToString() == "Avalonia 12");
            foreach (var category in rootTabs.Items.OfType<TabItem>())
            {
                rootTabs.SelectedItem = category;
                window.UpdateLayout();
                Assert.True(Assert.IsAssignableFrom<Control>(category.Content).Bounds.Width > 0);
            }
            rootTabs.SelectedItem = rootTabs.Items.OfType<TabItem>().Single(tab => tab.Header?.ToString() == "Navigation");
            window.UpdateLayout();
            var navigationTabs = rootTabs.GetVisualDescendants().OfType<TabControl>().First();
            navigationTabs.SelectedIndex = 1;
            window.UpdateLayout();
            var pages = window.GetVisualDescendants().OfType<TabbedPage>().Single();
            Assert.NotNull(pages.Pages);
            var children = pages.Pages.ToArray();
            Assert.Equal(4, children.Length);
            for (var index = 0; index < children.Length; index++)
            {
                pages.SelectedIndex = index;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.True(children[index].Bounds.Width > 0);
            }
            Assert.IsType<NavigationPage>(children[1]);
            Assert.IsType<DrawerPage>(children[2]);
            Assert.IsType<CarouselPage>(children[3]);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void New_controls_and_existing_color_picker_have_working_templates()
    {
        Control[] controls =
        [
            new CommandBar(), new CommandBarButton { Label = "Command" },
            new CommandBarToggleButton { Label = "Toggle" }, new CommandBarSeparator(),
            new PipsPager { NumberOfPages = 3 }, new ContentPage { Content = new TextBlock { Text = "Page" } },
            new CarouselPage(), new DrawerPage(), new NavigationPage(), new TabbedPage(),
            new GroupBox { Header = "Group" }, new ColorView(), new ColorPicker()
        ];

        foreach (var control in controls)
        {
            var window = Show(control);
            try
            {
                var templated = Assert.IsAssignableFrom<TemplatedControl>(control);
                Assert.NotNull(templated.Template);
                Assert.NotEmpty(control.GetVisualChildren());
                Assert.True(control.Bounds.Width > 0);
            }
            finally { window.Close(); }
        }
    }

    [AvaloniaFact]
    public void TextBox_displays_new_placeholder_properties()
    {
        var text = new TextBox { PlaceholderText = "Enter a value", UseFloatingPlaceholder = true };
        var window = Show(text);
        try
        {
            var labels = text.GetVisualDescendants().OfType<TextBlock>().ToArray();
            Assert.Contains(labels, label => label.Text == "Enter a value" && label.IsVisible);
            text.Text = "Value";
            window.UpdateLayout();
            Assert.Contains(labels, label => label.Name == "floatingPlaceholder" && label.IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CommandBar_opens_overflow_and_toggle_uses_classic_pressed_border()
    {
        var toggle = new CommandBarToggleButton { Label = "Pin" };
        var bar = new CommandBar();
        bar.PrimaryCommands.Add(toggle);
        bar.SecondaryCommands.Add(new CommandBarButton { Label = "Extra" });
        var window = Show(bar);
        try
        {
            toggle.IsChecked = true;
            var border = toggle.GetVisualDescendants().OfType<ClassicBorderDecorator>()
                .First(item => item.Name == "PART_Border");
            Assert.Equal(ClassicBorderStyle.RaisedPressed, border.BorderStyle);
            Assert.Same(ClassicBorderDecorator.ClassicBorderBrush, border.BorderBrush);
            bar.IsOpen = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(bar.IsOpen);
            Assert.Contains(bar.GetVisualDescendants().OfType<Button>(), button => button.Name == "PART_OverflowButton");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task Navigation_can_push_and_pop_and_tabbed_pages_select_content()
    {
        var first = new ContentPage { Header = "First", Content = new TextBlock { Text = "First page" } };
        var second = new ContentPage { Header = "Second", Content = new TextBlock { Text = "Second page" } };
        var navigation = new NavigationPage();
        var window = Show(navigation);
        navigation.PageTransition = null;
        try
        {
            await navigation.PushAsync(first, null);
            await navigation.PushAsync(second, null);
            Assert.Equal(second, navigation.CurrentPage);
            await navigation.PopAsync(null);
            Assert.Equal(first, navigation.CurrentPage);
        }
        finally { window.Close(); }

        var pages = new TabbedPage();
        pages.Pages = new[] { new ContentPage { Header = "One" }, new ContentPage { Header = "Two" } };
        window = Show(pages);
        try
        {
            pages.SelectedIndex = 1;
            window.UpdateLayout();
            var tabs = pages.GetVisualDescendants().OfType<TabControl>().Single();
            Assert.Equal(1, tabs.SelectedIndex);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ListView_supports_mouse_and_keyboard_selection()
    {
        var list = new ListView
        {
            ItemsSource = new[] { new ListViewItem { Text = "One" }, new ListViewItem { Text = "Two" } }
        };
        var window = Show(list);
        try
        {
            var item = Assert.IsType<ListViewItem>(list.ContainerFromIndex(1));
            var label = item.GetVisualDescendants().OfType<TextBlock>().Single(control => control.Name == "PART_Text");
            Assert.Equal("Two", label.Text);
            var point = label.TranslatePoint(new Point(label.Bounds.Width / 2, label.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, list.SelectedIndex);
            var first = Assert.IsType<ListViewItem>(list.ContainerFromIndex(0));
            first.Focus();
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(0, list.SelectedIndex);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Drawn_decorations_provide_caption_buttons_and_classic_frame()
    {
        Assert.True(Application.Current!.TryFindResource(typeof(WindowDrawnDecorations), out var resource));
        var theme = Assert.IsType<ControlTheme>(resource);
        var setter = theme.Setters.OfType<Setter>().Single(item => item.Property == WindowDrawnDecorations.TemplateProperty);
        var template = Assert.IsAssignableFrom<IWindowDrawnDecorationsTemplate>(setter.Value);
        var result = template.Build();
        Assert.NotNull(result.NameScope.Find<Button>("PART_CloseButton"));
        Assert.NotNull(result.NameScope.Find<Button>("PART_MinimizeButton"));
        Assert.NotNull(result.NameScope.Find<Button>("PART_MaximizeButton"));
        Assert.NotNull(result.NameScope.Find<ClassicBorderDecorator>("PART_WindowBorder"));
        Assert.False(result.NameScope.Find<Button>("PART_FullScreenButton")!.IsVisible);
        var minimize = result.NameScope.Find<Button>("PART_MinimizeButton")!;
        var maximize = result.NameScope.Find<Button>("PART_MaximizeButton")!;
        var close = result.NameScope.Find<Button>("PART_CloseButton")!;
        foreach (var (button, key) in new[]
        {
            (minimize, "ClassicCaptionMinimizeGeometry"),
            (maximize, "ClassicCaptionMaximizeGeometry"),
            (close, "ClassicCaptionCloseGeometry")
        })
        {
            var glyph = Assert.IsType<global::Avalonia.Controls.Shapes.Path>(button.Content);
            Assert.True(Application.Current!.TryFindResource(key, out var geometry));
            Assert.Same(geometry, glyph.Data);
            Assert.Null(glyph.RenderTransform);
        }
        var panel = Assert.IsType<StackPanel>(minimize.Parent);
        Assert.Equal(0, panel.Spacing);
        Assert.Equal(new Thickness(2, 0, 0, 0), close.Margin);
    }

    [AvaloniaFact]
    public void Client_decorations_have_a_single_classic_title_bar()
    {
        var window = new ClassicWindow { Width = 400, Height = 300, Title = "Classic title", Content = new TextBox() };
        window.UseClientSideDecorations = true;
        window.Show();
        window.UpdateLayout();
        try
        {
            Assert.Equal(WindowDecorations.None, window.WindowDecorations);
            Assert.Single(window.GetVisualDescendants().OfType<TextBlock>(), label => label.Text == "Classic title");
            Assert.Single(window.GetVisualDescendants().OfType<Button>(), button => button.Name == "PART_CloseButton");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Pager_buttons_change_page_and_drawer_tracks_open_state()
    {
        var pager = new PipsPager { NumberOfPages = 3, SelectedPageIndex = 1, IsPreviousButtonVisible = true, IsNextButtonVisible = true };
        var window = Show(pager);
        try
        {
            var next = pager.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_NextButton");
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(2, pager.SelectedPageIndex);
            var previous = pager.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_PreviousButton");
            previous.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, pager.SelectedPageIndex);
        }
        finally { window.Close(); }

        var drawer = new DrawerPage { Drawer = new TextBlock { Text = "Drawer" }, IsOpen = true };
        window = Show(drawer);
        try
        {
            var split = drawer.GetVisualDescendants().OfType<SplitView>().Single();
            Assert.True(split.IsPaneOpen);
            drawer.IsOpen = false;
            Assert.False(split.IsPaneOpen);
        }
        finally { window.Close(); }
    }
}
