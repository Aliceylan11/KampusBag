using Microsoft.AspNetCore.SignalR.Client;
using KampusBag.MobileUI.Models;

namespace KampusBag.MobileUI.Services;

/// <summary>
/// SignalR Hub bağlantısını yöneten servis.
/// Tek bağlantı tüm uygulama boyunca yaşar.
/// </summary>
public class SignalRService
{
    private HubConnection? _connection;
    private readonly EncryptionService _encryption = new();

#if ANDROID
    // Fiziksel cihazlar ve ngrok için tam adres:
    private const string BaseUrl = "https://resistant-sacred-exes.ngrok-free.dev/hubs/chat";
#else
    private const string BaseUrl = "https://resistant-sacred-exes.ngrok-free.dev/hubs/chat";
#endif
    public bool IsConnected
        => _connection?.State == HubConnectionState.Connected;

    public event Action<MessageModel>? MessageReceived;

    // ════════════════════════════════════════════════════════════════
    // BAŞLAT
    // ════════════════════════════════════════════════════════════════
    public async Task StartAsync(Guid userId)
    {
        if (_connection != null &&
            _connection.State != HubConnectionState.Disconnected)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{BaseUrl}?userId={userId}")
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // ════════════════════════════════════════════════════════════
        // #5 DÜZELTME: Mesaj handler'da AES decrypt uygula.
        // Server ciphertext1 broadcast eder (mobile'ın gönderdiği şifreli içerik).
        // Decrypt yapılmazsa UI'da base64 cipher text görünür.
        // ════════════════════════════════════════════════════════════
        _connection.On<MessageModel>("ReceiveMessage", message =>
        {
            // Decrypt: ciphertext1 → plaintext
            // Eğer içerik zaten düz metin ise (Swagger test vb.), try-catch içinde güvenle döner
            message.Content = _encryption.Decrypt(message.Content);

            MessageReceived?.Invoke(message);
        });

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

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[SignalR] Bağlantı kuruldu.");
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