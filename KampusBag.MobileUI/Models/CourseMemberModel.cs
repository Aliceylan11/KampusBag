namespace KampusBag.MobileUI.Models;

/// <summary>
/// Temsilci atama ekranı ve ders üye listesi için model.
/// CoursesController.GetCourseMembers endpoint'inden gelir.
/// </summary>
public class CourseMemberModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsRepresentative { get; set; }

    // ── Computed — XAML Binding ────────────────────────────────────
    public string RoleLabel => Role switch
    {
        1 => "Öğrenci",
        2 => "Akademisyen",
        3 => "Temsilci",
        _ => "Kullanıcı"
    };

    public string AvatarInitial
        => string.IsNullOrEmpty(FullName) ? "?" : FullName[0].ToString().ToUpper();

    public string RepresentativeLabel
        => IsRepresentative ? "Temsilciyi Geri Al" : "Temsilci Yap";

    public string RepresentativeColor
        => IsRepresentative ? "#DC2626" : "#059669";
}