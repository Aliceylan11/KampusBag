using KampusBag.Core.Entities;

namespace KampusBag.Core.Interfaces;

public interface IMessageService
{
    // Acil durum mesajı gönderme (Hak kontrolü yaparak)
    Task<bool> SendEmergencyMessageAsync(Guid senderId, Guid courseId, string content);

    // Kalan hak sayısını öğrenme
    Task<int> GetRemainingRightsAsync(Guid userId);
}