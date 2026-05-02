namespace KampusBag.Core.DTOs;

public class SendMessageDto
{
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }   // Özel mesajsa dolu
    public Guid? CourseId { get; set; }   // Grup mesajıysa dolu
    public string Content { get; set; } = string.Empty;
    public bool IsEmergency { get; set; } = false;
}
