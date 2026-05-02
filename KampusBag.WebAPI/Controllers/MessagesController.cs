using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]   ← JWT entegre edilince bu satırı aktif et
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;

    public MessagesController(
        IMessageService messageService,
        IUserService userService)
    {
        _messageService = messageService;
        _userService = userService;
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/messages/send
    // Mesaj gönder — Acil hak, sessiz mod, şifreleme burada tetiklenir
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        if (dto.SenderId == Guid.Empty)
            return BadRequest(new { message = "SenderId zorunludur." });

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });

        if (dto.ReceiverId == null && dto.CourseId == null)
            return BadRequest(new
            {
                message = "ReceiverId veya CourseId alanlarından biri zorunludur."
            });

        try
        {
            // ── ROL BAZLI YAZMA YETKİSİ KONTROLÜ ────────────────────────
            // Resmi kanal mesajı (CourseId var ama ReceiverId yok)
            // Sadece Akademisyen (2) veya Temsilci (3) yazabilir
            if (dto.CourseId.HasValue && !dto.ReceiverId.HasValue)
            {
                var sender = await _userService.SearchUsersAsync(
                    dto.SenderId.ToString());

                // Gerçek rol bilgisi için repository üzerinden alınmalı;
                // burada ClaimsPrincipal üzerinden alınır (JWT aktif olunca)
                // Şimdilik Header'dan "X-User-Role" ile alıyoruz
                if (Request.Headers.TryGetValue("X-User-Role", out var roleHeader)
                    && int.TryParse(roleHeader, out int roleValue))
                {
                    bool isOfficialChannel = !roleHeader.Equals("2")
                                          && !roleHeader.Equals("3");
                    if (isOfficialChannel)
                    {
                        return Forbid();   // Öğrenci resmi kanala yazamaz
                    }
                }
            }

            var result = await _messageService.SendMessageAsync(dto);

            return Ok(new
            {
                message = result.IsEmergency
                    ? "🚨 Acil mesaj başarıyla gönderildi!"
                    : result.IsSilent
                        ? "Mesaj gönderildi. (Sessiz Mod — Hoca 17:00 sonrası bildirim almayacak)"
                        : "Mesaj başarıyla gönderildi.",
                data = result
            });
        }
        catch (Exception ex) when (ex.Message.Contains("hakkınız kalmadı"))
        {
            // Acil hak bitmiş
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/history
    // Mesaj geçmişi — birebir veya grup sohbeti
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid userId,
        [FromQuery] Guid? otherUserId = null,
        [FromQuery] Guid? courseId = null)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { message = "userId zorunludur." });

        if (otherUserId == null && courseId == null)
            return BadRequest(new
            {
                message = "otherUserId veya courseId parametrelerinden biri zorunludur."
            });

        try
        {
            var history = await _messageService.GetChatHistoryAsync(
                userId, otherUserId, courseId);

            return Ok(new
            {
                message = "Mesaj geçmişi başarıyla getirildi.",
                count = history.Count(),
                data = history
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/chats/{userId}
    // Kullanıcının tüm sohbet listesi (3 kategori birleşik)
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("chats/{userId:guid}")]
    public async Task<IActionResult> GetChatList(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { message = "Geçerli bir userId giriniz." });

        try
        {
            var chats = await _messageService.GetChatListAsync(userId);

            // Kategorilere ayır
            var official = chats.Where(c => c.Type == Core.DTOs.ChatType.OfficialChannel);
            var study = chats.Where(c => c.Type == Core.DTOs.ChatType.StudyRoom);
            var privates = chats.Where(c => c.Type == Core.DTOs.ChatType.PrivateMessage);

            return Ok(new
            {
                message = "Sohbet listesi başarıyla getirildi.",
                officialChannels = official,
                studyRooms = study,
                privateMessages = privates
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/rights/{userId}
    // Kalan acil mesaj hakkını öğren
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("rights/{userId:guid}")]
    public async Task<IActionResult> GetEmergencyRights(Guid userId)
    {
        try
        {
            int remaining = await _messageService.GetRemainingRightsAsync(userId);

            return Ok(new
            {
                message = "Acil hak bilgisi getirildi.",
                remaining = remaining,
                maxRights = 3
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/messages/emergency
    // Acil durum mesajı — eski endpoint (backward compat.)
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("emergency")]
    public async Task<IActionResult> SendEmergency(
        [FromQuery] Guid senderId,
        [FromQuery] Guid courseId,
        [FromBody] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });

        bool success = await _messageService
            .SendEmergencyMessageAsync(senderId, courseId, content);

        if (!success)
            return UnprocessableEntity(new
            {
                message = "Acil mesaj hakkınız kalmadı veya kayıt bulunamadı."
            });

        return Ok(new { message = "🚨 Acil mesaj başarıyla iletildi." });
    }
}
