using System.Text.Json;
using System.Text.Json.Serialization;

namespace LatexStudio.Core;

public class LicenseModel
{
    public string ClientName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Price { get; set; } = "0.00";
    public string ExpiryDate { get; set; } = ""; // Formato: yyyy-MM-dd
    public string Signature { get; set; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    public static LicenseModel? FromJson(string json) => JsonSerializer.Deserialize<LicenseModel>(json, JsonOptions);
}
