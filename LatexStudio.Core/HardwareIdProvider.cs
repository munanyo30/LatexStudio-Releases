using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace LatexStudio.Core;

public static class HardwareIdProvider
{
    public static string GetDeviceId()
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append(GetManagementProperty("Win32_Processor", "ProcessorId"));
            sb.Append(GetManagementProperty("Win32_BaseBoard", "SerialNumber"));
            
            var rawId = sb.ToString();
            if (string.IsNullOrWhiteSpace(rawId) || rawId.Contains("Unknown"))
            {
                // Fallback para o nome da máquina se WMI falhar totalmente
                rawId += Environment.MachineName + Environment.UserName;
            }

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
            return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16);
        }
        catch
        {
            return "DEFAULT-DEV-ID-01"; // Fallback absoluto para evitar crash
        }
    }

    private static string GetManagementProperty(string className, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var obj in searcher.Get())
            {
                return obj[propertyName]?.ToString() ?? "Unknown";
            }
        }
        catch
        {
            return "FallbackID";
        }
        return "Unknown";
    }
}
