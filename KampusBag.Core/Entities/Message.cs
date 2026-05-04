namespace KampusBag.Core.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Gönderen
    public Guid SenderId { get; set; }
    public virtual User Sender { get; set; } = null!;

    // Hedef: biri dolu olmalı
    public Guid? ReceiverId { get; set; }
    public Guid? CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    // Durum bayrakları
    public bool IsEmergency { get; set; } = false;
    public bool IsRead { get; set; } = false;  // YENİ
    public bool IsSilent { get; set; } = false;  // YENİ
    public bool? IsApprovedByAcademic { get; set; }
}
