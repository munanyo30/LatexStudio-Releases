using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class TheoremElement : DocumentElement
{
    [ObservableProperty] private string content = "O conteúdo do teorema...";
    [ObservableProperty] private TheoremKind theoremKind = TheoremKind.Theorem;
    [ObservableProperty] private string subtitle = ""; // ex: (Teorema de Pitágoras)

    public override DocumentElementKind Kind => DocumentElementKind.Theorem;
    public override string DisplayName => string.IsNullOrWhiteSpace(Title) ? TheoremKind.ToString() : Title;

    public static TheoremElement CreateExample()
    {
        return new TheoremElement
        {
            TheoremKind = TheoremKind.Theorem,
            Subtitle = "Pitágoras",
            Content = "Em qualquer triângulo retângulo, o quadrado da hipotenusa é igual à soma dos quadrados dos catetos."
        };
    }
}
