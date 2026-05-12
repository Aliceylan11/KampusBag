namespace KampusBag.Core.Options;

/// <summary>JWT yapılandırması — appsettings.json "Jwt" bölümünden gelir.</summary>
public class JwtOptions
{
    public string Key { get; set; } = string.Empty; // En az 32 karakter
    public string Issuer { get; set; } = "KampusBag";
    public string Audience { get; set; } = "KampusBagUsers";
    public int ExpiryDays { get; set; } = 7;
}

/// <summary>SMTP e-posta yapılandırması — appsettings.json "EmailSettings" bölümünden gelir.</summary>
public class EmailOptions
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty; // user-secrets ile gelir
    public string SenderName { get; set; } = "KampusBag Platform";
}

/// <summary>AES şifreleme anahtarı — user-secrets ile gelir.</summary>
public class EncryptionOptions
{
    public string Key { get; set; } = string.Empty; // 32 karakter, user-secrets'ten
}
