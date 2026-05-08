namespace KampusBag.MobileUI.Models;

public class ChatSummaryModel
{
    // ── Kimlik ────────────────────────────────────────────────────────
    public string ChatId { get; set; } = string.Empty;
    public string ChatType { get; set; } = string.Empty; // private / official / study

    // ── Görüntüleme ───────────────────────────────────────────────────
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarInitial { get; set; } = "?";
    public string AvatarColor { get; set; } = "#1B305E";

    // ── Son Mesaj ─────────────────────────────────────────────────────
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }

    // ── Durum bayrakları ──────────────────────────────────────────────
    public int UnreadCount { get; set; }
    public bool IsSilentMode { get; set; }
    public bool IsLocked { get; set; }
    public bool HasEmergency { get; set; }

    /// <summary>
    /// Course.IsOfficial değerinden gelir.
    /// true  = Resmi Kanal (sadece yetkili yazabilir)
    /// false = Çalışma Odası (herkes yazabilir)
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// Giriş yapan kullanıcı bu kanalda temsilci mi?
    /// CourseMembership.IsRepresentative'den gelir.
    /// Temsilci resmi kanallara da yazabilir.
    /// </summary>
    public bool IsUserRepresentative { get; set; }

    // ── Hedef bilgileri ───────────────────────────────────────────────
    public Guid? OtherUserId { get; set; }
    public int? OtherUserRole { get; set; }
    public Guid? CourseId { get; set; }

    // ════════════════════════════════════════════════════════════════
    // Computed — XAML Binding
    // ════════════════════════════════════════════════════════════════

    public string DisplayNameWithIcon
        => IsSilentMode ? $"{DisplayName} 🌙" : DisplayName;

    public bool HasUnread => UnreadCount > 0;

    public string UnreadText
        => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

    public string TimeText => FormatTime(LastMessageAt);

    public string BorderColor
        => IsSilentMode ? "#FECACA" : "#E5E7EB";

    public double BorderThickness
        => IsSilentMode ? 1.5 : 0.5;

    // ── Statik yardımcı ───────────────────────────────────────────────
    private static string FormatTime(DateTime dt)
    {
        if (dt == DateTime.MinValue) return string.Empty;
        var diff = DateTime.UtcNow - dt;

        if (diff.TotalMinutes < 1) return "Şimdi";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} dk";
        if (diff.TotalHours < 24) return dt.ToLocalTime().ToString("HH:mm");
        if (diff.TotalDays < 2) return "Dün";
        if (diff.TotalDays < 7) return dt.ToLocalTime().ToString("ddd");
        return dt.ToLocalTime().ToString("dd.MM");
    }
}
