using System.Security.Cryptography;
using System.Text;

namespace LatexStudio.Core;

public static class CryptoService
{
    public static (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (
            Convert.ToBase64String(rsa.ExportRSAPrivateKey()),
            Convert.ToBase64String(rsa.ExportRSAPublicKey())
        );
    }

    public static string Sign(string data, string privateKeyBase64)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
        
        var bytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public static bool Verify(string data, string signatureBase64, string publicKeyBase64)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}
