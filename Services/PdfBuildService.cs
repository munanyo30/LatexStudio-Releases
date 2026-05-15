using System.Diagnostics;
using System.IO;
using LatexStudio.Models;

namespace LatexStudio.Services;

public sealed class PdfBuildService
{
    public async Task<string> BuildAsync(AcademicDocument document, string texPath, CancellationToken cancellationToken = default)
    {
        var engine = document.Engine == LatexEngineKind.Custom
            ? document.CustomEnginePath
            : document.Engine.ToString().ToLowerInvariant();

        var startInfo = new ProcessStartInfo
        {
            FileName = engine,
            Arguments = $"-interaction=nonstopmode \"{Path.GetFileName(texPath)}\"",
            WorkingDirectory = Path.GetDirectoryName(texPath) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Não foi possível iniciar o motor LaTeX.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(output + Environment.NewLine + error);
        }

        return Path.ChangeExtension(texPath, ".pdf");
    }
}
