using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;

namespace KampusBag.Core.Interfaces;

public interface IMessageService
{
    // ── Mesaj Gönderimi ──────────────────────────────────────────────────
    Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto);

    // ── Mesaj Geçmişi — #6 Pagination ────────────────────────────────────
    // page=1, pageSize=50 ile son mesajlar önce gelir.
    Task<IEnumerable<MessageResponseDto>> GetChatHistoryAsync(
        Guid userId,
        Guid? otherUserId,
        Guid? courseId,
        int page = 1,
        int pageSize = 50);

    // ── Sohbet Listesi ───────────────────────────────────────────────────
    Task<IEnumerable<ChatSummaryDto>> GetChatListAsync(Guid userId);

    // ── Mevcut Metodlar ───────────────────────────────────────────────────
    Task<bool> SendEmergencyMessageAsync(Guid senderId, Guid courseId, string content);
    Task<int> GetRemainingRightsAsync(Guid userId);
    Task<int> MarkMessagesAsReadAsync(Guid userId, Guid? senderId, Guid? courseId);
    Task<int> GetCountByUserIdAsync(Guid userId);
}
