namespace KampusBag.MobileUI.Models;

public class CourseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AcademicName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsRepresentative { get; set; }

    // Computed
    public string TypeLabel
        => IsRepresentative ? "📚 Çalışma Odası" : "🏛️ Resmi Kanal";

    public string TypeColor
        => IsRepresentative ? "#059669" : "#1B305E";

    public string MemberText
        => MemberCount == 1 ? "1 üye" : $"{MemberCount} üye";

    public string AvatarInitial
        => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
}
