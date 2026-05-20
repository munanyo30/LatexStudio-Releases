using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class ChartSeries : ObservableObject
{
    [ObservableProperty] private string name = "Série";
    [ObservableProperty] private string color = "blue";
    public ObservableCollection<double> Values { get; } = [];

    public string ValuesRaw
    {
        get => string.Join(", ", Values);
        set
        {
            var parts = value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Values.Clear();
            foreach (var p in parts)
            {
                if (double.TryParse(p.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    Values.Add(v);
            }
            OnPropertyChanged(nameof(ValuesRaw));
        }
    }
}
