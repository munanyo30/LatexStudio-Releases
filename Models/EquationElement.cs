using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class EquationElement : DocumentElement
{
    [ObservableProperty] private string formula = "E = mc^2";
    [ObservableProperty] private bool isNumbered = true;

    public override DocumentElementKind Kind => DocumentElementKind.Equation;
    public override string DisplayName => string.IsNullOrWhiteSpace(Title) ? "Equação" : Title;

    public static EquationElement CreateExample()
    {
        return new EquationElement
        {
            Title = "Equação de Einstein",
            Formula = "E = mc^2",
            IsNumbered = true
        };
    }
}
