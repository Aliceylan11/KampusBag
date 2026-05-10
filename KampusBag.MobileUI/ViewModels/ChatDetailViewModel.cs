using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class ChatDetailViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService = new();
    private readonly SignalRService _signalR = new();   // YENİ

    // ── Parametreler ─────────────────────────────────────────────────
    public string ChatName { get; set; } = "Sohbet";
    public Guid? OtherUserId { get; set; }
    public Guid? CourseId { get; set; }
    public bool IsPrivateWithTeacher { get; set; }
    public bool IsReadOnly { get; set; }

    // ── Mesaj koleksiyonu ─────────────────────────────────────────────
    public ObservableCollection<MessageModel> Messages { get; } = new();

    // ── Metin girişi ──────────────────────────────────────────────────
    private string _messageText = string.Empty;
    public string MessageText
    {
        get => _messageText;
        set { Set(ref _messageText, value); OnPropertyChanged(nameof(CanSend)); }
    }

    // ── Acil hak ─────────────────────────────────────────────────────
    private int _remainingRights = 3;
    public int RemainingRights
    {
        get => _remainingRights;
        set
        {
            Set(ref _remainingRights, value);
            OnPropertyChanged(nameof(RightsText));
            OnPropertyChanged(nameof(ShowEmergencyButton));
        }
    }

    public string RightsText => $"Acil hak: {RemainingRights}/3";
    public bool ShowEmergencyButton => IsPrivateWithTeacher && !IsReadOnly && RemainingRights > 0;

    // ── State ─────────────────────────────────────────────────────────
    private bool _isLoading;
    private bool _isSending;

    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
    public bool IsSending { get => _isSending; set { Set(ref _isSending, value); OnPropertyChanged(nameof(CanSend)); } }
    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && !IsSending && !IsReadOnly;

    // ── SignalR bağlantı durumu ───────────────────────────────────────
    private bool _isSignalRConnected;
    public bool IsSignalRConnected
    {
        get => _isSignalRConnected;
        set { Set(ref _isSignalRConnected, value); OnPropertyChanged(nameof(ConnectionStatusText)); }
    }
    public string ConnectionStatusText
        => IsSignalRConnected ? string.Empty : "⚠️ Canlı bağlantı yok";

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand SendCommand { get; }
    public ICommand SendEmergencyCommand { get; }

    private readonly Page _page;

    public ChatDetailViewModel(Page page)
    {
        _page = page;
        SendCommand = new Command(async () => await SendAsync(false));
        SendEmergencyCommand = new Command(async () => await ConfirmAndSendEmergencyAsync());
    }

    // ════════════════════════════════════════════════════════════════
    // BAŞLAT: SignalR + Geçmiş
    // ════════════════════════════════════════════════════════════════
    public async Task InitializeAsync()
    {
        // 1. SignalR'ı başlat ve odaya katıl
        await StartSignalRAsync();

        // 2. Acil hak bilgisi
        var (ok, remaining) = await _apiService.GetEmergencyRightsAsync(ApiService.Session.UserId);
        if (ok) RemainingRights = remaining;

        // 3. Geçmiş mesajları yükle
        await LoadHistoryAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // SignalR başlatma
    // ════════════════════════════════════════════════════════════════
    private async Task StartSignalRAsync()
    {
        // Mesaj gelince çalışacak handler'ı kaydet
        _signalR.MessageReceived -= OnSignalRMessageReceived; // çift kayıt önle
        _signalR.MessageReceived += OnSignalRMessageReceived;

        await _signalR.StartAsync(ApiService.Session.UserId);
        IsSignalRConnected = _signalR.IsConnected;

        // Uygun odaya katıl
        if (CourseId.HasValue)
            await _signalR.JoinCourseRoomAsync(CourseId.Value);
        else if (OtherUserId.HasValue)
            await _signalR.JoinPrivateRoomAsync(ApiService.Session.UserId, OtherUserId.Value);
    }

    // ════════════════════════════════════════════════════════════════
    // SignalR mesaj handler'ı
    // ════════════════════════════════════════════════════════════════
    private void OnSignalRMessageReceived(MessageModel message)
    {
        // UI thread'e geç (ObservableCollection cross-thread güncellemesi kırılır)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Kendi gönderdiğimiz mesajı tekrar ekleme (ID ile kontrol)
            if (Messages.Any(m => m.Id == message.Id)) return;

            // Bu sohbete ait değilse yoksay
            if (CourseId.HasValue && message.CourseId != CourseId) return;
            if (OtherUserId.HasValue &&
                message.CourseId.HasValue) return; // grup mesajı, özel sohbete ekleme

            Messages.Add(message);
        });
    }

    // ════════════════════════════════════════════════════════════════
    // TEMIZLE: Sayfa kapanınca odadan ayrıl
    // ════════════════════════════════════════════════════════════════
    public async Task CleanupAsync()
    {
        _signalR.MessageReceived -= OnSignalRMessageReceived;

        if (CourseId.HasValue)
            await _signalR.LeaveCourseRoomAsync(CourseId.Value);
        else if (OtherUserId.HasValue)
            await _signalR.LeavePrivateRoomAsync(ApiService.Session.UserId, OtherUserId.Value);

        // Bağlantıyı kapat (uygulama geneli tek bağlantı istersen StopAsync'i kaldır)
        await _signalR.StopAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // GEÇMİŞ YÜKLEME (HTTP)
    // ════════════════════════════════════════════════════════════════
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;

        var (success, messages, error) = await _apiService
            .GetChatHistoryAsync(ApiService.Session.UserId, OtherUserId, CourseId);

        if (success)
        {
            var sorted = messages.OrderBy(m => m.SentAt);
            Messages.Clear();
            foreach (var m in sorted) Messages.Add(m);

            // Okundu işaretle
            await _apiService.MarkAsReadAsync(OtherUserId, CourseId);
        }
        else
        {
            await _page.DisplayAlert("Bağlantı Hatası", error, "Tamam");
        }

        IsLoading = false;
    }

    // ════════════════════════════════════════════════════════════════
    // MESAJ GÖNDER
    // ════════════════════════════════════════════════════════════════
    private async Task SendAsync(bool isEmergency)
    {
        if (!CanSend) return;

        IsSending = true;
        var text = MessageText.Trim();
        MessageText = string.Empty;

        var result = await _apiService.SendMessageAsync(
            OtherUserId, CourseId, text, isEmergency);

        if (result.Success && result.Message != null)
        {
            // Mesajı hemen lokalde ekle (SignalR echo'su gelince ID kontrolü atar)
            var newMsg = new MessageModel
            {
                Id = result.Message.Id,
                Content = text,
                SentAt = DateTime.UtcNow,
                SenderId = ApiService.Session.UserId,
                SenderName = ApiService.Session.FullName,
                SenderRole = ApiService.Session.Role,
                IsEmergency = isEmergency,
                IsSilent = result.Message.IsSilent,
                ReceiverId = OtherUserId,
                CourseId = CourseId
            };

            if (newMsg.IsSilent)
                await _page.DisplayAlert(
                    "🌙 Sessiz Mod",
                    "Mesaj iletildi. Alıcı 17:00 sonrası bildirim almayacak.",
                    "Tamam");

            Messages.Add(newMsg);

            if (isEmergency)
                RemainingRights = Math.Max(0, RemainingRights - 1);
        }
        else if (result.IsRightsDepleted)
        {
            await _page.DisplayAlert("🚨 Hak Doldu", result.StatusMsg, "Tamam");
            MessageText = text;
        }
        else
        {
            await _page.DisplayAlert("Hata", result.StatusMsg, "Tamam");
            MessageText = text;
        }

        IsSending = false;
    }

    private async Task ConfirmAndSendEmergencyAsync()
    {
        if (!CanSend) return;

        bool confirm = await _page.DisplayAlert(
            "🚨 Acil Mesaj Gönder",
            $"Kalan hakkınız: {RemainingRights}/3\n\n"
            + "Bu işlem 1 acil hakkınızı tüketecek. Devam edilsin mi?",
            "Evet, Gönder", "Vazgeç");

        if (confirm) await SendAsync(true);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
    {
        if (EqualityComparer<T>.Default.Equals(f, v)) return;
        f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
