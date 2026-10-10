using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Classic.CommonControls;

public class ListViewItem : ListBoxItem
{
    public static readonly StyledProperty<Bitmap?> SmallIconProperty =
        AvaloniaProperty.Register<ListViewItem, Bitmap?>(nameof(SmallIcon));

    public static readonly StyledProperty<Bitmap?> LargeIconProperty =
        AvaloniaProperty.Register<ListViewItem, Bitmap?>(nameof(LargeIcon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ListViewItem, string?>(nameof(Text));

    public Bitmap? SmallIcon
    {
        get => GetValue(SmallIconProperty);
        set => SetValue(SmallIconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Bitmap? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }
}
