using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Models;

public class MessageModel
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty; // Çözülmüş metin
    public DateTime SentAt { get; set; }
    public bool IsEmergency { get; set; }
    public bool IsSilent { get; set; }

    // Gönderen
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int SenderRole { get; set; }

    // Hedef
    public Guid? ReceiverId { get; set; }
    public Guid? CourseId { get; set; }

    // Onay
    public bool? IsApprovedByAcademic { get; set; }

    // ── Computed — XAML Binding ─────────────────────────────────────────

    // Benim mesajım mı?
    public bool IsMine
        => SenderId == ApiService.Session.UserId;

    // Mesaj hizalaması
    public LayoutOptions BubbleAlignment
        => IsMine ? LayoutOptions.End : LayoutOptions.Start;

    // Balon rengi
    public Color BubbleColor
        => IsEmergency
            ? Color.FromArgb("#FFEBEE")   // Acil → açık kırmızı
            : IsMine
                ? Color.FromArgb("#1B305E") // Ben → lacivert
                : Color.FromArgb("#F0F2F5"); // Karşı → açık gri

    // Metin rengi
    public Color TextColor
        => IsMine && !IsEmergency
            ? Colors.White
            : Color.FromArgb("#111827");

    // Acil mesaj border rengi
    public Color EmergencyBorderColor
        => IsEmergency ? Color.FromArgb("#D32F2F") : Colors.Transparent;

    // Zaman metni
    public string TimeText
        => SentAt.ToLocalTime().ToString("HH:mm");

    // Acil mesaj etiketi
    public bool ShowEmergencyBadge => IsEmergency;

    // Sessiz mod etiketi
    public bool ShowSilentBadge => IsSilent;
}
