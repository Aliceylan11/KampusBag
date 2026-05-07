namespace KampusBag.MobileUI.Models;

public class CourseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AcademicName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsOfficial { get; set; }

    // Computed
    public string TypeLabel
        => IsOfficial ? "🏛️ Resmi Kanal" : "📚 Çalışma Odası";
    public string TypeColor
        => IsOfficial ? "#059669" : "#1B305E";

    public string MemberText
        => MemberCount == 1 ? "1 üye" : $"{MemberCount} üye";

    public string AvatarInitial
        => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
}
