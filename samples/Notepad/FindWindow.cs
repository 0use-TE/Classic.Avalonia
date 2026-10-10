using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Classic.Avalonia.Theme;

namespace Notepad;

public sealed class FindWindow : ClassicWindow
{
    public TextBox Query { get; } = new();
    public TextBox Replacement { get; } = new();
    public CheckBox MatchCase { get; } = new() { Content = "Match case" };
    public CheckBox WholeWord { get; } = new() { Content = "Whole word" };

    public FindWindow(bool replace, Func<bool, Task> find, Func<bool, Task> replaceText)
    {
        Title = replace ? "Replace" : "Find";
        Width = 440;
        CanResize = false;
        SizeToContent = SizeToContent.Height;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var fields = new StackPanel { Spacing = 6 };
        fields.Children.Add(new TextBlock { Text = "Find what:" });
        fields.Children.Add(Query);
        if (replace)
        {
            fields.Children.Add(new TextBlock { Text = "Replace with:" });
            fields.Children.Add(Replacement);
        }
        fields.Children.Add(MatchCase);
        fields.Children.Add(WholeWord);
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
        var next = new Button { Content = "Find next", IsDefault = true };
        next.Click += async (_, _) => await find(false);
        buttons.Children.Add(next);
        var previous = new Button { Content = "Previous" };
        previous.Click += async (_, _) => await find(true);
        buttons.Children.Add(previous);
        if (replace)
        {
            var one = new Button { Content = "Replace" };
            one.Click += async (_, _) => await replaceText(false);
            buttons.Children.Add(one);
            var all = new Button { Content = "Replace all" };
            all.Click += async (_, _) => await replaceText(true);
            buttons.Children.Add(all);
        }
        var close = new Button { Content = "Cancel", IsCancel = true };
        close.Click += (_, _) => Close();
        fields.Children.Add(buttons);
        fields.Children.Add(close);
        Content = new Border { Padding = new Thickness(10), Child = fields };
        Opened += (_, _) => Query.Focus();
    }
}
