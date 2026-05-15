using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ChartSeries : ObservableObject
{
    [ObservableProperty] private string name = "Série";
    [ObservableProperty] private string color = "blue";
    public ObservableCollection<double> Values { get; } = [];
}
