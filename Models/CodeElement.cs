using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexStudio.Models;

public partial class CodeElement : DocumentElement
{
    [ObservableProperty] private string code = "// Escreva o seu código aqui";
    [ObservableProperty] private string language = "C++";
    [ObservableProperty] private bool showLineNumbers = true;
    [ObservableProperty] private bool breakLines = true;

    public override DocumentElementKind Kind => DocumentElementKind.Code;
    public override string DisplayName => string.IsNullOrWhiteSpace(Title) ? $"Código ({Language})" : Title;

    public static CodeElement CreateExample()
    {
        return new CodeElement
        {
            Title = "Exemplo de Algoritmo",
            Language = "Python",
            Code = "def hello_world():\n    print(\"Olá do Latex Studio!\")\n\nhello_world()"
        };
    }
}
