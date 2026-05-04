using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Infrastructure.Helpers;

public static class EncryptionHelper
{
    // Key sabit kalabilir (32 karakter)
    private static readonly string Key = "nGaklMaEenLAmNLyaelm193414785051"; 

    public static string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);

        // ARTIK HER SEFERİNDE RASTGELE IV ÜRETİLİYOR
        aes.GenerateIV();
        byte[] iv = aes.IV;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);

        using MemoryStream ms = new();

        // 🚩 IV'yi şifreli metnin en başına ekliyoruz ki çözerken okuyabilelim
        ms.Write(iv, 0, iv.Length);

        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        {
            using (StreamWriter sw = new(cs))
            {
                sw.Write(plainText);
            }
        }

        // Sonuç: [16 byte IV] + [Şifreli Veri]
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);

        // Şifreli metnin başındaki ilk 16 byte'ı IV olarak alıyoruz
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