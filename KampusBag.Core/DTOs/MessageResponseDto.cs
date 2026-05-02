namespace KampusBag.Core.DTOs;

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsEmergency { get; set; }
    public bool IsSilent { get; set; }

    // Gönderen bilgisi
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int SenderRole { get; set; }

    // Hedef
    public Guid? ReceiverId { get; set; }
    public Guid? CourseId { get; set; }

    // Onay durumu (hoca onayı)
    public bool? IsApprovedByAcademic { get; set; }
}
