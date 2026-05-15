using System.IO;
using System.Text.Json;

namespace LatexStudio.Services;

public sealed record LatexTemplate(string Name, string Description, string Body);

public sealed class TemplateService
{
    private readonly string templateDirectory;
    private List<LatexTemplate>? cachedTemplates;

    public TemplateService(string? templateDirectory = null)
    {
        this.templateDirectory = templateDirectory ?? Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    public IReadOnlyList<LatexTemplate> LoadTemplates()
    {
        if (cachedTemplates is not null) return cachedTemplates;

        if (!Directory.Exists(templateDirectory))
        {
            return cachedTemplates = [DefaultTemplate()];
        }

        cachedTemplates = Directory.EnumerateFiles(templateDirectory, "*.json")
            .Select(File.ReadAllText)
            .Select(json => JsonSerializer.Deserialize<LatexTemplate>(json, JsonOptions()))
            .Where(template => template is not null)
            .Cast<LatexTemplate>()
            .ToList();

        if (cachedTemplates.Count == 0) cachedTemplates.Add(DefaultTemplate());
        return cachedTemplates;
    }

    public static LatexTemplate DefaultTemplate() => new(
        "Artigo académico",
        "Documento simples com preâmbulo automático.",
        """
        \documentclass{article}
        {{packages}}
        \usepackage[a4paper,margin=2.5cm]{geometry}
        \begin{document}
        {{body}}
        \end{document}
        """);

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}
