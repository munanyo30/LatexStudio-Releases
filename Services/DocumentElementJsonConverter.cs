using System.Text.Json;
using System.Text.Json.Serialization;
using LatexStudio.Models;

namespace LatexStudio.Services;

public sealed class DocumentElementJsonConverter : JsonConverter<DocumentElement>
{
    public override DocumentElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty(nameof(DocumentElement.Kind), out var kindProperty))
        {
            return null;
        }

        var json = document.RootElement.GetRawText();
        return Enum.Parse<DocumentElementKind>(kindProperty.GetString() ?? "Table") switch
        {
            DocumentElementKind.Table => JsonSerializer.Deserialize<TableElement>(json, options),
            DocumentElementKind.Image => JsonSerializer.Deserialize<ImageElement>(json, options),
            DocumentElementKind.List => JsonSerializer.Deserialize<ListElement>(json, options),
            DocumentElementKind.Chart => JsonSerializer.Deserialize<ChartElement>(json, options),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, DocumentElement value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
