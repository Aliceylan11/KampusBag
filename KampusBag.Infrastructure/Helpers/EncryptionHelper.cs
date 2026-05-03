using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Infrastructure.Helpers;

public static class EncryptionHelper
{
    // Bu anahtarlar tam olarak 32 ve 16 karakter olmalı (AES-256 kuralı)
    private static readonly string Key = "g6f3k9l2m5n8b1v4c7x0zQWERT123456"; // 32 chars
    private static readonly string IV = "a1b2c3d4e5f6g7h8";                  // 16 chars

    public static string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(IV);

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new();

        // DÜZELTME: CryptoStream ve StreamWriter ayrı using blokları içinde
        // sw önce kapanır → cs.FlushFinalBlock() tetiklenir → ms hâlâ açık
        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        {
            using (StreamWriter sw = new(cs))   // ← cs'ye yaz, ms'ye değil
            {
                sw.Write(plainText);
            }
            // sw kapanınca cs.FlushFinalBlock() çağrılır, şifrelenmiş veri ms'ye yazılır
        }

        // ms burada hâlâ açık, ToArray() güvenle çalışır
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(IV);

        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using MemoryStream ms = new(Convert.FromBase64String(cipherText));
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new(cs);

        return sr.ReadToEnd();
    }
}