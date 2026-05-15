using LatexStudio.Models;

namespace LatexStudio.Services;

public sealed class ReferenceRegistry
{
    private int tableCount;
    private int figureCount;
    private int chartCount;
    private readonly Dictionary<DocumentElement, ReferenceInfo> references = [];

    public IReadOnlyDictionary<DocumentElement, ReferenceInfo> References => references;

    public ReferenceInfo Assign(DocumentElement element)
    {
        if (references.TryGetValue(element, out var existing))
        {
            return existing;
        }

        var info = element.Kind switch
        {
            DocumentElementKind.Table => Create("tab", ++tableCount, "Tabela"),
            DocumentElementKind.Image => Create("fig", ++figureCount, "Figura"),
            DocumentElementKind.Chart => Create("gra", ++chartCount, "Gráfico"),
            _ => Create("lst", references.Count + 1, "Lista")
        };

        if (element.Label != info.Label)
        {
            element.Label = info.Label;
        }
        references[element] = info;
        return info;
    }

    private static ReferenceInfo Create(string prefix, int number, string name)
        => new(prefix, number, $"{prefix}:{number}", $"{name} {number}");
}
