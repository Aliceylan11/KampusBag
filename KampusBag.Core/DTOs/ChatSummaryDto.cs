namespace KampusBag.Core.DTOs;

public class ChatSummaryDto
{
    // Sohbet kimliği (birebir: "userId1_userId2", grup: courseId)
    public string ChatId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Son mesaj önizlemesi
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }

    // Okunmamış sayısı
    public int UnreadCount { get; set; }

    // Sohbet tipi
    public ChatType Type { get; set; }

    // Özel mesajsa karşı tarafın bilgisi
    public Guid? OtherUserId { get; set; }
    public int? OtherUserRole { get; set; }

    // Grup ise ders bilgisi
    public Guid? CourseId { get; set; }
    public bool IsLocked { get; set; }   // Resmi kanal kilitli mi?

    // Sessiz mod (akademisyen + 17:00 sonrası)
    public bool IsSilentMode { get; set; }
}

public enum ChatType
{
    OfficialChannel = 1,   // Resmi kanal - sadece yetkili yazar
    StudyRoom = 2,   // Çalışma odası - herkes yazar
    PrivateMessage = 3    // Birebir mesaj
}
