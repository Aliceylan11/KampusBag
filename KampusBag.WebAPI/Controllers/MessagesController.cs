using KampusBag.Core.DTOs;
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
    private readonly IHubContext<ChatHub> _hub;

    public MessagesController(
        IMessageService messageService,
        IHubContext<ChatHub> hub)
    {
        _messageService = messageService;
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
            return BadRequest(new { message = "ReceiverId veya CourseId gereklidir." });

        try
        {
            var result = await _messageService.SendMessageAsync(dto);

            await BroadcastMessageAsync(result);

            return Ok(new
            {
                message = result.IsEmergency
                    ? "🚨 Acil mesaj gönderildi!"
                    : result.IsSilent
                        ? "Mesaj gönderildi. (Sessiz Mod)"
                        : "Mesaj gönderildi.",
                data = result
            });
        }
        catch (Exception ex) when (ex.Message.Contains("hakkınız kalmadı!"))
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/history   (#6 pagination eklendi)
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid userId,
        [FromQuery] Guid? otherUserId = null,
        [FromQuery] Guid? courseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { message = "userId zorunludur." });

        if (otherUserId == null && courseId == null)
            return BadRequest(new { message = "otherUserId veya courseId gereklidir." });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 50;

        try
        {
            var history = await _messageService.GetChatHistoryAsync(
                userId, otherUserId, courseId, page, pageSize);

            var list = history.ToList();

            return Ok(new
            {
                message = "Mesaj geçmişi getirildi.",
                page,
                pageSize,
                count = list.Count,
                hasMore = list.Count == pageSize,
                data = list
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
            return BadRequest(new { message = "Geçerli userId gereklidir." });

        try
        {
            var chats = await _messageService.GetChatListAsync(userId);

            return Ok(new
            {
                message = "Sohbet listesi getirildi.",
                officialChannels = chats.Where(c => c.Type == ChatType.OfficialChannel),
                studyRooms = chats.Where(c => c.Type == ChatType.StudyRoom),
                privateMessages = chats.Where(c => c.Type == ChatType.PrivateMessage)
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
            return Ok(new { remaining, maxRights = 3 });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/messages/emergency  (geriye uyumluluk)
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("emergency")]
    public async Task<IActionResult> SendEmergency(
        [FromQuery] Guid senderId,
        [FromQuery] Guid courseId,
        [FromBody] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(new { message = "İçerik boş olamaz." });

        bool success = await _messageService.SendEmergencyMessageAsync(senderId, courseId, content);

        return success
            ? Ok(new { message = "🚨 Acil mesaj iletildi." })
            : UnprocessableEntity(new { message = "Acil mesaj hakkınız kalmadı." });
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
            return Ok(new { message = $"{updated} mesaj okundu.", count = updated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── SignalR Broadcast ─────────────────────────────────────────────
    private async Task BroadcastMessageAsync(MessageResponseDto result)
    {
        if (result.CourseId.HasValue)
        {
            await _hub.Clients
                .Group($"course_{result.CourseId}")
                .SendAsync("ReceiveMessage", result);
        }
        else if (result.ReceiverId.HasValue)
        {
            await _hub.Clients
                .Group(ChatHub.GetPrivateRoom(result.SenderId, result.ReceiverId.Value))
                .SendAsync("ReceiveMessage", result);
        }
    }
}