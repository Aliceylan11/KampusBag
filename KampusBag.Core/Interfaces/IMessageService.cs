using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;

namespace KampusBag.Core.Interfaces;

public interface IMessageService
{
    // ── Mesaj Gönderimi ──────────────────────────────────────────────────
    // Acil hak kontrolü, sessiz mod bayrağı ve şifreleme burada yapılır
    Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto);

    // ── Mesaj Geçmişi ────────────────────────────────────────────────────
    // İki kullanıcı arasındaki veya bir gruba ait tüm mesajları döner
    Task<IEnumerable<MessageResponseDto>> GetChatHistoryAsync(
        Guid userId, Guid? otherUserId, Guid? courseId);

    // ── Sohbet Listesi ───────────────────────────────────────────────────
    // Kullanıcının dahil olduğu tüm sohbetlerin özetini döner
    Task<IEnumerable<ChatSummaryDto>> GetChatListAsync(Guid userId);

    // ── Mevcut Metodlar (değişmedi) ───────────────────────────────────────
    Task<bool> SendEmergencyMessageAsync(Guid senderId, Guid courseId, string content);
    Task<int> GetRemainingRightsAsync(Guid userId);
}
