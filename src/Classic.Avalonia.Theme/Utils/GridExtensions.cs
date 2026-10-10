using Avalonia;
using Avalonia.Controls;

namespace Classic.Avalonia.Theme.Utils;

internal class GridExtensions
{
    public static readonly AvaloniaProperty<RowDefinitions?> RowDefinitionsExProperty = AvaloniaProperty.RegisterAttached<GridExtensions, Grid, RowDefinitions?>("RowDefinitionsEx");

    public static readonly AvaloniaProperty<ColumnDefinitions?> ColumnDefinitionsExProperty = AvaloniaProperty.RegisterAttached<GridExtensions, Grid, ColumnDefinitions?>("ColumnDefinitionsEx");

    public static RowDefinitions? GetRowDefinitionsEx(Grid obj)
    {
        return (RowDefinitions?)obj.GetValue(RowDefinitionsExProperty);
    }

    public static void SetRowDefinitionsEx(Grid obj, RowDefinitions? value)
    {
        obj.SetValue(RowDefinitionsExProperty, value);
    }

    public static ColumnDefinitions? GetColumnDefinitionsEx(Grid obj)
    {
        return (ColumnDefinitions?)obj.GetValue(ColumnDefinitionsExProperty);
    }

    public static void SetColumnDefinitionsEx(Grid obj, ColumnDefinitions? value)
    {
        obj.SetValue(ColumnDefinitionsExProperty, value);
    }

    static GridExtensions()
    {
        RowDefinitionsExProperty.Changed.AddClassHandler<Grid>((grid, e) =>
        {
            grid.RowDefinitions.Clear();
            // Style values are shared; each grid must own its mutable layout definitions.
            if (e.GetNewValue<RowDefinitions>() is { } rows)
                foreach (var row in rows)
                    grid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = row.Height, MinHeight = row.MinHeight, MaxHeight = row.MaxHeight,
                        SharedSizeGroup = row.SharedSizeGroup
                    });
            grid.InvalidateMeasure();
            grid.InvalidateArrange();
        });
        ColumnDefinitionsExProperty.Changed.AddClassHandler<Grid>((grid, e) =>
        {
            grid.ColumnDefinitions.Clear();
            if (e.GetNewValue<ColumnDefinitions>() is { } columns)
                foreach (var column in columns)
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = column.Width, MinWidth = column.MinWidth, MaxWidth = column.MaxWidth,
                        SharedSizeGroup = column.SharedSizeGroup
                    });
            grid.InvalidateMeasure();
            grid.InvalidateArrange();
        });
    }
}
