using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class AcademicDocument : ObservableObject
{
    [ObservableProperty] private string name = "Projeto académico";
    [ObservableProperty] private string author = "Autor";
    [ObservableProperty] private string institution = "Universidade Exemplo";
    [ObservableProperty] private string advisor = "";
    [ObservableProperty] private string coAdvisor = "";
    [ObservableProperty] private string abstractText = "";
    [ObservableProperty] private string keywords = "";
    [ObservableProperty] private bool includeTableOfContents = true;
    [ObservableProperty] private bool includeListOfFigures;
    [ObservableProperty] private bool includeListOfTables;
    [ObservableProperty] private LatexEngineKind engine = LatexEngineKind.PdfLatex;
    [ObservableProperty] private string customEnginePath = "pdflatex";
    [ObservableProperty] private DateTime lastSavedAt = DateTime.Now;

    public ObservableCollection<DocumentElement> Elements { get; } = [];

    public static AcademicDocument CreateSample()
    {
        var document = new AcademicDocument();
        document.Elements.Add(TextElement.CreateExample());
        document.Elements.Add(TheoremElement.CreateExample());
        document.Elements.Add(TableElement.CreateExample());
        document.Elements.Add(CodeElement.CreateExample());
        document.Elements.Add(ImageElement.CreateExample());
        document.Elements.Add(ListElement.CreateExample());
        document.Elements.Add(ChartElement.CreateExample());
        document.Elements.Add(BibliographyElement.CreateExample());
        return document;
    }
}
