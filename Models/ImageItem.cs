using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ImageItem : ObservableObject
{
    [ObservableProperty] private string path = "";
    [ObservableProperty] private string caption = "";
    [ObservableProperty] private double widthPercent = 0.45;
    [ObservableProperty] private string borderColor = "#FF111827";
    [ObservableProperty] private double borderThickness;
}
