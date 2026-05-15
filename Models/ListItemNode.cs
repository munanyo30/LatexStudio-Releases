using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ListItemNode : ObservableObject
{
    [ObservableProperty] private string text = "";
    [ObservableProperty] private bool isChecked;
    public ObservableCollection<ListItemNode> Children { get; } = [];
}
