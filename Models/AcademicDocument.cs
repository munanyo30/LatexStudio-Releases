using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class AcademicDocument : ObservableObject
{
    [ObservableProperty] private string name = "Projeto académico";
    [ObservableProperty] private string author = "Autor";
    [ObservableProperty] private LatexEngineKind engine = LatexEngineKind.PdfLatex;
    [ObservableProperty] private string customEnginePath = "pdflatex";
    [ObservableProperty] private DateTime lastSavedAt = DateTime.Now;

    public ObservableCollection<DocumentElement> Elements { get; } = [];

    public static AcademicDocument CreateSample()
    {
        var document = new AcademicDocument();
        document.Elements.Add(TableElement.CreateExample());
        document.Elements.Add(ImageElement.CreateExample());
        document.Elements.Add(ListElement.CreateExample());
        document.Elements.Add(ChartElement.CreateExample());
        return document;
    }
}
