namespace KampusBag.Core.DTOs;

public class ChatSummaryDto
{
    // ── Kimlik ─────────────────────────────────────────────────────────
    public string ChatId { get; set; } = string.Empty;
    public ChatType Type { get; set; }

    // ── Görüntüleme ────────────────────────────────────────────────────
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarInitial { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#1B305E";

    // ── Son Mesaj ──────────────────────────────────────────────────────
    public string LastMessage { get; set; } = "Henüz mesaj yok";
    public DateTime LastMessageAt { get; set; }
    public string LastMessageTimeText => FormatTime(LastMessageAt);

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

    /// <summary>
    /// Course.IsOfficial değerinden gelir.
    /// true  = Resmi Kanal (sadece hoca/temsilci yazar)
    /// false = Çalışma Odası (herkes yazar)
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// Bu sohbet kanalına kullanıcı mesaj yazabilir mi?
    /// Resmi kanallarda sadece hoca (Role=2) veya temsilci yazabilir.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Giriş yapmış kullanıcı bu derste temsilci mi?
    /// CourseMembership.IsRepresentative alanından gelir.
    /// </summary>
    public bool IsUserRepresentative { get; set; }

    public bool IsOfficialChannel => Type == ChatType.OfficialChannel;

    // ── Durum Bayrakları ───────────────────────────────────────────────
    public bool IsSilentMode { get; set; }
    public bool HasEmergency { get; set; }

    // ── Border Rengi ──────────────────────────────────────────────────
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
    OfficialChannel = 1,   // Resmi kanal — Course.IsOfficial = true
    StudyRoom = 2,   // Çalışma odası — Course.IsOfficial = false
    PrivateMessage = 3    // Birebir mesaj
}
