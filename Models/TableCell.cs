using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class TableCell : ObservableObject
{
    [ObservableProperty] private string text = "";
    [ObservableProperty] private int rowSpan = 1;
    [ObservableProperty] private int columnSpan = 1;
    [ObservableProperty] private int rowIndex;
    [ObservableProperty] private int columnIndex;
    [ObservableProperty] private bool isMergedChild;
    [ObservableProperty] private HorizontalCellAlignment horizontalAlignment = HorizontalCellAlignment.Center;
    [ObservableProperty] private VerticalCellAlignment verticalAlignment = VerticalCellAlignment.Middle;
    [ObservableProperty] private string background = "#FFFFFFFF";
    [ObservableProperty] private string foreground = "#FF111827";
    [ObservableProperty] private string borderColor = "#FF111827";
    [ObservableProperty] private double borderThickness = 0.8;
    [ObservableProperty] private double padding = 6;
    [ObservableProperty] private string fontFamily = "Segoe UI";
    [ObservableProperty] private double fontSize = 12;
    [ObservableProperty] private bool isHeader;

    public TableCell Clone() => new()
    {
        Text = Text,
        RowSpan = RowSpan,
        ColumnSpan = ColumnSpan,
        IsMergedChild = IsMergedChild,
        HorizontalAlignment = HorizontalAlignment,
        VerticalAlignment = VerticalAlignment,
        Background = Background,
        Foreground = Foreground,
        BorderColor = BorderColor,
        BorderThickness = BorderThickness,
        Padding = Padding,
        FontFamily = FontFamily,
        FontSize = FontSize,
        IsHeader = IsHeader
    };
}
