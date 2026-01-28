using System.Security.Cryptography;
using System.Text;

namespace AIDataQuery.API.Infrastructure.Encryption;

public class AesEncryptor : IAesEncryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptor(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured");

        // Ensure key is 32 bytes for AES-256
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));

        // Use fixed IV for simplicity (in production, consider using random IV stored with ciphertext)
        _iv = Encoding.UTF8.GetBytes("AIDataQuery_IV16");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        // Check if already encrypted (Base64 pattern)
        if (IsBase64String(plainText) && plainText.Length > 50)
        {
            return plainText; // Already encrypted
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        // Check if it looks like a connection string (not encrypted)
        if (cipherText.Contains("Server=") || cipherText.Contains("Data Source="))
        {
            return cipherText; // Not encrypted, return as-is
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // If decryption fails, assume it's not encrypted
            return cipherText;
        }
    }

    private static bool IsBase64String(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length % 4 != 0)
            return false;

        try
        {
            Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
