namespace KampusBag.MobileUI.Models;

public class CourseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AcademicName { get; set; } = string.Empty;
    public int MemberCount { get; set; }

    /// <summary>
    /// Course.IsOfficial'dan gelir.
    /// true  = Resmi Kanal (hoca açtı)
    /// false = Çalışma Odası (temsilci/öğrenci açtı)
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// CourseMembership.IsRepresentative'den gelir.
    /// Giriş yapan kullanıcı bu derste temsilci mi?
    /// </summary>
    public bool IsRepresentative { get; set; }

    // ════════════════════════════════════════════════════════════════
    // Computed — XAML Binding
    // ════════════════════════════════════════════════════════════════

    /// <summary>Kanal tipini UI'da göster.</summary>
    public string TypeLabel
        => IsOfficial ? "🏛️ Resmi Kanal" : "📚 Çalışma Odası";

    /// <summary>Avatar arka plan rengi.</summary>
    public string TypeColor
        => IsOfficial ? "#1B305E" : "#059669";

    public string MemberText
        => MemberCount == 1 ? "1 üye" : $"{MemberCount} üye";

    public string AvatarInitial
        => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
}
