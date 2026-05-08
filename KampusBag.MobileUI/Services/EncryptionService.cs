using System.Security.Cryptography;
using System.Text;

namespace KampusBag.MobileUI.Services;

/// <summary>
/// Backend'deki EncryptionHelper ile birebir uyumlu.
/// Yapı: [16 byte IV] + [Şifreli Metin] → Base64
///
/// !! UYARI: Key sabiti KampusBag.Infrastructure/Helpers/EncryptionHelper.cs
/// içindeki Key ile BİREBİR AYNI olmalıdır.
/// </summary>
public class EncryptionService
{
    // Backend ile aynı sabit anahtar (32 karakter = AES-256)
    private const string Key = "KampusBag@2025!SecureAES256Key#1"; // 32 karakter = AES-256

    public string Decrypt(string base64CipherText)
    {
        try
        {
            byte[] fullCipher = Convert.FromBase64String(base64CipherText);

            byte[] iv = new byte[16];
            byte[] actualCipher = new byte[fullCipher.Length - 16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, actualCipher, 0, actualCipher.Length);

            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(actualCipher);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // Eski şifresiz kayıtlar veya bozuk veri → ham metni döndür
            return base64CipherText;
        }
    }

    public string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.GenerateIV();
        byte[] iv = aes.IV;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);

        using MemoryStream ms = new();
        ms.Write(iv, 0, iv.Length); // İlk 16 byte = IV

        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        using (StreamWriter sw = new(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}
