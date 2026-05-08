using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Infrastructure.Helpers;

public static class EncryptionHelper
{
    // !! UYARI: Bu anahtar KampusBag.MobileUI/Services/EncryptionService.cs
    // içindeki Key sabiti ile BİREBİR AYNI olmalıdır.
    // Değiştirirseniz her iki dosyayı da aynı anda güncelleyin.
    private const string Key = "KampusBag@2025!SecureAES256Key#1"; // 32 karakter = AES-256

    public static string Encrypt(string plainText)
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

    public static string Decrypt(string cipherText)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);

        byte[] iv = new byte[16];
        byte[] actualCipher = new byte[fullCipher.Length - 16];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
        Buffer.BlockCopy(fullCipher, 16, actualCipher, 0, actualCipher.Length);

        aes.IV = iv;
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream ms = new(actualCipher);
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new(cs);

        return sr.ReadToEnd();
    }
}
