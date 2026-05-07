namespace KampusBag.Core.DTOs;

public class ChatSummaryDto
{
    // ── Kimlik ─────────────────────────────────────────────────────────
    // Birebir: "{userId}_{contactId}"
    // Ders   : "{courseId}"
    public string ChatId { get; set; } = string.Empty;
    public ChatType Type { get; set; }

    // ── Görüntüleme ────────────────────────────────────────────────────
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarInitial { get; set; } = string.Empty; // İlk harf
    public string AvatarColor { get; set; } = "#1B305E";    // Rol'e göre

    // ── Son Mesaj ──────────────────────────────────────────────────────
    public string LastMessage { get; set; } = "Henüz mesaj yok";
    public DateTime LastMessageAt { get; set; }
    public string LastMessageTimeText       // UI'a hazır formatlı zaman
        => FormatTime(LastMessageAt);

    // ── Okunmamış ──────────────────────────────────────────────────────
    public int UnreadCount { get; set; }
    public bool HasUnread => UnreadCount > 0;
    public string UnreadText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

    // ── Özel Mesaj Alanları ────────────────────────────────────────────
    public Guid? OtherUserId { get; set; }
    public int? OtherUserRole { get; set; }
    public bool IsAcademic => OtherUserRole == 2;

    // ── Ders Alanları ──────────────────────────────────────────────────
    public Guid? CourseId { get; set; }
    public bool IsLocked { get; set; }    // Resmi kanal kilitli mi?
    public bool IsOfficial { get; set; }

    // ── Durum Bayrakları ───────────────────────────────────────────────
    public bool IsSilentMode { get; set; }    // 17:00 sonrası + akademisyen
    public bool HasEmergency { get; set; }    // Acil mesaj var mı?

    // ── Border Rengi (XAML binding için) ──────────────────────────────
    public string BorderColor => IsSilentMode ? "#FECACA" : "#E5E7EB";

    // ── Zaman Formatlayıcı ─────────────────────────────────────────────
    private static string FormatTime(DateTime dt)
    {
        if (dt == DateTime.MinValue) return string.Empty;

        var now = DateTime.UtcNow;
        var diff = now - dt;

        if (diff.TotalMinutes < 1) return "Şimdi";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} dk";
        if (diff.TotalHours < 24) return dt.ToLocalTime().ToString("HH:mm");
        if (diff.TotalDays < 2) return "Dün";
        if (diff.TotalDays < 7) return dt.ToLocalTime().ToString("ddd");

        return dt.ToLocalTime().ToString("dd.MM");
    }
}

public enum ChatType
{
    OfficialChannel = 1,   // Resmi kanal — sadece yetkili yazar
    StudyRoom = 2,   // Çalışma odası — herkes yazar
    PrivateMessage = 3    // Birebir mesaj
}
