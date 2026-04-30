using KampusBag.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace KampusBag.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendVerificationCodeAsync(string email, string code)
    {
        try
        {
            Console.WriteLine("========== E-POSTA GÖNDERİM İŞLEMİ BAŞLADI ==========");

            // SMTP ayarlarını appsettings.json'dan okuyoruz
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "KampusBag";

            Console.WriteLine($"📧 Alıcı: {email}");
            Console.WriteLine($"📤 Gönderici: {senderEmail}");
            Console.WriteLine($"🌐 SMTP Sunucu: {smtpHost}:{smtpPort}");
            Console.WriteLine($"🔑 Doğrulama Kodu: {code}");

            // E-posta mesajını oluşturuyoruz
            Console.WriteLine("📝 E-posta mesajı oluşturuluyor...");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "KampusBag - Email Doğrulama Kodu";

            // HTML formatında e-posta içeriği
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #1B305E;'>KampusBag Email Doğrulama</h2>
                            <p>Merhaba,</p>
                            <p>KampusBag hesabınızı doğrulamak için aşağıdaki 6 haneli kodu kullanın:</p>
                            <div style='background-color: #f0f0f0; padding: 20px; text-align: center; margin: 20px 0;'>
                                <h1 style='color: #1B305E; letter-spacing: 5px; margin: 0;'>{code}</h1>
                            </div>
                            <p>Bu kod 15 dakika süreyle geçerlidir.</p>
                            <p>Eğer bu hesabı siz oluşturmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                            <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                            <p style='color: #888; font-size: 12px;'>Bu otomatik bir e-postadır, lütfen yanıtlamayın.</p>
                        </div>
                    </body>
                    </html>
                "
            };

            message.Body = bodyBuilder.ToMessageBody();
            Console.WriteLine("✅ E-posta mesajı hazırlandı");

            // SMTP üzerinden e-postayı gönderiyoruz
            using var client = new SmtpClient();

            Console.WriteLine("🔌 SMTP sunucusuna bağlanılıyor...");
            // DEĞİŞİKLİK: SecureSocketOptions.Auto deneniyor
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.Auto);
            Console.WriteLine("✅ SMTP sunucusuna bağlantı başarılı");

            Console.WriteLine("🔐 Kimlik doğrulaması yapılıyor...");
            await client.AuthenticateAsync(senderEmail, senderPassword);
            Console.WriteLine("✅ Kimlik doğrulama başarılı");

            Console.WriteLine("📨 E-posta gönderiliyor...");
            await client.SendAsync(message);
            Console.WriteLine("✅ E-posta başarıyla gönderildi!");

            await client.DisconnectAsync(true);
            Console.WriteLine("🔌 SMTP bağlantısı kapatıldı");

            Console.WriteLine("========== E-POSTA GÖNDERİM İŞLEMİ TAMAMLANDI ==========\n");
            return true;
        }
        catch (Exception ex)
        {
            // DEĞİŞİKLİK: Artık sessizce false dönmüyor, hatayı fırlatıyor!
            Console.WriteLine("❌ HATA OLUŞTU!");
            Console.WriteLine($"Hata Tipi: {ex.GetType().Name}");
            Console.WriteLine($"Hata Mesajı: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine("========== E-POSTA GÖNDERİM İŞLEMİ BAŞARISIZ ==========\n");

            // Hatayı yukarı fırlat ki Controller'da görünsün
            throw new Exception($"E-posta gönderimi başarısız: {ex.Message}", ex);
        }
    }

    // EKLEME YAPILACAK METOD - EmailService.cs dosyasına ekleyin

    public async Task<bool> SendPasswordResetCodeAsync(string email, string code)
    {
        try
        {
            Console.WriteLine("========== ŞİFRE SIFIRLAMA KODU GÖNDERİLİYOR ==========");

            // SMTP ayarlarını appsettings.json'dan okuyoruz
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "KampusBag";

            Console.WriteLine($"📧 Alıcı: {email}");
            Console.WriteLine($"🔑 Sıfırlama Kodu: {code}");

            // E-posta mesajını oluşturuyoruz
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "KampusBag - Şifre Sıfırlama Kodu";

            // HTML formatında e-posta içeriği
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #1B305E;'>🔒 Şifre Sıfırlama Talebi</h2>
                        <p>Merhaba,</p>
                        <p>KampusBag hesabınız için şifre sıfırlama talebinde bulundunuz. Yeni şifre oluşturmak için aşağıdaki 6 haneli kodu kullanın:</p>
                        <div style='background-color: #f0f0f0; padding: 20px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #D32F2F; letter-spacing: 5px; margin: 0;'>{code}</h1>
                        </div>
                        <p><strong>⚠️ Önemli Güvenlik Uyarısı:</strong></p>
                        <ul>
                            <li>Bu kod 15 dakika süreyle geçerlidir</li>
                            <li>Kodu kimseyle paylaşmayın</li>
                            <li>Bu talebi siz yapmadıysanız, hesabınız risk altında olabilir</li>
                        </ul>
                        <p>Eğer bu talebi siz yapmadıysanız, lütfen hemen şifrenizi değiştirin ve bu e-postayı görmezden gelin.</p>
                        <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #888; font-size: 12px;'>Bu otomatik bir e-postadır, lütfen yanıtlamayın.</p>
                        <p style='color: #888; font-size: 12px;'>KampusBag Güvenlik Ekibi</p>
                    </div>
                </body>
                </html>
            "
            };

            message.Body = bodyBuilder.ToMessageBody();

            // SMTP üzerinden e-postayı gönderiyoruz
            using var client = new SmtpClient();

            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine("✅ Şifre sıfırlama kodu başarıyla gönderildi!");
            Console.WriteLine("========== İŞLEM TAMAMLANDI ==========\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Şifre sıfırlama e-postası gönderilemedi: {ex.Message}");
            throw new Exception($"E-posta gönderimi başarısız: {ex.Message}", ex);
        }
    }

  
}