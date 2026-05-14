using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace KampusBag.MobileUI.Services;

public class NotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    private readonly ApiService _apiService = new();
    private bool _initialized;

    private NotificationService() { }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            await SaveTokenAsync(token);

            CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;
            CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
            CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;

            Console.WriteLine("[FCM] Initialize tamamlandı.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FCM] Initialize hatası: {ex.Message}");
            _initialized = false;
        }
    }

    private async void OnTokenChanged(object? sender, FCMTokenChangedEventArgs e)
    {
        Console.WriteLine($"[FCM] Token yenilendi.");
        await SaveTokenAsync(e.Token);
    }

    private async Task SaveTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        if (!ApiService.Session.IsLoggedIn) return;

        string platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
        bool ok = await _apiService.SaveDeviceTokenAsync(token, platform);
        Console.WriteLine($"[FCM] Token kayıt: {(ok ? "OK" : "FAIL")}");
    }

    // Ön planda: SignalR zaten çalışıyor, gösterme
    private void OnNotificationReceived(object? sender, FCMNotificationReceivedEventArgs e)
    {
        Console.WriteLine("[FCM] Ön plan bildirim alındı, atlandı.");
    }

    // Bildirime tıklandı — doğru sohbete navigate et
    private async void OnNotificationTapped(object? sender, FCMNotificationTappedEventArgs e)
    {
        try
        {
            var data = e.Notification?.Data;
            if (data == null || data.Count == 0) return;

            // FIX: GetValueOrDefault yerine TryGetValue kullan (CS0411 düzeltmesi)
            data.TryGetValue("chatType", out var chatType);
            data.TryGetValue("courseId", out var courseIdStr);
            data.TryGetValue("otherUserId", out var otherIdStr);
            data.TryGetValue("chatName", out var chatName);
            data.TryGetValue("senderName", out var senderName);

            chatName ??= senderName ?? "Sohbet";

            Guid? courseId = Guid.TryParse(courseIdStr?.ToString(), out var c) ? c : null;
            Guid? otherUserId = Guid.TryParse(otherIdStr?.ToString(), out var u) ? u : null;

            if (courseId == null && otherUserId == null) return;

            bool isPrivateWithTeacher = chatType?.ToString() == "private";
            bool isReadOnly = chatType?.ToString() == "official"
                              && ApiService.Session.Role == 1;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = new Views.Chats.ChatDetailPage(
                    chatName: chatName?.ToString() ?? "Sohbet",
                    courseId: courseId,
                    otherUserId: otherUserId,
                    isPrivateWithTeacher: isPrivateWithTeacher,
                    isReadOnly: isReadOnly);

                if (Shell.Current?.Navigation != null)
                    await Shell.Current.Navigation.PushAsync(page);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FCM] Tıklama hatası: {ex.Message}");
        }
    }

    public void Reset()
    {
        try
        {
            CrossFirebaseCloudMessaging.Current.TokenChanged -= OnTokenChanged;
            CrossFirebaseCloudMessaging.Current.NotificationReceived -= OnNotificationReceived;
            CrossFirebaseCloudMessaging.Current.NotificationTapped -= OnNotificationTapped;
            _initialized = false;
        }
        catch { }
    }
}