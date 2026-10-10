using Avalonia;
using Avalonia.Controls;

namespace Classic.CommonControls;

public class ListView : ListBox
{
    public static readonly StyledProperty<ListViewStyle> ViewProperty = AvaloniaProperty.Register<ListView, ListViewStyle>(nameof(View));

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<ListViewItem>(item, out recycleKey);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new ListViewItem();
    }

    public ListViewStyle View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

}
