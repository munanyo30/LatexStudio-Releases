namespace LatexStudio.Core;

public static class LicenseService
{
    // Esta chave pública será embutida na aplicação.
    // O ideal é gerá-la uma vez e mantê-la constante.
    private const string DefaultPublicKey = "MIIBCgKCAQEA2/nIHIuxv3TnWUTxdss1WXGTQCCwUHy/mDK9Sr793ZlbcDKkwImuj8/zjdeblGvKgKnZInlUNeDQoqrglPG/FyCxPZQcpG49MnbOyBeqoQ2aWlkoycFvruYCa+bc+q2+sPrgcYnFZQqtc532+O0Hjr5QEte/HJoKLKKSAlCgBcC/D2m4vQfuS5UFEdKxmCGkzhFKXxH6kYJ0NlpGBQuz91YNzHJWpo7pyz1EuB6WhtZLZ4mAcXCjVBQ1LgS20iM1GKHGC+HRDjLZbryVOLxSQlf27oZZwM8bmRP/442IVcRuH51EhEW/wB8MQdxAhDOmWCkAJPW/CiCunciTNKVGZQIDAQAB";

    public static (bool IsValid, string Message) ValidateLicense(string licenseBase64, string publicKey)
    {
        try
        {
            // Limpar espaços e quebras de linha que o utilizador possa ter colado
            licenseBase64 = licenseBase64.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
            
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(licenseBase64));
            var license = LicenseModel.FromJson(json);
            if (license == null) return (false, "Formato de licença inválido.");

            // Guardar a assinatura e limpar o campo no objeto para reconstruir o JSON original
            var signatureToVerify = license.Signature;
            
            // Importante: Criamos um novo objeto com os mesmos dados para garantir que a ordem dos campos no JSON é a mesma da geração
            var cleanModel = new LicenseModel
            {
                ClientName = license.ClientName,
                DeviceId = license.DeviceId,
                Price = license.Price,
                ExpiryDate = license.ExpiryDate,
                Signature = "" // Deve estar vazio para validar o que foi assinado
            };
            
            var dataToVerify = cleanModel.ToJson();

            if (!CryptoService.Verify(dataToVerify, signatureToVerify, publicKey))
                return (false, "A assinatura digital não confere (Chave corrompida ou alterada).");

            var currentDeviceId = HardwareIdProvider.GetDeviceId();
            if (license.DeviceId != currentDeviceId)
                return (false, $"Esta licença pertence a outro computador.\nLicença: {license.DeviceId}\nEste PC: {currentDeviceId}");

            if (DateTime.TryParseExact(license.ExpiryDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var expiryDate))
            {
                if (DateTime.Now.Date > expiryDate)
                    return (false, $"A licença expirou em {expiryDate:dd/MM/yyyy}.");
            }
            else
            {
                return (false, "Formato de data de expiração inválido na licença.");
            }

            return (true, $"Licença válida para {license.ClientName}.");
        }
        catch (Exception ex)
        {
            return (false, "Erro técnico na validação: " + ex.Message);
        }
    }

    public static (bool IsValid, string Message) ValidateWithDefaultKey(string licenseBase64)
        => ValidateLicense(licenseBase64, DefaultPublicKey);

    public static string GetLicensePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "LatexStudio");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return Path.Combine(folder, "license.lic");
    }
}
