using Microsoft.AspNetCore.SignalR.Client;
using KampusBag.MobileUI.Models;

namespace KampusBag.MobileUI.Services;

/// <summary>
/// SignalR Hub bağlantısını yöneten servis.
/// Tek bağlantı tüm uygulama boyunca yaşar; sayfa geçişlerinde yeniden oluşturulmaz.
/// Yeni bir sohbet odasına girildiğinde JoinXxx, çıkıldığında LeaveXxx çağrılır.
/// </summary>
public class SignalRService
{
    private HubConnection? _connection;
    private readonly EncryptionService _encryption = new();

    // ── Hub URL ───────────────────────────────────────────────────────
#if ANDROID
    private const string HubUrl = "https://resistant-sacred-exes.ngrok-free.dev/hubs/chat";
#else
    // Laptop için de aynı ngrok adresi
    private const string HubUrl = "https://resistant-sacred-exes.ngrok-free.dev/hubs/chat";
#endif

    // ── Durum ─────────────────────────────────────────────────────────
    public bool IsConnected
        => _connection?.State == HubConnectionState.Connected;

    /// <summary>Yeni mesaj geldiğinde tetiklenir. UI thread'e geçiş gerekir.</summary>
    public event Action<MessageModel>? MessageReceived;
    // ════════════════════════════════════════════════════════════════
    // BAŞLAT
    // ════════════════════════════════════════════════════════════════
    public async Task StartAsync(Guid userId)
    {
        // Zaten bağlıysa tekrar bağlanma
        if (_connection != null &&
            _connection.State != HubConnectionState.Disconnected)
            return;

        // Ngrok statik URL'nizi buraya tanımlıyoruz (HubUrl değişkenini de buna göre güncellediğinizden emin olun)
        var finalHubUrl = "https://resistant-sacred-exes.ngrok-free.dev/hubs/chat";

        _connection = new HubConnectionBuilder()
            .WithUrl($"{finalHubUrl}?userId={userId}", options =>
            {
                // Ngrok'un ücretsiz planındaki "browser warning" sayfasını atlamak için ŞART:
                options.Headers.Add("ngrok-skip-browser-warning", "69420");
            })
            .AddJsonProtocol(options =>
            {
                // Sunucu PascalCase gönderir; bu ayar MAUI model sınıflarıyla eşleştirir
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .WithAutomaticReconnect(new[]
            {
            // İlk kopuşta hemen, sonra artan aralıklarla yeniden bağlan
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
            })
            .Build();

        // ── Mesaj dinleyici ───────────────────────────────────────────
        _connection.On<MessageModel>("ReceiveMessage", message =>
        {
            // Yeni mesaj geldiğinde UI thread'e haber ver
            MessageReceived?.Invoke(message);
        });

        // ── Bağlantı olayları ─────────────────────────────────────────
        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"[SignalR] Yeniden bağlanıyor… {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"[SignalR] Bağlandı: {connectionId}");
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            Console.WriteLine($"[SignalR] Bağlantı kapandı: {error?.Message}");
            return Task.CompletedTask;
        };

        // ── Bağlan ───────────────────────────────────────────────────
        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[SignalR] Ngrok üzerinden bağlantı kuruldu.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Başlatılamadı: {ex.Message}");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // ODA YÖNETİMİ
    // ════════════════════════════════════════════════════════════════

    public async Task JoinCourseRoomAsync(Guid courseId)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("JoinCourseRoom", courseId.ToString());
    }

    public async Task LeaveCourseRoomAsync(Guid courseId)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync("LeaveCourseRoom", courseId.ToString());
    }

    public async Task JoinPrivateRoomAsync(Guid userId, Guid otherUserId)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync(
            "JoinPrivateRoom", userId.ToString(), otherUserId.ToString());
    }

    public async Task LeavePrivateRoomAsync(Guid userId, Guid otherUserId)
    {
        if (!IsConnected) return;
        await _connection!.InvokeAsync(
            "LeavePrivateRoom", userId.ToString(), otherUserId.ToString());
    }

    // ════════════════════════════════════════════════════════════════
    // DURDUR
    // ════════════════════════════════════════════════════════════════
    public async Task StopAsync()
    {
        if (_connection == null) return;

        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;

        Console.WriteLine("[SignalR] Bağlantı kapatıldı.");
    }
}
