using System;
using Avalonia;
using Classic.Avalonia.Theme;
using Classic.CommonControls.Dialogs;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace Classic.Demo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        NavigationDemo.Content = CreateNavigationPage(1);
        CompactNavigation.ItemsSource = System.Linq.Enumerable.Select(
            System.Linq.Enumerable.OfType<TabItem>(DemoCategories.Items), item => item.Header?.ToString());
        CompactNavigation.SelectedIndex = DemoCategories.SelectedIndex;
        foreach (var category in System.Linq.Enumerable.OfType<TabItem>(DemoCategories.Items))
        {
            if (category.Content is Control content && content is not ScrollViewer)
            {
                category.Content = null;
                category.Content = new ScrollViewer
                {
                    Content = content,
                    HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                };
            }
        }
        SizeChanged += (_, _) =>
        {
            var compact = Bounds.Width < 600;
            CompactNavigation.IsVisible = compact;
            DemoCategories.Classes.Set("compact", compact);
            DemoCategories.TabStripPlacement = compact ? Dock.Top : Dock.Left;
            DialogScroller.MaxHeight = Math.Max(100, Bounds.Height - 80);
        };
        DetachedFromVisualTree += (_, _) => DismissInlineDialog();
    }

    private ContentPage CreateNavigationPage(int number)
    {
        var next = new Button { Content = "Open next page" };
        next.Click += async (_, _) => await NavigationDemo.PushAsync(CreateNavigationPage(number + 1));
        return new ContentPage
        {
            Header = $"Page {number}",
            Content = new StackPanel { Margin = new Thickness(8), Spacing = 8,
                Children = { new TextBlock { Text = $"Navigation page {number}" }, next } }
        };
    }
    private async void OpenSaveDialog(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider.CanOpen != true) return;
        await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false
        });
    }

    private async void OpenAboutDialog(object? sender, RoutedEventArgs e)
    {
        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://Classic.Demo/icon16.png")));
        var options = new AboutDialogOptions()
        {
            Title = "Avalonia",
            Copyright = "Copyleft (R) 2024",
            Icon = bitmap
        };
        if (TopLevel.GetTopLevel(this) is Window owner) await AboutDialog.ShowDialog(owner, options);
        else {
            var vm = new AboutDialogViewModel(options);
            vm.CloseRequested += DismissInlineDialog;
            await ShowInlineDialogAsync(new AboutDialog { DataContext = vm }, "About Avalonia");
        }
    }

    private async void OpenFontDialog(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window owner) await FontDialog.ShowDialog(owner);
        else {
            var vm = new FontDialogViewModel(null);
            vm.CancelRequested += DismissInlineDialog;
            vm.AcceptRequested += _ => DismissInlineDialog();
            await ShowInlineDialogAsync(new FontDialog { DataContext = vm }, "Font");
        }
    }

    private async void OpenMessageBox(object? sender, RoutedEventArgs e)
    {
        var text = MessageBoxText.Text ?? "";
        var caption = MessageBoxCaption.Text ?? "";
        var buttons = (MessageBoxButtons?)((ComboBoxItem?)MessageBoxButtons.SelectedItem)?.Content ?? default;
        var icon = (MessageBoxIcon?)((ComboBoxItem?)MessageBoxIcon.SelectedItem)?.Content ?? default;
        if (TopLevel.GetTopLevel(this) is Window owner)
            await MessageBox.ShowDialog(owner, text, caption, buttons, icon);
        else {
            var dialog = new MessageBox { Text = text, Buttons = buttons, Icon = icon };
            dialog.AcceptRequest += _ => DismissInlineDialog();
            await ShowInlineDialogAsync(dialog, caption);
        }
    }

    private System.Threading.Tasks.TaskCompletionSource? pendingDialog;
    public System.Threading.Tasks.Task ShowInlineDialogAsync(Control content, string title)
    {
        DismissInlineDialog();
        pendingDialog = new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        DialogTitle.Text = title;
        DialogContent.Content = content;
        MainContent.IsEnabled = false;
        DialogOverlay.IsVisible = true;
        content.Focus();
        return pendingDialog.Task;
    }

    public void DismissInlineDialog()
    {
        DialogOverlay.IsVisible = false;
        DialogContent.Content = null;
        MainContent.IsEnabled = true;
        pendingDialog?.TrySetResult();
        pendingDialog = null;
    }

    private void CloseInlineDialog(object? sender, RoutedEventArgs e) => DismissInlineDialog();
    private async void OpenFindDialog(object? sender, RoutedEventArgs e)
    {
        var input = new TextBox { PlaceholderText = "Find a control category", Margin = new Thickness(4) };
        var result = new TextBlock { Margin = new Thickness(4) };
        var find = new Button { Content = "Find", Margin = new Thickness(4) };
        find.Click += (_, _) =>
        {
            foreach (var item in System.Linq.Enumerable.OfType<TabItem>(DemoCategories.Items))
                if (!string.IsNullOrWhiteSpace(input.Text) &&
                    item.Header?.ToString()?.Contains(input.Text, StringComparison.OrdinalIgnoreCase) == true)
                {
                    DemoCategories.SelectedItem = item;
                    CompactNavigation.SelectedIndex = DemoCategories.SelectedIndex;
                    DismissInlineDialog();
                    return;
                }
            result.Text = "No matching category.";
        };
        await ShowInlineDialogAsync(new StackPanel { MinWidth = 240, Children = { input, find, result } }, "Find category");
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is { } app && e.AddedItems.Count > 0 && e.AddedItems[0] is ThemeVariant variant) app.RequestedThemeVariant = variant;
    }
}
