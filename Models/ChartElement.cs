using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ChartElement : DocumentElement
{
    [ObservableProperty] private ChartKind chartKind = ChartKind.Bar;
    [ObservableProperty] private string xLabel = "X";
    [ObservableProperty] private string yLabel = "Y";
    [ObservableProperty] private bool showGrid = true;
    [ObservableProperty] private bool showLegend = true;
    [ObservableProperty] private string sourceTableLabel = "";

    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<ChartSeries> Series { get; } = [];

    public string CategoriesRaw
    {
        get => string.Join(", ", Categories);
        set
        {
            var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            Categories.Clear();
            foreach (var p in parts)
            {
                Categories.Add(p.Trim());
            }
            OnPropertyChanged(nameof(CategoriesRaw));
        }
    }

    public override DocumentElementKind Kind => DocumentElementKind.Chart;
    public override string DisplayName => $"Gráfico: {Title}";

    public static ChartElement CreateExample() => new()
    {
        Title = "Comparação",
        Caption = "Comparação visual entre métodos.",
        XLabel = "Método",
        YLabel = "Precisão",
        Categories = { "Baseline", "Proposto" },
        Series = { new ChartSeries { Name = "Precisão", Color = "teal", Values = { 82.1, 91.6 } } }
    };
}
