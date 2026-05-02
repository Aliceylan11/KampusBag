using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Helpers;
using KampusBag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly IGenericRepository<Message> _messageRepo;
    private readonly IGenericRepository<EmergencyRight> _rightRepo;
    private readonly IGenericRepository<User> _userRepo;
    private readonly KampusBagDbContext _context;

    public MessageService(
        IGenericRepository<Message> messageRepo,
        IGenericRepository<EmergencyRight> rightRepo,
        IGenericRepository<User> userRepo,
        KampusBagDbContext context)
    {
        _messageRepo = messageRepo;
        _rightRepo = rightRepo;
        _userRepo = userRepo;
        _context = context;
    }

    // ════════════════════════════════════════════════════════════════════
    // MESAJ GÖNDER
    // ════════════════════════════════════════════════════════════════════
    public async Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto)
    {
        // ── 1. Göndereni doğrula ─────────────────────────────────────────
        var sender = await _userRepo.GetByIdAsync(dto.SenderId)
            ?? throw new Exception("Gönderen kullanıcı bulunamadı.");

        // ── 2. ACİL HAK KONTROLÜ ─────────────────────────────────────────
        if (dto.IsEmergency)
        {
            var rightRecord = (await _rightRepo
                .FindAsync(r => r.UserId == dto.SenderId))
                .FirstOrDefault();

            if (rightRecord == null || rightRecord.RemainingRights <= 0)
                throw new Exception(
                    "Acil durum mesajı hakkınız kalmadı. " +
                    "Dönemlik 3 hakkınızı kullandınız.");

            // Hakkı düş
            rightRecord.RemainingRights -= 1;
            await _rightRepo.UpdateAsync(rightRecord);
        }

        // ── 3. SESSİZ MOD BAYRAGI ────────────────────────────────────────
        // Alıcı Akademisyen (Role 2) VE saat 17:00 sonrasıysa → IsSilent
        bool isSilent = false;

        if (dto.ReceiverId.HasValue)
        {
            var receiver = await _userRepo.GetByIdAsync(dto.ReceiverId.Value);

            if (receiver?.Role == UserRole.Academic &&
                DateTime.Now.Hour >= 17)
            {
                isSilent = true;
            }
        }

        // ── 4. İÇERİĞİ ŞİFRELE ──────────────────────────────────────────
        string encryptedContent = EncryptionHelper.Encrypt(dto.Content);

        // ── 5. MESAJI OLUŞTUR VE KAYDET ───────────────────────────────────
        var message = new Message
        {
            SenderId = dto.SenderId,
            ReceiverId = dto.ReceiverId,
            CourseId = dto.CourseId,
            Content = encryptedContent,
            IsEmergency = dto.IsEmergency,
            SentAt = DateTime.UtcNow
        };

        await _messageRepo.AddAsync(message);

        // ── 6. DTO OLARAK DÖN ─────────────────────────────────────────────
        return new MessageResponseDto
        {
            Id = message.Id,
            Content = dto.Content,     // Şifresiz döndür
            SentAt = message.SentAt,
            IsEmergency = message.IsEmergency,
            IsSilent = isSilent,
            SenderId = sender.Id,
            SenderName = sender.FullName,
            SenderRole = (int)sender.Role,
            ReceiverId = message.ReceiverId,
            CourseId = message.CourseId
        };
    }

    // ════════════════════════════════════════════════════════════════════
    // MESAJ GEÇMİŞİ
    // ════════════════════════════════════════════════════════════════════
    public async Task<IEnumerable<MessageResponseDto>> GetChatHistoryAsync(
        Guid userId, Guid? otherUserId, Guid? courseId)
    {
        List<Message> messages;

        if (courseId.HasValue)
        {
            // Grup mesajları: CourseId üzerinden
            messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        else if (otherUserId.HasValue)
        {
            // Birebir mesajlar: her iki yön
            messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        else
        {
            throw new Exception(
                "courseId veya otherUserId parametrelerinden biri zorunludur.");
        }

        // İçerikleri çöz ve DTO'ya dönüştür
        return messages.Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = SafeDecrypt(m.Content),
            SentAt = m.SentAt,
            IsEmergency = m.IsEmergency,
            IsSilent = false,   // Geçmişte saklı değil, UI kararı
            SenderId = m.SenderId,
            SenderName = m.Sender?.FullName ?? "Bilinmiyor",
            SenderRole = (int)(m.Sender?.Role ?? 0),
            ReceiverId = m.ReceiverId,
            CourseId = m.CourseId,
            IsApprovedByAcademic = m.IsApprovedByAcademic
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // SOHBET LİSTESİ (Özet)
    // ════════════════════════════════════════════════════════════════════
    public async Task<IEnumerable<ChatSummaryDto>> GetChatListAsync(Guid userId)
    {
        var result = new List<ChatSummaryDto>();
        bool isSilentHour = DateTime.Now.Hour >= 17;

        // ── Grup sohbetleri: üye olduğu dersler ──────────────────────────
        var memberships = await _context.CourseMemberships
            .Include(cm => cm.Course)
            .Where(cm => cm.UserId == userId)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            var course = membership.Course;

            // Bu dersteki son mesaj
            var lastMsg = await _context.Messages
                .Where(m => m.CourseId == course.Id)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            // Okunmamış: son 24 saatte gelmiş mesajlar (basit simülasyon)
            var unread = await _context.Messages
                .Where(m => m.CourseId == course.Id
                         && m.SenderId != userId
                         && m.SentAt > DateTime.UtcNow.AddHours(-24))
                .CountAsync();

            // Resmi kanal mı yoksa çalışma odası mı?
            bool isOfficial = membership.IsRepresentative == false
                           && course.AcademicId != Guid.Empty;

            result.Add(new ChatSummaryDto
            {
                ChatId = course.Id.ToString(),
                DisplayName = course.Name,
                LastMessage = lastMsg != null
                                    ? SafeDecrypt(lastMsg.Content)
                                    : "Henüz mesaj yok",
                LastMessageAt = lastMsg?.SentAt ?? DateTime.MinValue,
                UnreadCount = unread,
                Type = isOfficial ? ChatType.OfficialChannel : ChatType.StudyRoom,
                CourseId = course.Id,
                IsLocked = isOfficial  // Resmi kanallar kilitli
            });
        }

        // ── Birebir mesajlar ──────────────────────────────────────────────
        var contactIds = await _context.Messages
            .Where(m => m.SenderId == userId ||
                        m.ReceiverId == userId)
            .Where(m => m.CourseId == null)
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Where(id => id != null)
            .Distinct()
            .ToListAsync();

        foreach (var contactId in contactIds)
        {
            var contact = await _userRepo.GetByIdAsync(contactId!.Value);
            if (contact == null) continue;

            var lastMsg = await _context.Messages
                .Where(m => m.CourseId == null &&
                    ((m.SenderId == userId && m.ReceiverId == contactId) ||
                     (m.SenderId == contactId && m.ReceiverId == userId)))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unread = await _context.Messages
                .Where(m => m.SenderId == contactId
                         && m.ReceiverId == userId
                         && m.SentAt > DateTime.UtcNow.AddHours(-24))
                .CountAsync();

            // Sessiz mod: karşı taraf akademisyen + 17:00 sonrası
            bool silentMode = contact.Role == UserRole.Academic && isSilentHour;

            result.Add(new ChatSummaryDto
            {
                ChatId = $"{userId}_{contactId}",
                DisplayName = contact.FullName,
                LastMessage = lastMsg != null
                                    ? SafeDecrypt(lastMsg.Content)
                                    : "Henüz mesaj yok",
                LastMessageAt = lastMsg?.SentAt ?? DateTime.MinValue,
                UnreadCount = unread,
                Type = ChatType.PrivateMessage,
                OtherUserId = contact.Id,
                OtherUserRole = (int)contact.Role,
                IsSilentMode = silentMode
            });
        }

        // Zaman sırasına göre sırala (en yeni üstte)
        return result.OrderByDescending(c => c.LastMessageAt);
    }

    // ════════════════════════════════════════════════════════════════════
    // MEVCUT METODLAR (Değişmedi — Backward Compat.)
    // ════════════════════════════════════════════════════════════════════
    public async Task<int> GetRemainingRightsAsync(Guid userId)
    {
        var rightRecord = (await _rightRepo
            .FindAsync(r => r.UserId == userId))
            .FirstOrDefault();
        return rightRecord?.RemainingRights ?? 0;
    }

    public async Task<bool> SendEmergencyMessageAsync(
        Guid senderId, Guid courseId, string content)
    {
        var rightRecord = (await _rightRepo
            .FindAsync(r => r.UserId == senderId))
            .FirstOrDefault();

        if (rightRecord == null || rightRecord.RemainingRights <= 0)
            return false;

        var message = new Message
        {
            SenderId = senderId,
            CourseId = courseId,
            Content = EncryptionHelper.Encrypt(content),
            IsEmergency = true,
            SentAt = DateTime.UtcNow
        };

        await _messageRepo.AddAsync(message);

        rightRecord.RemainingRights -= 1;
        await _rightRepo.UpdateAsync(rightRecord);

        return true;
    }

    // ── Yardımcı: Şifre çözme — bozuk içeriği sessizce atla ─────────────
    private static string SafeDecrypt(string content)
    {
        try { return EncryptionHelper.Decrypt(content); }
        catch { return content; }   // Eski şifresiz kayıtlar için
    }
}
