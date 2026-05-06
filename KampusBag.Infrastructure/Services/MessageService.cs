using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Helpers;
using KampusBag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;


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

    public async Task<int> GetCountByUserIdAsync(Guid userId)
    {
        // Kullanıcının gönderdiği toplam mesaj sayısını dönüyoruz
        return await _context.Messages.CountAsync(m => m.SenderId == userId);
    }

    // ════════════════════════════════════════════════════════════════════
    // MESAJ GÖNDER
    // ════════════════════════════════════════════════════════════════════
    public async Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto)
    {
        // 🚩 1. İŞLEM (TRANSACTION) BAŞLAT
        // Bu sayede hata olursa hiçbir veri (hak düşüşü dahil) kaydedilmez.
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ── 1. Göndereni doğrula ─────────────────────────────────────────
            var sender = await _userRepo.GetByIdAsync(dto.SenderId)
                ?? throw new Exception("Gönderen kullanıcı bulunamadı.");

            // ── 2. ACİL HAK KONTROLÜ ─────────────────────────────────────────
            if (dto.IsEmergency && sender.Role != UserRole.Admin)
            {
                string currentTerm = GetCurrentAcademicTerm();

                var rightRecord = (await _rightRepo.FindAsync(r =>
                    r.UserId == dto.SenderId &&
                    r.AcademicTerm == currentTerm))
                    .FirstOrDefault();

                // Kayıt yoksa "Hoş Geldin" hakkını tanımla
                if (rightRecord == null)
                {
                    rightRecord = new EmergencyRight
                    {
                        UserId = dto.SenderId,
                        RemainingRights = 3,
                        AcademicTerm = currentTerm
                    };
                    await _rightRepo.AddAsync(rightRecord);
                    // Not: Henüz SaveChanges yapmıyoruz, her şey toplu olacak.
                }

                // Hak bittiyse hata fırlat (Bu hata catch bloğuna düşer ve Rollback yapar)
                if (rightRecord.RemainingRights <= 0)
                {
                    throw new Exception($"Acil durum mesajı hakkınız kalmadı. {currentTerm} dönemi sınırı 3 adettir.");
                }

                // Hakkı düş
                rightRecord.RemainingRights -= 1;
                await _rightRepo.UpdateAsync(rightRecord);
            }

            // ── 3. SESSİZ MOD KONTROLÜ ───────────────────────────────────────
            bool isSilent = false;
            if (dto.ReceiverId.HasValue)
            {
                var receiver = await _userRepo.GetByIdAsync(dto.ReceiverId.Value);
                if (receiver?.Role == UserRole.Academic && DateTime.Now.Hour >= 17)
                {
                    isSilent = true;
                }
            }

            // ── 4. İÇERİĞİ ŞİFRELE ──────────────────────────────────────────
            // Burada hata alırsan catch bloğu sayesinde hak düşüşü iptal edilir.
            string encryptedContent = EncryptionHelper.Encrypt(dto.Content);

            // ── 5. MESAJI OLUŞTUR ───────────────────────────────────────────
            var message = new Message
            {
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId,
                CourseId = dto.CourseId,
                Content = encryptedContent,
                IsEmergency = dto.IsEmergency,
                SentAt = DateTime.UtcNow,
                IsSilent = isSilent // IsSilent kolonunu buraya ekledik
            };

            await _messageRepo.AddAsync(message);

            // 🚩 6. VERİTABANINA TOPLU KAYDET VE ONAYLA
            // Eğer buraya kadar bir hata gelirse her şey iptal (Rollback) olur.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ── 7. DTO OLARAK DÖN ────────────────────────────────────────────
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
            // Bir hata oluşursa veritabanını eski haline döndür
            await transaction.RollbackAsync();
            // Hatayı yukarı (Controller'a) fırlat ki Swagger'da mesajı görebilesin
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
        // SQL dosyasındaki CTE sorgusunu burada string olarak tutuyoruz.
        // İleride bir .sql dosyasından da okunabilir.
        const string sql = @"
        WITH
        private_threads AS (
            SELECT
                CASE
                    WHEN m.""SenderId"" = @userId THEN m.""ReceiverId""
                    ELSE m.""SenderId""
                END                                            AS contact_id,
                m.""Content""                                  AS last_content,
                m.""SentAt""                                   AS last_sent_at,
                m.""IsSilent""                                 AS is_silent,
                m.""IsEmergency""                              AS is_emergency,
                ROW_NUMBER() OVER (
                    PARTITION BY
                        LEAST(m.""SenderId"", m.""ReceiverId""),
                        GREATEST(m.""SenderId"", m.""ReceiverId"")
                    ORDER BY m.""SentAt"" DESC
                )                                              AS rn
            FROM ""Messages"" m
            WHERE m.""CourseId"" IS NULL
              AND (m.""SenderId"" = @userId OR m.""ReceiverId"" = @userId)
        ),
        private_unread AS (
            SELECT m.""SenderId"" AS contact_id, COUNT(*) AS unread_count
            FROM ""Messages"" m
            WHERE m.""ReceiverId"" = @userId
              AND m.""IsRead"" = false
              AND m.""CourseId"" IS NULL
            GROUP BY m.""SenderId""
        ),
        private_summary AS (
            SELECT
                'private'                                           AS chat_type,
                CONCAT(@userId::text, '_', pt.contact_id::text)    AS chat_id,
                u.""FullName""                                      AS display_name,
                u.""Id""                                            AS other_user_id,
                u.""Role""                                          AS other_user_role,
                NULL::uuid                                          AS course_id,
                pt.last_content                                     AS last_message,
                pt.last_sent_at,
                COALESCE(pu.unread_count, 0)                       AS unread_count,
                CASE
                    WHEN u.""Role"" = 2
                     AND EXTRACT(HOUR FROM NOW() AT TIME ZONE 'Europe/Istanbul') >= 17
                    THEN true ELSE false
                END                                                 AS is_silent_mode,
                false                                               AS is_locked,
                pt.is_emergency
            FROM private_threads pt
            INNER JOIN ""Users"" u ON u.""Id"" = pt.contact_id
            LEFT  JOIN private_unread pu ON pu.contact_id = pt.contact_id
            WHERE pt.rn = 1
        ),
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
        course_unread AS (
            SELECT m.""CourseId"", COUNT(*) AS unread_count
            FROM ""Messages"" m
            WHERE m.""SenderId"" <> @userId
              AND m.""IsRead""   = false
              AND m.""CourseId"" IN (
                  SELECT cm.""CourseId"" FROM ""CourseMemberships"" cm
                  WHERE cm.""UserId"" = @userId
              )
            GROUP BY m.""CourseId""
        ),
        course_summary AS (
            SELECT
                CASE WHEN cm.""IsRepresentative"" = false THEN 'official' ELSE 'study' END AS chat_type,
                c.""Id""::text                                          AS chat_id,
                c.""Name""                                              AS display_name,
                NULL::uuid                                              AS other_user_id,
                NULL::int                                               AS other_user_role,
                c.""Id""                                                AS course_id,
                COALESCE(clm.last_content, 'Henüz mesaj yok')          AS last_message,
                COALESCE(clm.last_sent_at, '1970-01-01'::timestamptz)  AS last_sent_at,
                COALESCE(cu.unread_count, 0)                           AS unread_count,
                false                                                   AS is_silent_mode,
                CASE WHEN cm.""IsRepresentative"" = false THEN true ELSE false END AS is_locked,
                false                                                   AS is_emergency
            FROM ""CourseMemberships"" cm
            INNER JOIN ""Courses"" c   ON c.""Id"" = cm.""CourseId""
            LEFT  JOIN course_last_msg clm ON clm.""CourseId"" = c.""Id"" AND clm.rn = 1
            LEFT  JOIN course_unread cu    ON cu.""CourseId""  = c.""Id""
            WHERE cm.""UserId"" = @userId
        )
        SELECT chat_type, chat_id, display_name, other_user_id,
               other_user_role, course_id, last_message, last_sent_at,
               unread_count, is_silent_mode, is_locked, is_emergency
        FROM private_summary
        UNION ALL
        SELECT chat_type, chat_id, display_name, other_user_id,
               other_user_role, course_id, last_message, last_sent_at,
               unread_count, is_silent_mode, is_locked, is_emergency
        FROM course_summary
        ORDER BY last_sent_at DESC
    ";

        var result = new List<ChatSummaryDto>();

        // EF Core'un raw connection'ını kullanıyoruz
        var connection = _context.Database.GetDbConnection();

        try
        {
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            // Parametreyi güvenli şekilde ekle (SQL Injection önlemi)
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
                    HasEmergency = reader.GetBoolean(reader.GetOrdinal("is_emergency")),
                };

                // Nullable alanlar
                var otherUserIdOrdinal = reader.GetOrdinal("other_user_id");
                if (!reader.IsDBNull(otherUserIdOrdinal))
                    dto.OtherUserId = reader.GetGuid(otherUserIdOrdinal);

                var otherRoleOrdinal = reader.GetOrdinal("other_user_role");
                if (!reader.IsDBNull(otherRoleOrdinal))
                    dto.OtherUserRole = reader.GetInt32(otherRoleOrdinal);

                var courseIdOrdinal = reader.GetOrdinal("course_id");
                if (!reader.IsDBNull(courseIdOrdinal))
                    dto.CourseId = reader.GetGuid(courseIdOrdinal);

                // Avatar: DisplayName'in baş harfi
                dto.AvatarInitial = string.IsNullOrEmpty(dto.DisplayName)
                    ? "?"
                    : dto.DisplayName[0].ToString().ToUpper();

                // Renk: Rol'e göre
                dto.AvatarColor = dto.OtherUserRole switch
                {
                    2 => "#1B305E",   // Akademisyen → lacivert
                    3 => "#7C3AED",   // Temsilci → mor
                    _ => "#059669"    // Öğrenci → yeşil
                };

                result.Add(dto);
            }
        }
        finally
        {
            // Connection'ı her zaman kapat
            if (connection.State == System.Data.ConnectionState.Open)
                await connection.CloseAsync();
        }

        return result;
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

    public async Task<int> MarkMessagesAsReadAsync(
        Guid userId, Guid? senderId, Guid? courseId)
    {
        IQueryable<Message> query = _context.Messages
            .Where(m => m.IsRead == false);

        if (courseId.HasValue)
        {
            // Grup mesajları: bu derste, bu kullanıcının göndermediği mesajlar
            query = query
                .Where(m => m.CourseId == courseId
                         && m.SenderId != userId);
        }
        else if (senderId.HasValue)
        {
            // Özel mesajlar: karşıdaki kişinin bana gönderdiği mesajlar
            query = query
                .Where(m => m.SenderId == senderId
                         && m.ReceiverId == userId);
        }

        // Toplu güncelleme — EF Core 7+ ExecuteUpdateAsync kullanımı
        int updated = await query.ExecuteUpdateAsync(
            s => s.SetProperty(m => m.IsRead, true));

        return updated;
    }

    // ── Yardımcı: Şifre çözme — bozuk içeriği sessizce atla ─────────────
    private static string SafeDecrypt(string content)
    {
        try { return EncryptionHelper.Decrypt(content); }
        catch { return content; }   // Eski şifresiz kayıtlar için
    }
    private string GetCurrentAcademicTerm()
    {
        var now = DateTime.Now;
        // Şubat-Temmuz arası Bahar, diğer aylar Güz
        string termName = (now.Month >= 2 && now.Month <= 7) ? "Bahar" : "Güz";

        return $"{now.Year}-{termName}";
    }
}
