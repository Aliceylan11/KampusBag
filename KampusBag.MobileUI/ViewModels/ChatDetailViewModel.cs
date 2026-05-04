using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace KampusBag.MobileUI.ViewModels;

public class ChatDetailViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService = new();

    // ── Parametreler ─────────────────────────────────────────────────
    public string ChatName { get; set; } = "Sohbet";
    public Guid? OtherUserId { get; set; }
    public Guid? CourseId { get; set; }
    public bool IsPrivateWithTeacher { get; set; }
    public bool IsReadOnly { get; set; }

    // ── Mesajlar ─────────────────────────────────────────────────────
    // Acil mesajlar her zaman başta, sonra zaman sırasında
    public ObservableCollection<MessageModel> Messages { get; } = new();

    // ── Giriş ─────────────────────────────────────────────────────────
    private string _messageText = string.Empty;
    public string MessageText
    {
        get => _messageText;
        set
        {
            Set(ref _messageText, value);
            OnPropertyChanged(nameof(CanSend));
        }
    }

    // ── Acil Hak ──────────────────────────────────────────────────────
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

    public string RightsText
        => $"Acil hak: {RemainingRights}/3";

    public bool ShowEmergencyButton
        => IsPrivateWithTeacher && !IsReadOnly && RemainingRights > 0;

    // ── State ─────────────────────────────────────────────────────────
    private bool _isLoading;
    private bool _isSending;
    private string _statusMessage = string.Empty;

    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
    public bool IsSending { get => _isSending; set => Set(ref _isSending, value); }
    public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }
    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && !IsSending;

    // ── Komutlar ──────────────────────────────────────────────────────
    public ICommand LoadHistoryCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand SendEmergencyCommand { get; }

    // Page referansı — DisplayAlert için
    private readonly Page _page;

    public ChatDetailViewModel(Page page)
    {
        _page = page;
        LoadHistoryCommand = new Command(async () => await LoadHistoryAsync());
        SendCommand = new Command(async () => await SendAsync(false));
        SendEmergencyCommand = new Command(async () => await SendEmergencyAsync());
    }

    // ── Geçmiş Yükleme ───────────────────────────────────────────────
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;

        // Acil hak bilgisini al
        var (ok, remaining) = await _apiService
            .GetEmergencyRightsAsync(ApiService.Session.UserId);
        if (ok) RemainingRights = remaining;

        // Mesaj geçmişini al
        var (success, messages, error) = await _apiService
            .GetChatHistoryAsync(ApiService.Session.UserId, OtherUserId, CourseId);

        if (success)
        {
            // Acil mesajlar önce, sonra tarih sırası
            var sorted = messages
                .OrderByDescending(m => m.IsEmergency)
                .ThenBy(m => m.SentAt);

            Messages.Clear();
            foreach (var m in sorted) Messages.Add(m);

            // Okundu işaretle
            await _apiService.MarkAsReadAsync(OtherUserId, CourseId);
        }
        else
        {
            await _page.DisplayAlert("Hata", error, "Tamam");
        }

        IsLoading = false;
    }

    // ── Normal Mesaj Gönder ───────────────────────────────────────────
    private async Task SendAsync(bool isEmergency)
    {
        if (!CanSend) return;

        IsSending = true;
        string text = MessageText.Trim();
        MessageText = string.Empty;

        var result = await _apiService.SendMessageAsync(
            OtherUserId, CourseId, text, isEmergency);

        if (result.Success && result.Message != null)
        {
            // Gönderilen mesajı listeye ekle
            var newMsg = new MessageModel
            {
                Id = result.Message.Id,
                Content = text,     // Şifreli değil, ham metin
                SentAt = DateTime.UtcNow,
                SenderId = ApiService.Session.UserId,
                SenderName = ApiService.Session.FullName,
                SenderRole = ApiService.Session.Role,
                IsEmergency = isEmergency,
                IsSilent = result.Message.IsSilent,
                ReceiverId = OtherUserId,
                CourseId = CourseId
            };

            // Sessiz mod uyarısı
            if (newMsg.IsSilent)
            {
                await _page.DisplayAlert(
                    "🌙 Sessiz Mod",
                    "Mesajınız iletildi. Alıcı 17:00 sonrası bildirim almayacak.",
                    "Tamam");
            }

            // Acil mesajlar başa, normal mesajlar sona
            if (isEmergency)
                Messages.Insert(0, newMsg);
            else
                Messages.Add(newMsg);

            RemainingRights = isEmergency
                ? Math.Max(0, RemainingRights - 1)
                : RemainingRights;
        }
        else if (result.IsRightsDepleted)
        {
            // 422 — Acil hak bitti
            await _page.DisplayAlert(
                "🚨 Hak Doldu",
                result.StatusMsg,
                "Tamam");
        }
        else
        {
            await _page.DisplayAlert("Hata", result.StatusMsg, "Tamam");
        }

        IsSending = false;
    }

    // ── Acil Mesaj Gönder ─────────────────────────────────────────────
    private async Task SendEmergencyAsync()
    {
        if (!CanSend) return;

        bool confirm = await _page.DisplayAlert(
            "🚨 Acil Mesaj",
            $"Bu mesaj 1 acil hakkınızı tüketecek. " +
            $"Kalan hakkınız: {RemainingRights}/3\n\nDevam etmek istiyor musunuz?",
            "Evet, Gönder",
            "Vazgeç");

        if (confirm) await SendAsync(true);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
