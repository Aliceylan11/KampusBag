-- ==========================================================================
-- KAMPUSBAG - CHAT SUMMARY (SOHBET ÖZETİ) SORGUSU
-- Bu sorgu; Özel Mesajları, Resmi Kanalları ve Çalışma Odalarını tek seferde getirir.
-- ==========================================================================

WITH 
-- --------------------------------------------------------------------------
-- CTE 1: private_threads
-- Amaç: İki kişi arasındaki en son mesajı bulur.
-- --------------------------------------------------------------------------
private_threads AS (
    SELECT 
        -- Mesajı atan ben isem karşıdakini, karşıdaki ise beni 'contact_id' olarak belirle
        CASE WHEN m."SenderId" = @userId THEN m."ReceiverId" ELSE m."SenderId" END AS contact_id,
        m."Content" AS last_content,
        m."SentAt" AS last_sent_at,
        m."IsSilent" AS is_silent,
        m."IsEmergency" AS is_emergency,
        -- LEAST ve GREATEST: Mesajın yönü ne olursa olsun (A'dan B'ye veya B'den A'ye),
        -- ikisini aynı 'konuşma' (thread) olarak kabul et ve en sonuncuyu (rn=1) seç.
        ROW_NUMBER() OVER (
            PARTITION BY 
                LEAST(CAST(m."SenderId" AS text), CAST(m."ReceiverId" AS text)), 
                GREATEST(CAST(m."SenderId" AS text), CAST(m."ReceiverId" AS text))
            ORDER BY m."SentAt" DESC
        ) AS rn
    FROM "Messages" m
    WHERE m."CourseId" IS NULL -- Sadece birebir özel mesajları filtrele
      AND (m."SenderId" = @userId OR m."ReceiverId" = @userId)
),

-- --------------------------------------------------------------------------
-- CTE 2: private_unread
-- Amaç: Her özel sohbet için kaç tane okunmamış mesajım olduğunu hesaplar.
-- --------------------------------------------------------------------------
private_unread AS (
    SELECT m."SenderId" AS contact_id, COUNT(*) AS unread_count
    FROM "Messages" m
    WHERE m."ReceiverId" = @userId  -- Alıcı ben olmalıyım
      AND m."IsRead" = false        -- Mesaj okunmamış olmalı
      AND m."CourseId" IS NULL
    GROUP BY m."SenderId"
),

-- --------------------------------------------------------------------------
-- CTE 3: private_summary
-- Amaç: Özel mesaj bilgilerini, kullanıcı rolleri ve "Sessiz Mod" ile birleştirir.
-- --------------------------------------------------------------------------
private_summary AS (
    SELECT 
        CAST('private' AS text) AS chat_type, -- VS hatası için CAST kullanıldı
        -- Mobil uygulama için benzersiz bir ChatId oluştur (BenimID_OnunID)
        CONCAT(CAST(@userId AS text), '_', CAST(pt.contact_id AS text)) AS chat_id,
        u."FullName" AS display_name,
        u."Id" AS other_user_id,
        u."Role" AS other_user_role,
        CAST(NULL AS uuid) AS course_id, -- Diğer tabloyla birleşmesi için tip uyuşması şart
        pt.last_content AS last_message,
        pt.last_sent_at,
        COALESCE(pu.unread_count, 0) AS unread_count,
        -- Sessiz Mod: Eğer karşı taraf Akademisyen (Role=2) ise ve saat 17:00'den büyükse 'true' yap[cite: 1].
        CASE 
            WHEN u."Role" = 2 AND EXTRACT(HOUR FROM NOW() AT TIME ZONE 'Europe/Istanbul') >= 17 
            THEN true ELSE false 
        END AS is_silent_mode,
        false AS is_locked,
        pt.is_emergency
    FROM private_threads pt
    INNER JOIN "Users" u ON u."Id" = pt.contact_id
    LEFT JOIN private_unread pu ON pu.contact_id = pt.contact_id
    WHERE pt.rn = 1 -- Sadece en son mesajı getir
),

-- --------------------------------------------------------------------------
-- CTE 4: course_summary
-- Amaç: Üyesi olduğum dersleri (Resmi ve Çalışma Odası) listeler.
-- --------------------------------------------------------------------------
course_summary AS (
    SELECT 
        -- IsRepresentative 'false' ise Resmi Kanal, 'true' ise Çalışma Odasıdır[cite: 1].
        CASE WHEN cm."IsRepresentative" = false THEN CAST('official' AS text) ELSE CAST('study' AS text) END AS chat_type,
        CAST(c."Id" AS text) AS chat_id,
        c."Name" AS display_name,
        CAST(NULL AS uuid) AS other_user_id,
        CAST(NULL AS int) AS other_user_role,
        c."Id" AS course_id,
        'Henüz mesaj yok' AS last_message, 
        CAST('1970-01-01' AS timestamptz) AS last_sent_at,
        0 AS unread_count,
        false AS is_silent_mode,
        -- Resmi kanallar genelde sadece duyuru için kilitli (is_locked) olabilir[cite: 1].
        CASE WHEN cm."IsRepresentative" = false THEN true ELSE false END AS is_locked,
        false AS is_emergency
    FROM "CourseMemberships" cm
    INNER JOIN "Courses" c ON c."Id" = cm."CourseId"
    WHERE cm."UserId" = @userId
)

-- --------------------------------------------------------------------------
-- ANA SORGULARI BİRLEŞTİR VE TİTRİZLİKLE SIRALA
-- --------------------------------------------------------------------------
SELECT * FROM private_summary
UNION ALL
SELECT * FROM course_summary
ORDER BY last_sent_at DESC; -- En son mesajı olan sohbeti en üste al