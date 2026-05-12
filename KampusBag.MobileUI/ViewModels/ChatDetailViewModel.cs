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
    private readonly SignalRService _signalR = new();
    private readonly EncryptionService _encryption = new();

    // ── Parametreler ─────────────────────────────────────────────────
    public string ChatName { get; set; } = "Sohbet";
    public Guid? OtherUserId { get; set; }
    public Guid? CourseId { get; set; }
    public bool IsPrivateWithTeacher { get; set; }
    public bool IsReadOnly { get; set; }

    // ── Mesaj listesi ─────────────────────────────────────────────────
    public ObservableCollection<MessageModel> Messages { get; } = new();

    // #6 Pagination
    private int _currentPage = 1;
    private const int PageSize = 50;
    public bool HasMoreMessages { get; private set; } = true;

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

    // ── SignalR durum ─────────────────────────────────────────────────
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
    public ICommand LoadMoreCommand { get; }

    private readonly Page _page;

    public ChatDetailViewModel(Page page)
    {
        _page = page;
        SendCommand = new Command(async () => await SendAsync(false));
        SendEmergencyCommand = new Command(async () => await ConfirmAndSendEmergencyAsync());
        LoadMoreCommand = new Command(async () => await LoadHistoryAsync(loadMore: true));
    }

    // ════════════════════════════════════════════════════════════════
    // BAŞLAT
    // ════════════════════════════════════════════════════════════════
    public async Task InitializeAsync()
    {
        // #7 GUARD: Her iki ID de null ise işlem yapma
        if (!CourseId.HasValue && !OtherUserId.HasValue)
            throw new InvalidOperationException("CourseId veya OtherUserId gereklidir.");

        await StartSignalRAsync();

        var (ok, remaining) = await _apiService.GetEmergencyRightsAsync(ApiService.Session.UserId);
        if (ok) RemainingRights = remaining;

        await LoadHistoryAsync(loadMore: false);
    }

    // ════════════════════════════════════════════════════════════════
    // SignalR
    // ════════════════════════════════════════════════════════════════
    private async Task StartSignalRAsync()
    {
        _signalR.MessageReceived -= OnSignalRMessageReceived;
        _signalR.MessageReceived += OnSignalRMessageReceived;

        await _signalR.StartAsync(ApiService.Session.UserId);
        IsSignalRConnected = _signalR.IsConnected;

        if (CourseId.HasValue)
            await _signalR.JoinCourseRoomAsync(CourseId.Value);
        else if (OtherUserId.HasValue)
            await _signalR.JoinPrivateRoomAsync(ApiService.Session.UserId, OtherUserId.Value);
    }

    // ─── SignalR mesaj handler ────────────────────────────────────────
    // #5: SignalRService zaten decrypt eder, burada tekrar etme.
    // Sadece filtrele ve UI thread'e geç.
    private void OnSignalRMessageReceived(MessageModel message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Kendi mesajımızın echo'sunu atla (ID ile kontrol)
            if (Messages.Any(m => m.Id == message.Id)) return;

            // Bu sohbete ait değilse yoksay
            if (CourseId.HasValue && message.CourseId != CourseId) return;
            if (OtherUserId.HasValue && message.CourseId.HasValue) return;

            Messages.Add(message);
        });
    }

    // ════════════════════════════════════════════════════════════════
    // GEÇMİŞ — #6 Pagination + Background Decrypt
    // ════════════════════════════════════════════════════════════════
    public async Task LoadHistoryAsync(bool loadMore = false)
    {
        if (!loadMore) _currentPage = 1;
        if (loadMore && !HasMoreMessages) return;

        IsLoading = true;

        try
        {
            var (success, messages, error) = await _apiService.GetChatHistoryAsync(
                ApiService.Session.UserId,
                OtherUserId,
                CourseId,
                page: _currentPage,
                pageSize: PageSize);

            if (!success)
            {
                await _page.DisplayAlert("Hata", error, "Tamam");
                return;
            }

            // #6: AES decrypt'i background thread'de yap — main thread donmasını önler
            var decrypted = await Task.Run(() =>
                messages
                    .OrderBy(m => m.SentAt)
                    .ToList());

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!loadMore) Messages.Clear();

                foreach (var m in decrypted)
                    Messages.Add(m);

                HasMoreMessages = messages.Count == PageSize;
                if (loadMore) _currentPage++;
            });

            await _apiService.MarkAsReadAsync(OtherUserId, CourseId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatDetailViewModel] LoadHistoryAsync hata: {ex}");
            await _page.DisplayAlert("Hata", $"Mesajlar yüklenemedi: {ex.Message}", "Tamam");
        }
        finally
        {
            IsLoading = false;
        }
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
            // Mesajı hemen lokale ekle (SignalR echo gelince ID kontrolü atar)
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
                await _page.DisplayAlert("🌙 Sessiz Mod",
                    "Mesaj iletildi. Alıcı 17:00 sonrası bildirim almayacak.", "Tamam");

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
            "🚨 Acil Mesaj",
            $"Kalan hakkınız: {RemainingRights}/3\n\nBu işlem 1 acil hakkınızı tüketecek.",
            "Evet, Gönder", "Vazgeç");

        if (confirm) await SendAsync(true);
    }

    // ════════════════════════════════════════════════════════════════
    // TEMİZLE
    // ════════════════════════════════════════════════════════════════
    public async Task CleanupAsync()
    {
        _signalR.MessageReceived -= OnSignalRMessageReceived;

        if (CourseId.HasValue)
            await _signalR.LeaveCourseRoomAsync(CourseId.Value);
        else if (OtherUserId.HasValue)
            await _signalR.LeavePrivateRoomAsync(ApiService.Session.UserId, OtherUserId.Value);

        await _signalR.StopAsync();
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