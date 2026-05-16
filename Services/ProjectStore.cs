using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LatexStudio.Models;

namespace LatexStudio.Services;

public sealed class ProjectStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new DocumentElementJsonConverter() }
    };

    public void Save(AcademicDocument document, string path)
    {
        document.LastSavedAt = DateTime.Now;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Serialize(document));
    }

    public AcademicDocument Load(string path)
        => Deserialize(File.ReadAllText(path));

    public string Serialize(AcademicDocument document) => JsonSerializer.Serialize(document, Options);

    public AcademicDocument Deserialize(string json)
        => JsonSerializer.Deserialize<AcademicDocument>(json, Options) ?? new AcademicDocument();
}
