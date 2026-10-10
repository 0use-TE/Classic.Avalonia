using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Classic.Avalonia.Theme;

namespace AvaloniaExplorer;

public partial class OptionsDialog : ClassicWindow
{
    public OptionsDialog()
    {
        InitializeComponent();
    }

    private void AcceptOptions(object? sender, RoutedEventArgs e) => Close();
}
