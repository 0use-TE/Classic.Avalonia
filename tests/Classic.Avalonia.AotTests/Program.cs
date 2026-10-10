using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Classic.Avalonia.Theme;
using Classic.CommonControls;
using Classic.CommonControls.Dialogs;

namespace Classic.Avalonia.AotTests;

public sealed partial class TestApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}

internal static class Program
{
    private static int _checks;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            AppBuilder.Configure<TestApp>().WithClassicFont().UseSkia().UseHarfBuzz()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .SetupWithoutStarting();
            if (args.Length == 2 && args[0] == "--font-preview")
            {
                FontPreview(args[1]);
                return 0;
            }
            if (args.Length == 2 && args[0] == "--menu-preview")
            {
                MenuPreview(args[1]);
                return 0;
            }
            var log = new BindingLog();
            Logger.Sink = log;
            Require(!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported, "Run the published NativeAOT executable for verification");
            RunChecks();
            Require(log.Errors.Count == 0, "Binding diagnostics: " + string.Join("; ", log.Errors));
            Console.WriteLine($"PASS: {_checks} library checks; dynamic code supported: {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        _checks++;
    }

    private static void Pump(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static ClassicWindow Show(Control content)
    {
        var window = new ClassicWindow { Width = 640, Height = 480, Content = content,
            Title = "0use.txt - Notepad", UseClientSideDecorations = true };
        window.Show();
        Pump(window);
        return window;
    }

    private static void RunChecks()
    {
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
        {
            Require(FontManager.Current.TryGetGlyphTypeface(new Typeface(new FontFamily("fonts:Classic#Tahoma"),
                FontStyle.Normal, weight), out var glyphs), "Embedded Tahoma resolves");
            Require(glyphs!.Weight == weight, "Tahoma face weight");
            foreach (var character in Enumerable.Range(32, 95))
                Require(glyphs.CharacterToGlyphMap.TryGetGlyph(character, out var glyph) && glyph != 0, "Tahoma ASCII glyph");
        }
        Control[] controls = [new Button { Content = "Button" }, new TextBox { Text = "Editor" },
            new ComboBox { ItemsSource = new[] { "One", "Two" }, SelectedIndex = 0 },
            new Calendar(), new CalendarDatePicker(), new DatePicker(), new TimePicker(),
            new NumericUpDown(), new Slider(), new ProgressBar(), new TreeView(),
            new TabControl(), new CommandBar(), new PipsPager { NumberOfPages = 3 },
            new ContentPage(), new CarouselPage(), new DrawerPage(), new NavigationPage(), new TabbedPage(),
            new GroupBox { Header = "Group" }, new ColorView(), new ColorPicker(),
            new ToolBar(), new ListView(), new StatusBar(), new InputBox { Prompt = "Value" }];
        foreach (var control in controls)
        {
            var window = Show(control);
            try
            {
                Require(((TemplatedControl)control).Template != null, control.GetType().Name + " template");
                Require(control.Bounds.Width > 0, control.GetType().Name + " layout");
            }
            finally { window.Close(); }
        }
        foreach (var placement in new[] { Dock.Left, Dock.Right })
        {
            var inner = new TabControl { TabStripPlacement = placement, Items =
                { new TabItem { Header = "A" }, new TabItem { Header = "B" }, new TabItem { Header = "C" } } };
            var outer = new TabControl { TabStripPlacement = placement, Items =
                { new TabItem { Header = "Wide navigation category", Content = inner } } };
            var tabsWindow = Show(outer);
            try
            {
                var innerGrid = inner.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "RootGrid");
                var outerGrid = outer.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "RootGrid");
                Require(!ReferenceEquals(innerGrid.ColumnDefinitions[0], outerGrid.ColumnDefinitions[0]), "Independent tab columns");
                Require(!ReferenceEquals(innerGrid.RowDefinitions[0], outerGrid.RowDefinitions[0]), "Independent tab rows");
                var content = inner.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "ContentPanel");
                var item = (TabItem)inner.SelectedItem!;
                var x = item.TranslatePoint(default, inner)!.Value.X;
                var gap = placement == Dock.Left ? content.Bounds.Left - x - item.Bounds.Width : x - content.Bounds.Right;
                Require(Math.Abs(gap) <= 3, "Side tab content alignment");
            }
            finally { tabsWindow.Close(); }
        }
        var label = new TextBlock { Text = "Windows 98" };
        var paletteWindow = Show(label);
        try
        {
            foreach (var variant in ClassicTheme.AllVariants)
            {
                paletteWindow.RequestedThemeVariant = variant;
                Pump(paletteWindow);
                Require(paletteWindow.TryFindResource("ContentControlThemeFontFamily", variant, out var family)
                    && family is FontFamily expected && label.FontFamily == expected, "Palette font " + variant.Key);
                Require(paletteWindow.TryFindResource(global::Classic.CommonControls.SystemColors.HighlightBrushKey, variant, out var brush)
                    && brush is ISolidColorBrush, "Palette highlight " + variant.Key);
            }
        }
        finally { paletteWindow.Close(); }
        var picker = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero) };
        var pickerWindow = Show(picker);
        try
        {
            var button = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
            var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump(pickerWindow);
            Require(popup.IsOpen, "Date popup");
            popup.Child!.GetVisualDescendants().OfType<DateTimePickerPanel>().Single(p => p.Name == "PART_DaySelector").SelectedValue = 12;
            popup.Child!.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AcceptButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(picker.SelectedDate!.Value.Day == 12, "Date commit");
        }
        finally { pickerWindow.Close(); }
        var color = new ColorView { Color = Colors.Blue };
        var colorWindow = Show(color);
        try
        {
            color.GetVisualDescendants().OfType<NumericUpDown>().Single(c => c.Name == "RedNumericUpDown").Value = 128;
            Pump(colorWindow);
            Require(color.Color == Color.FromRgb(128, 0, 255), "Color editor two-way binding");
        }
        finally { colorWindow.Close(); }
        var time = new TimePicker { SelectedTime = new TimeSpan(9, 15, 0) };
        var timeWindow = Show(time);
        try
        {
            var button = time.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
            var popup = time.GetVisualDescendants().OfType<Popup>().Single();
            foreach (var accept in new[] { false, true })
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Pump(timeWindow);
                var minute = popup.Child!.GetVisualDescendants().OfType<DateTimePickerPanel>().Single(p => p.Name == "PART_MinuteSelector");
                Require(minute.SelectedValue == 15, "Time popup restores committed value");
                minute.SelectedValue = 17;
                popup.Child!.GetVisualDescendants().OfType<Button>().Single(b => b.Name ==
                    (accept ? "PART_AcceptButton" : "PART_DismissButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(time.SelectedTime == new TimeSpan(9, accept ? 17 : 15, 0), "Time confirm/cancel");
            }
        }
        finally { timeWindow.Close(); }
        var owner = Show(new TextBlock { Text = "Dialogs" });
        try
        {
            var messageTask = MessageBox.ShowDialog(owner, "AOT", "Message", MessageBoxButtons.OkCancel, MessageBoxIcon.Information);
            Pump(owner);
            var dialog = owner.OwnedWindows.Single();
            ((MessageBox)dialog.Content!).PickOption(MessageBoxResult.Ok);
            Require(messageTask.GetAwaiter().GetResult() == MessageBoxResult.Ok, "MessageBox result");
            var aboutTask = AboutDialog.ShowDialog(owner, new AboutDialogOptions { Title = "AOT About", Copyright = "Test" });
            Pump(owner);
            dialog = owner.OwnedWindows.Single();
            Require(dialog.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "AOT About"), "About compiled binding");
            var close = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Command != null);
            close.Command!.Execute(null);
            aboutTask.GetAwaiter().GetResult();
            var fontTask = FontDialog.ShowDialog(owner, new FontDialogResult(new FontFamily("fonts:Classic#Tahoma"), FontStyle.Normal, FontWeight.Bold, 11));
            Pump(owner);
            dialog = owner.OwnedWindows.Single();
            var acceptFont = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "OK");
            Require(acceptFont.Command != null, "FontDialog compiled command binding");
            acceptFont.Command!.Execute(null);
            Require(fontTask.GetAwaiter().GetResult() is { Weight: FontWeight.Bold, Size: 11 }, "FontDialog result and model binding");
            var inputTask = InputBox.ShowDialog(owner, "Enter", "Input", "Before");
            Pump(owner);
            dialog = owner.OwnedWindows.Single();
            var input = (InputBox)dialog.Content!;
            input.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_Text").Text = "After";
            Pump(dialog);
            Require(input.Text == "After", "InputBox two-way binding");
            input.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OkButton")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(inputTask.GetAwaiter().GetResult() == "After", "InputBox result");
        }
        finally { owner.Close(); }
    }

    private static void MenuPreview(string output)
    {
        var format = new MenuItem { Header = "F_ormat", Items =
        {
            new MenuItem { Header = "_Word Wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
            new MenuItem { Header = "_Font..." }, new MenuItem { Header = "_Encoding", Items = { new MenuItem { Header = "UTF-8" } } }
        } };
        var menu = new Menu { Items = { new MenuItem { Header = "_File" }, new MenuItem { Header = "_Edit" }, format,
            new MenuItem { Header = "_View" }, new MenuItem { Header = "_Help" } } };
        DockPanel.SetDock(menu, Dock.Top);
        var window = Show(new DockPanel { Children = { menu, new TextBox { Text = "Tahoma regular / bold" } } });
        try
        {
            var popup = format.GetVisualDescendants().OfType<Popup>().Single();
            popup.ShouldUseOverlayLayer = true;
            format.IsSubMenuOpen = true;
            Pump(window);
            using var bitmap = new RenderTargetBitmap(new PixelSize(640, 480), new Vector(96, 96));
            bitmap.Render(window);
            bitmap.Save(output);
        }
        finally { format.IsSubMenuOpen = false; window.Close(); }
    }

    private static void FontPreview(string output)
    {
        var rows = new StackPanel { Spacing = 8, Background = new SolidColorBrush(Color.Parse("#D4D0C8")) };
        foreach (var hint in new[] { TextHintingMode.None, TextHintingMode.Strong })
        {
            var caption = new TextBlock { Text = "0use.txt - Notepad", FontFamily = new FontFamily("fonts:Classic#Tahoma"),
                FontSize = 11, FontWeight = FontWeight.Bold, Foreground = Brushes.White, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
            var band = new Border { Background = new SolidColorBrush(Color.Parse("#000080")), Height = 20,
                Padding = new Thickness(4, 0), Child = caption };
            var menu = new TextBlock { Text = "File   Edit   Search   Format   View   Help", FontSize = 11,
                FontFamily = caption.FontFamily, Margin = new Thickness(4, 0) };
            var row = new StackPanel { Children = { new TextBlock { Text = hint.ToString(), FontSize = 12 }, band, menu } };
            TextOptions.SetTextRenderingMode(row, TextRenderingMode.Alias);
            TextOptions.SetTextHintingMode(row, hint);
            TextOptions.SetBaselinePixelAlignment(row, BaselinePixelAlignment.Aligned);
            rows.Children.Add(row);
        }
        var window = Show(rows);
        try
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize(640, 240), new Vector(96, 96));
            bitmap.Render(rows);
            bitmap.Save(output);
        }
        finally { window.Close(); }
    }

    private sealed class BindingLog : ILogSink
    {
        public List<string> Errors { get; } = [];
        public bool IsEnabled(LogEventLevel level, string area) => area == "Binding" && level >= LogEventLevel.Warning;
        public void Log(LogEventLevel level, string area, object? source, string template) => Errors.Add(template);
        public void Log(LogEventLevel level, string area, object? source, string template, params object?[] values)
            => Errors.Add(template + " " + string.Join(", ", values));
    }
}
