using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class CustomCodeElement : DocumentElement
{
    [ObservableProperty] private string rawLatex = "% Insira o seu código LaTeX personalizado aqui";

    public override DocumentElementKind Kind => DocumentElementKind.CustomCode;
    public override string DisplayName => string.IsNullOrWhiteSpace(Title) ? "Código Manual" : Title;

    public static CustomCodeElement CreateExample()
    {
        return new CustomCodeElement
        {
            Title = "Snippet Personalizado",
            RawLatex = "\\vspace{2cm}\n\\begin{center}\n  \\textbf{Fim da Secção}\n\\end{center}"
        };
    }
}
