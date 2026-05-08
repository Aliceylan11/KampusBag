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
    // TOPLAM MESAJ SAYISI (Profil sayfası için)
    // ════════════════════════════════════════════════════════════════════
    public async Task<int> GetCountByUserIdAsync(Guid userId)
        => await _context.Messages.CountAsync(m => m.SenderId == userId);

    // ════════════════════════════════════════════════════════════════════
    // MESAJ GÖNDER
    // ════════════════════════════════════════════════════════════════════
    public async Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sender = await _userRepo.GetByIdAsync(dto.SenderId)
                ?? throw new Exception("Gönderen kullanıcı bulunamadı.");

            // ── Acil hak kontrolü ──────────────────────────────────────
            if (dto.IsEmergency && sender.Role != UserRole.Admin)
            {
                string currentTerm = GetCurrentAcademicTerm();

                var rightRecord = (await _rightRepo.FindAsync(r =>
                    r.UserId == dto.SenderId && r.AcademicTerm == currentTerm))
                    .FirstOrDefault();

                if (rightRecord == null)
                {
                    rightRecord = new EmergencyRight
                    {
                        UserId = dto.SenderId,
                        RemainingRights = 3,
                        AcademicTerm = currentTerm
                    };
                    await _rightRepo.AddAsync(rightRecord);
                }

                if (rightRecord.RemainingRights <= 0)
                    throw new Exception(
                        $"Acil durum mesajı hakkınız kalmadı. {currentTerm} dönemi sınırı 3 adettir.");

                rightRecord.RemainingRights -= 1;
                await _rightRepo.UpdateAsync(rightRecord);
            }

            // ── Sessiz mod kontrolü ────────────────────────────────────
            bool isSilent = false;
            if (dto.ReceiverId.HasValue)
            {
                var receiver = await _userRepo.GetByIdAsync(dto.ReceiverId.Value);
                if (receiver?.Role == UserRole.Academic && DateTime.Now.Hour >= 17)
                    isSilent = true;
            }

            // ── Şifrele ve kaydet ──────────────────────────────────────
            var message = new Message
            {
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId,
                CourseId = dto.CourseId,
                Content = EncryptionHelper.Encrypt(dto.Content),
                IsEmergency = dto.IsEmergency,
                SentAt = DateTime.UtcNow,
                IsSilent = isSilent
            };

            await _messageRepo.AddAsync(message);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new MessageResponseDto
            {
                Id = message.Id,
                Content = dto.Content,
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Mesaj gönderilemedi: {ex.Message}");
        }
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
            messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        else if (otherUserId.HasValue)
        {
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
            throw new Exception("courseId veya otherUserId parametrelerinden biri zorunludur.");
        }

        return messages.Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = SafeDecrypt(m.Content),
            SentAt = m.SentAt,
            IsEmergency = m.IsEmergency,
            IsSilent = false,
            SenderId = m.SenderId,
            SenderName = m.Sender?.FullName ?? "Bilinmiyor",
            SenderRole = (int)(m.Sender?.Role ?? 0),
            ReceiverId = m.ReceiverId,
            CourseId = m.CourseId,
            IsApprovedByAcademic = m.IsApprovedByAcademic
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // SOHBET LİSTESİ (özet)
    // ════════════════════════════════════════════════════════════════════
    public async Task<IEnumerable<ChatSummaryDto>> GetChatListAsync(Guid userId)
    {
        // DÜZELTME: Ders kategorisi artık Course.IsOfficial üzerinden belirleniyor.
        // Önceki yanlış mantık: cm."IsRepresentative" → 'official'/'study'
        // Doğru mantık:        c."IsOfficial" = true → 'official', false → 'study'
        //
        // EK: is_user_representative — giriş yapan kullanıcının o dersteki temsilcilik durumu
        const string sql = @"
WITH
-- ─────────────────────────────────────────────────────────────────────
-- CTE 1 · Özel mesaj başlıkları (son mesaj)
-- ─────────────────────────────────────────────────────────────────────
private_threads AS (
    SELECT
        CASE
            WHEN m.""SenderId"" = @userId THEN m.""ReceiverId""
            ELSE m.""SenderId""
        END                                              AS contact_id,
        m.""Content""                                    AS last_content,
        m.""SentAt""                                     AS last_sent_at,
        m.""IsSilent""                                   AS is_silent,
        m.""IsEmergency""                                AS is_emergency,
        ROW_NUMBER() OVER (
            PARTITION BY
                LEAST(m.""SenderId"", m.""ReceiverId""),
                GREATEST(m.""SenderId"", m.""ReceiverId"")
            ORDER BY m.""SentAt"" DESC
        )                                                AS rn
    FROM ""Messages"" m
    WHERE m.""CourseId"" IS NULL
      AND (m.""SenderId"" = @userId OR m.""ReceiverId"" = @userId)
),
-- ─────────────────────────────────────────────────────────────────────
-- CTE 2 · Özel mesaj okunmamış sayısı
-- ─────────────────────────────────────────────────────────────────────
private_unread AS (
    SELECT m.""SenderId"" AS contact_id, COUNT(*) AS unread_count
    FROM ""Messages"" m
    WHERE m.""ReceiverId"" = @userId
      AND m.""IsRead""     = false
      AND m.""CourseId""   IS NULL
    GROUP BY m.""SenderId""
),
-- ─────────────────────────────────────────────────────────────────────
-- CTE 3 · Özel mesaj özeti
-- ─────────────────────────────────────────────────────────────────────
private_summary AS (
    SELECT
        'private'                                             AS chat_type,
        CONCAT(@userId::text, '_', pt.contact_id::text)      AS chat_id,
        u.""FullName""                                        AS display_name,
        u.""Id""                                              AS other_user_id,
        u.""Role""                                            AS other_user_role,
        NULL::uuid                                            AS course_id,
        pt.last_content                                       AS last_message,
        pt.last_sent_at,
        COALESCE(pu.unread_count, 0)                         AS unread_count,
        CASE
            WHEN u.""Role"" = 2
             AND EXTRACT(HOUR FROM NOW() AT TIME ZONE 'Europe/Istanbul') >= 17
            THEN true ELSE false
        END                                                   AS is_silent_mode,
        false                                                 AS is_locked,
        false                                                 AS is_user_representative,
        pt.is_emergency,
        false                                                 AS is_official
    FROM private_threads pt
    INNER JOIN ""Users"" u  ON u.""Id"" = pt.contact_id
    LEFT  JOIN private_unread pu ON pu.contact_id = pt.contact_id
    WHERE pt.rn = 1
),
-- ─────────────────────────────────────────────────────────────────────
-- CTE 4 · Ders kanallarının son mesajları
-- ─────────────────────────────────────────────────────────────────────
course_last_msg AS (
    SELECT
        m.""CourseId"",
        m.""Content""     AS last_content,
        m.""SentAt""      AS last_sent_at,
        m.""IsEmergency"" AS is_emergency,
        ROW_NUMBER() OVER (
            PARTITION BY m.""CourseId""
            ORDER BY m.""SentAt"" DESC
        ) AS rn
    FROM ""Messages"" m
    WHERE m.""CourseId"" IS NOT NULL
),
-- ─────────────────────────────────────────────────────────────────────
-- CTE 5 · Ders kanalları okunmamış sayısı
-- ─────────────────────────────────────────────────────────────────────
course_unread AS (
    SELECT m.""CourseId"", COUNT(*) AS unread_count
    FROM ""Messages"" m
    WHERE m.""SenderId"" <> @userId
      AND m.""IsRead""   = false
      AND m.""CourseId"" IN (
          SELECT cm.""CourseId""
          FROM ""CourseMemberships"" cm
          WHERE cm.""UserId"" = @userId
      )
    GROUP BY m.""CourseId""
),
-- ─────────────────────────────────────────────────────────────────────
-- CTE 6 · Ders kanalları özeti
--
-- DÜZELTME: chat_type ve is_locked artık c.""IsOfficial"" üzerinden belirleniyor.
--           Önceki yanlış: cm.""IsRepresentative"" baz alınıyordu.
--
-- EK: is_user_representative — giriş yapan kullanıcının temsilcilik durumu
-- ─────────────────────────────────────────────────────────────────────
course_summary AS (
    SELECT
        CASE WHEN c.""IsOfficial"" = true THEN 'official'::text
             ELSE 'study'::text END                          AS chat_type,
        c.""Id""::text                                       AS chat_id,
        c.""Name""                                           AS display_name,
        NULL::uuid                                           AS other_user_id,
        NULL::int                                            AS other_user_role,
        c.""Id""                                             AS course_id,
        COALESCE(clm.last_content, 'Henüz mesaj yok')       AS last_message,
        COALESCE(clm.last_sent_at, '1970-01-01'::timestamptz) AS last_sent_at,
        COALESCE(cu.unread_count, 0)                        AS unread_count,
        false                                                AS is_silent_mode,
        CASE WHEN c.""IsOfficial"" = true THEN true
             ELSE false END                                  AS is_locked,
        cm.""IsRepresentative""                              AS is_user_representative,
        COALESCE(clm.is_emergency, false)                   AS is_emergency,
        c.""IsOfficial""                                     AS is_official
    FROM ""CourseMemberships"" cm
    INNER JOIN ""Courses"" c    ON c.""Id"" = cm.""CourseId""
    LEFT  JOIN course_last_msg clm ON clm.""CourseId"" = c.""Id"" AND clm.rn = 1
    LEFT  JOIN course_unread cu    ON cu.""CourseId""  = c.""Id""
    WHERE cm.""UserId"" = @userId
)

-- ── Son birleşim: özel + ders kanalları, en yenisi üstte ─────────────
SELECT chat_type, chat_id, display_name, other_user_id, other_user_role,
       course_id, last_message, last_sent_at, unread_count,
       is_silent_mode, is_locked, is_user_representative, is_emergency, is_official
FROM private_summary

UNION ALL

SELECT chat_type, chat_id, display_name, other_user_id, other_user_role,
       course_id, last_message, last_sent_at, unread_count,
       is_silent_mode, is_locked, is_user_representative, is_emergency, is_official
FROM course_summary

ORDER BY last_sent_at DESC
";

        var result = new List<ChatSummaryDto>();
        var connection = _context.Database.GetDbConnection();

        try
        {
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            var param = cmd.CreateParameter();
            param.ParameterName = "userId";
            param.Value = userId;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var chatTypeStr = reader.GetString(reader.GetOrdinal("chat_type"));

                var dto = new ChatSummaryDto
                {
                    Type = chatTypeStr switch
                    {
                        "official" => ChatType.OfficialChannel,
                        "study" => ChatType.StudyRoom,
                        _ => ChatType.PrivateMessage
                    },
                    ChatId = reader.GetString(reader.GetOrdinal("chat_id")),
                    DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
                    LastMessage = SafeDecrypt(reader.GetString(reader.GetOrdinal("last_message"))),
                    LastMessageAt = reader.GetDateTime(reader.GetOrdinal("last_sent_at")),
                    UnreadCount = Convert.ToInt32(reader["unread_count"]),
                    IsSilentMode = reader.GetBoolean(reader.GetOrdinal("is_silent_mode")),
                    IsLocked = reader.GetBoolean(reader.GetOrdinal("is_locked")),
                    IsUserRepresentative = reader.GetBoolean(reader.GetOrdinal("is_user_representative")),
                    HasEmergency = reader.GetBoolean(reader.GetOrdinal("is_emergency")),
                    IsOfficial = reader.GetBoolean(reader.GetOrdinal("is_official")),
                };

                var otherUserIdOrdinal = reader.GetOrdinal("other_user_id");
                if (!reader.IsDBNull(otherUserIdOrdinal))
                    dto.OtherUserId = reader.GetGuid(otherUserIdOrdinal);

                var otherRoleOrdinal = reader.GetOrdinal("other_user_role");
                if (!reader.IsDBNull(otherRoleOrdinal))
                    dto.OtherUserRole = reader.GetInt32(otherRoleOrdinal);

                var courseIdOrdinal = reader.GetOrdinal("course_id");
                if (!reader.IsDBNull(courseIdOrdinal))
                    dto.CourseId = reader.GetGuid(courseIdOrdinal);

                dto.AvatarInitial = string.IsNullOrEmpty(dto.DisplayName)
                    ? "?"
                    : dto.DisplayName[0].ToString().ToUpper();

                dto.AvatarColor = dto.OtherUserRole switch
                {
                    2 => "#1B305E",
                    3 => "#7C3AED",
                    _ => "#059669"
                };

                result.Add(dto);
            }
        }
        finally
        {
            if (connection.State == System.Data.ConnectionState.Open)
                await connection.CloseAsync();
        }

        return result;
    }

    // ════════════════════════════════════════════════════════════════════
    // OKUNDU İŞARETLE
    // ════════════════════════════════════════════════════════════════════
    public async Task<int> MarkMessagesAsReadAsync(
        Guid userId, Guid? senderId, Guid? courseId)
    {
        IQueryable<Message> query = _context.Messages.Where(m => !m.IsRead);

        if (courseId.HasValue)
            query = query.Where(m => m.CourseId == courseId && m.SenderId != userId);
        else if (senderId.HasValue)
            query = query.Where(m => m.SenderId == senderId && m.ReceiverId == userId);

        return await query.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }

    // ════════════════════════════════════════════════════════════════════
    // ACİL MESAJ (eski endpoint — geriye uyumluluk)
    // ════════════════════════════════════════════════════════════════════
    public async Task<bool> SendEmergencyMessageAsync(
        Guid senderId, Guid courseId, string content)
    {
        var currentTerm = GetCurrentAcademicTerm();
        var rightRecord = (await _rightRepo
            .FindAsync(r => r.UserId == senderId && r.AcademicTerm == currentTerm))
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

    // ════════════════════════════════════════════════════════════════════
    // KALAN ACİL HAK
    // ════════════════════════════════════════════════════════════════════
    public async Task<int> GetRemainingRightsAsync(Guid userId)
    {
        var currentTerm = GetCurrentAcademicTerm();
        var rightRecord = (await _rightRepo
            .FindAsync(r => r.UserId == userId && r.AcademicTerm == currentTerm))
            .FirstOrDefault();
        return rightRecord?.RemainingRights ?? 3; // Kayıt yoksa dönem başı 3 hak
    }

    // ════════════════════════════════════════════════════════════════════
    // YARDIMCI METODLAR
    // ════════════════════════════════════════════════════════════════════

    private static string SafeDecrypt(string content)
    {
        try { return EncryptionHelper.Decrypt(content); }
        catch { return content; }
    }

    /// <summary>
    /// Türk üniversite takvimi:
    ///   Ocak          → önceki yılın Güz dönemi (örn. Ocak 2026 → "2025-Güz")
    ///   Şubat–Haziran → ilkbahar/Bahar dönemi   (örn. Mart 2026 → "2026-Bahar")
    ///   Temmuz–Aralık → Güz dönemi               (örn. Ekim 2026 → "2026-Güz")
    /// </summary>
    private static string GetCurrentAcademicTerm()
    {
        var now = DateTime.Now;
        if (now.Month == 1)
            return $"{now.Year - 1}-Güz";
        if (now.Month >= 2 && now.Month <= 6)
            return $"{now.Year}-Bahar";
        return $"{now.Year}-Güz";
    }
}
