using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class TableRow : ObservableObject
{
    [ObservableProperty] private double height = 34;
    public ObservableCollection<TableCell> Cells { get; } = [];
}
