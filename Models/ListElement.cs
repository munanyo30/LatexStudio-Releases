using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ListElement : DocumentElement
{
    [ObservableProperty] private ListKind listKind = ListKind.Unordered;
    [ObservableProperty] private string bullet = "\\textbullet";
    [ObservableProperty] private double itemSpacing = 2;
    [ObservableProperty] private double indentation = 16;

    public ObservableCollection<ListItemNode> Items { get; } = [];

    public override DocumentElementKind Kind => DocumentElementKind.List;
    public override string DisplayName => $"Lista: {Title}";

    public static ListElement CreateExample() => new()
    {
        Title = "Contribuições",
        Caption = "Lista de contribuições principais.",
        Items =
        {
            new ListItemNode { Text = "Editor visual orientado a LaTeX" },
            new ListItemNode
            {
                Text = "Exportação académica",
                Children = { new ListItemNode { Text = "Referências automáticas" } }
            }
        }
    };
}
