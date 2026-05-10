using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using KampusBag.WebAPI.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly IHubContext<ChatHub> _hub;           // YENİ

    public MessagesController(
        IMessageService messageService,
        IUserService userService,
        IHubContext<ChatHub> hub)
    {
        _messageService = messageService;
        _userService = userService;
        _hub = hub;
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/messages/send
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        if (dto.SenderId == Guid.Empty)
            return BadRequest(new { message = "SenderId zorunludur." });

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });

        if (dto.ReceiverId == null && dto.CourseId == null)
            return BadRequest(new { message = "ReceiverId veya CourseId alanlarından biri zorunludur." });

        try
        {
            // Resmi kanal yazma yetkisi kontrolü
            if (dto.CourseId.HasValue && !dto.ReceiverId.HasValue)
            {
                if (Request.Headers.TryGetValue("X-User-Role", out var roleHeader)
                    && int.TryParse(roleHeader, out int roleValue))
                {
                    if (roleValue == 1) // Öğrenci → kontrol et (temsilci olabilir)
                    {
                        // Gerçek temsilci kontrolü JWT ile yapılacak; şimdilik geçiyor
                    }
                }
            }

            // ── 1. Mesajı DB'ye kaydet ────────────────────────────────
            var result = await _messageService.SendMessageAsync(dto);

            // ── 2. SignalR ile ilgili gruba broadcast et ───────────────
            await BroadcastMessageAsync(result);

            return Ok(new
            {
                message = result.IsEmergency
                    ? "🚨 Acil mesaj başarıyla gönderildi!"
                    : result.IsSilent
                        ? "Mesaj gönderildi. (Sessiz Mod)"
                        : "Mesaj başarıyla gönderildi.",
                data = result
            });
        }
        catch (Exception ex) when (ex.Message.Contains("hakkınız kalmadı"))
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // SignalR Broadcast
    // ════════════════════════════════════════════════════════════════════
    private async Task BroadcastMessageAsync(MessageResponseDto result)
    {
        if (result.CourseId.HasValue)
        {
            // Ders odası → tüm oda üyelerine gönder
            var group = $"course_{result.CourseId}";
            await _hub.Clients.Group(group)
                .SendAsync("ReceiveMessage", result);
        }
        else if (result.ReceiverId.HasValue)
        {
            // Birebir oda → gönderene de alıcıya da gönder
            // (Gönderen farklı cihazda açık olabilir)
            var room = ChatHub.GetPrivateRoom(result.SenderId, result.ReceiverId.Value);
            await _hub.Clients.Group(room)
                .SendAsync("ReceiveMessage", result);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/history
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
            return BadRequest(new { message = "otherUserId veya courseId gereklidir." });

        try
        {
            var history = await _messageService.GetChatHistoryAsync(userId, otherUserId, courseId);
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
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("chats/{userId:guid}")]
    public async Task<IActionResult> GetChatList(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { message = "Geçerli bir userId giriniz." });

        try
        {
            var chats = await _messageService.GetChatListAsync(userId);

            var official = chats.Where(c => c.Type == ChatType.OfficialChannel);
            var study = chats.Where(c => c.Type == ChatType.StudyRoom);
            var privates = chats.Where(c => c.Type == ChatType.PrivateMessage);

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
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("rights/{userId:guid}")]
    public async Task<IActionResult> GetEmergencyRights(Guid userId)
    {
        try
        {
            int remaining = await _messageService.GetRemainingRightsAsync(userId);
            return Ok(new { message = "Acil hak bilgisi getirildi.", remaining, maxRights = 3 });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/messages/emergency (geriye uyumluluk)
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("emergency")]
    public async Task<IActionResult> SendEmergency(
        [FromQuery] Guid senderId,
        [FromQuery] Guid courseId,
        [FromBody] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });

        bool success = await _messageService.SendEmergencyMessageAsync(senderId, courseId, content);

        if (!success)
            return UnprocessableEntity(new
            {
                message = "Acil mesaj hakkınız kalmadı veya kayıt bulunamadı."
            });

        return Ok(new { message = "🚨 Acil mesaj başarıyla iletildi." });
    }

    // ════════════════════════════════════════════════════════════════════
    // PATCH api/messages/read
    // ════════════════════════════════════════════════════════════════════
    [HttpPatch("read")]
    public async Task<IActionResult> MarkAsRead(
        [FromQuery] Guid userId,
        [FromQuery] Guid? senderId = null,
        [FromQuery] Guid? courseId = null)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { message = "userId zorunludur." });

        if (senderId == null && courseId == null)
            return BadRequest(new { message = "senderId veya courseId gereklidir." });

        try
        {
            int updated = await _messageService.MarkMessagesAsReadAsync(userId, senderId, courseId);
            return Ok(new { message = $"{updated} mesaj okundu olarak işaretlendi.", count = updated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
