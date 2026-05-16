using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ImageElement : DocumentElement
{
    [ObservableProperty] private ImageLayoutMode layoutMode = ImageLayoutMode.Single;
    [ObservableProperty] private double spacing = 8;
    [ObservableProperty] private HorizontalCellAlignment alignment = HorizontalCellAlignment.Center;
    [ObservableProperty] private bool useSubfigures = true;

    public ObservableCollection<ImageItem> Images { get; } = [];

    public override DocumentElementKind Kind => DocumentElementKind.Image;
    public override string DisplayName => $"Figura: {Title}";

    public static ImageElement CreateExample() => new()
    {
        Title = "Arquitetura",
        Caption = "Exemplo de layout de figura.",
        Images =
        {
            new ImageItem { Path = "assets/figura-a.pdf", Caption = "Módulo A" },
            new ImageItem { Path = "assets/figura-b.pdf", Caption = "Módulo B" }
        },
        LayoutMode = ImageLayoutMode.Grid2x1
    };
}
