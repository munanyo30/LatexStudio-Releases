using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class TextElement : DocumentElement
{
    [ObservableProperty] private string content = "";
    [ObservableProperty] private SectionLevel level = SectionLevel.Paragraph;
    [ObservableProperty] private TextAlignment alignment = TextAlignment.Justify;
    [ObservableProperty] private bool isBold;
    [ObservableProperty] private bool isItalic;

    public override DocumentElementKind Kind => DocumentElementKind.Text;
    public override string DisplayName => string.IsNullOrWhiteSpace(Title) ? (Level == SectionLevel.Paragraph ? "Parágrafo" : Level.ToString()) : Title;

    public static TextElement CreateExample()
    {
        return new TextElement
        {
            Title = "Introdução",
            Content = "Este é um parágrafo de exemplo no Latex Studio. Pode conter texto formatado e ser organizado em secções.",
            Level = SectionLevel.Section
        };
    }
}
