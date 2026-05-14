using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KampusBag.Infrastructure.Services;

/// <summary>
/// Firebase Cloud Messaging üzerinden push notification gönderir.
///
/// Kurulum:
///   1. NuGet: FirebaseAdmin (en güncel sürüm, 3.x)
///   2. Firebase Console → Proje Ayarları → Hizmet Hesapları → "Yeni özel anahtar oluştur"
///   3. İndirilen JSON dosyasını WebAPI projesine koy (örn. kampusbag-firebase.json)
///   4. .gitignore'a ekle — bu dosya GitHub'a gitmemeli
///   5. user-secrets: dotnet user-secrets set "Firebase:ServiceAccountPath" "C:\path\to\file.json"
/// </summary>
public class FirebaseNotificationService : INotificationService
{
    private readonly KampusBagDbContext _context;
    private readonly ILogger<FirebaseNotificationService> _logger;

    private static bool _initialized = false;
    private static readonly object _lock = new();

    public FirebaseNotificationService(
        IConfiguration configuration,
        KampusBagDbContext context,
        ILogger<FirebaseNotificationService> logger)
    {
        _context = context;
        _logger = logger;
        EnsureInitialized(configuration);
    }

    // ════════════════════════════════════════════════════════════════
    // FIREBASE TEK SEFER BAŞLATMA
    // ════════════════════════════════════════════════════════════════
    private static void EnsureInitialized(IConfiguration config)
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            // 1. Dosya yolunu doğrudan senin bilgisayarındaki yola sabitliyoruz (En garantisi budur)
            var path = @"C:\Users\ali_c\Desktop\KampusBag\KampusBag.WebAPI\Secrets\kampusbag-firebase.json";

            try
            {
                if (File.Exists(path))
                {
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(path)
                        });
                    }
                    _initialized = true;
                    Console.WriteLine("✅ Firebase başarıyla başlatıldı.");
                }
                else
                {
                    // DOSYA YOKSA HATA FIRLATMA, SADECE LOG YAZ
                    // Böylece uygulama açılır, Swagger gelir, her şey çalışır.
                    Console.WriteLine("⚠️ Firebase dosyası bulunamadı: " + path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Firebase başlatma hatası: " + ex.Message);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TEK KULLANICIYA BİLDİRİM
    // ════════════════════════════════════════════════════════════════
    public async Task<bool> SendAsync(
        Guid receiverId,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var deviceToken = await _context.DeviceTokens
                .Where(d => d.UserId == receiverId)
                .OrderByDescending(d => d.UpdatedAt)
                .FirstOrDefaultAsync();

            if (deviceToken == null || string.IsNullOrWhiteSpace(deviceToken.Token))
            {
                _logger.LogInformation("Kullanıcı için cihaz token'ı yok: {UserId}", receiverId);
                return false;
            }

            var message = BuildMessage(deviceToken.Token, title, body, data);
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation("Bildirim gönderildi → {UserId} ({MsgId})", receiverId, response);
            return true;
        }
        catch (FirebaseMessagingException ex)
            when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered
               || ex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
        {
            // Token artık geçersiz, DB'den temizle
            await RemoveStaleTokensAsync(receiverId);
            _logger.LogWarning("Geçersiz FCM token temizlendi: {UserId}", receiverId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim gönderilemedi: {UserId}", receiverId);
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // GRUP BİLDİRİMİ (Multicast)
    // ════════════════════════════════════════════════════════════════
    public async Task<int> SendToGroupAsync(
        List<Guid> userIds,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        if (userIds == null || userIds.Count == 0) return 0;

        try
        {
            // Bu kullanıcıların kayıtlı tüm token'larını al
            var tokens = await _context.DeviceTokens
                .Where(d => userIds.Contains(d.UserId))
                .Select(d => d.Token)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToListAsync();

            if (tokens.Count == 0)
            {
                _logger.LogInformation("Grup için hiçbir cihaz token'ı yok.");
                return 0;
            }

            // FCM batch sınırı: tek seferde max 500 token
            const int batchSize = 500;
            int totalSuccess = 0;

            for (int i = 0; i < tokens.Count; i += batchSize)
            {
                var batch = tokens.Skip(i).Take(batchSize).ToList();

                var multicast = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification { Title = title, Body = body },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = BuildAndroidConfig(),
                    Apns = BuildApnsConfig()
                };

                var response = await FirebaseMessaging.DefaultInstance
                    .SendEachForMulticastAsync(multicast);

                totalSuccess += response.SuccessCount;

                _logger.LogInformation(
                    "Multicast: {Success}/{Total} başarılı (failure: {Failure})",
                    response.SuccessCount, batch.Count, response.FailureCount);
            }

            return totalSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grup bildirimi başarısız.");
            return 0;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // YARDIMCILAR
    // ════════════════════════════════════════════════════════════════
    private static Message BuildMessage(
        string token, string title, string body, Dictionary<string, string>? data)
    {
        return new Message
        {
            Token = token,
            Notification = new Notification { Title = title, Body = body },
            Data = data ?? new Dictionary<string, string>(),
            Android = BuildAndroidConfig(),
            Apns = BuildApnsConfig()
        };
    }

    private static AndroidConfig BuildAndroidConfig() => new()
    {
        Priority = Priority.High,
        Notification = new AndroidNotification
        {
            Sound = "default",
            ChannelId = "kampusbag_messages",
            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
        }
    };

    private static ApnsConfig BuildApnsConfig() => new()
    {
        Aps = new Aps
        {
            Sound = "default",
            Badge = 1,
            ContentAvailable = true
        }
    };

    private async Task RemoveStaleTokensAsync(Guid userId)
    {
        var tokens = await _context.DeviceTokens
            .Where(d => d.UserId == userId)
            .ToListAsync();

        if (tokens.Any())
        {
            _context.DeviceTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
    }
}