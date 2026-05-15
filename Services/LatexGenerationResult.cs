namespace LatexStudio.Services;

public sealed record LatexGenerationResult(string Code, IReadOnlyCollection<string> Packages);
