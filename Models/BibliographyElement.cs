using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class BibliographyElement : DocumentElement
{
    [ObservableProperty] private string bibContent = "@article{exemplo2026,\n  author = {Autor, Exemplo},\n  title = {Título do Artigo},\n  journal = {Revista de Exemplo},\n  year = {2026}\n}";
    [ObservableProperty] private string style = "plain";

    public override DocumentElementKind Kind => DocumentElementKind.Bibliography;
    public override string DisplayName => "Bibliografia";

    public static BibliographyElement CreateExample()
    {
        return new BibliographyElement
        {
            Title = "Referências Bibliográficas",
            BibContent = "@book{latexcompanion,\n  author = {Frank Mittelbach and Michel Goossens},\n  title = {The LaTeX Companion},\n  year = {2004},\n  publisher = {Addison-Wesley}\n}"
        };
    }
}
