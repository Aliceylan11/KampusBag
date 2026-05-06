namespace KampusBag.MobileUI.Models;

public class UserSearchModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public int Role { get; set; }

    // ── Computed — XAML Binding ────────────────────────────────────────
    public string AvatarInitial
        => string.IsNullOrEmpty(FullName) ? "?" : FullName[0].ToString().ToUpper();

    public string RoleLabel => Role switch
    {
        1 => "Öğrenci",
        2 => "Akademisyen",
        3 => "Temsilci",
        _ => "Kullanıcı"
    };

    public string RoleIcon => Role switch
    {
        1 => "👨‍🎓",
        2 => "👨‍🏫",
        3 => "🏅",
        _ => "👤"
    };

    public string AvatarColor => Role switch
    {
        2 => "#1B305E",   // Akademisyen — lacivert
        3 => "#7C3AED",   // Temsilci — mor
        _ => "#059669"    // Öğrenci — yeşil
    };

    public string RoleBadgeColor => Role switch
    {
        2 => "#EEF2FF",
        3 => "#F5F3FF",
        _ => "#F0FDF4"
    };

    public string RoleTextColor => Role switch
    {
        2 => "#1D4ED8",
        3 => "#6D28D9",
        _ => "#166534"
    };

    // Mesaj gönderilirse sessiz mod uyarısı gerekli mi?
    public bool MightBeSilent
        => Role == 2 && DateTime.Now.Hour >= 17;
}
