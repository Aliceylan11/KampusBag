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
            // SMTP ayarlarını appsettings.json'dan okuyoruz
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "KampusBag";

            // E-posta mesajını oluşturuyoruz
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

            // SMTP üzerinden e-postayı gönderiyoruz
            using var client = new SmtpClient();

            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            // Hata durumunda loglama yapılabilir
            Console.WriteLine($"E-posta gönderimi başarısız: {ex.Message}");
            return false;
        }
    }
}