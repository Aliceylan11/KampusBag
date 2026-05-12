using KampusBag.Core.DTOs;
using KampusBag.Core.Interfaces;
using KampusBag.Core.Options;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using KampusBag.WebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KampusBag.WebAPI.Controllers;

public class Program
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly IHubContext<ChatHub> _hub;

    public MessagesController(
        IMessageService messageService,
        IUserService userService,
        IHubContext<ChatHub> hub)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

            // SignalR broadcast
            await BroadcastMessageAsync(result);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                message = result.IsEmergency
                    ? "🚨 Acil mesaj gönderildi!"
                    : result.IsSilent ? "Mesaj gönderildi. (Sessiz Mod)" : "Mesaj gönderildi.",
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

    private async Task BroadcastMessageAsync(MessageResponseDto result)
    {
        if (result.CourseId.HasValue)
            await _hub.Clients
                .Group($"course_{result.CourseId}")
                .SendAsync("ReceiveMessage", result);
        else if (result.ReceiverId.HasValue)
            await _hub.Clients
                .Group(ChatHub.GetPrivateRoom(result.SenderId, result.ReceiverId.Value))
                .SendAsync("ReceiveMessage", result);
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/messages/history
    // #6 DÜZELTME: page + pageSize parametreleri eklendi.
    // Varsayılan: page=1, pageSize=50
    // Mobil uygulama yukarı kaydırdıkça page+1 ile eski mesajları çeker.
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
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        try
        {
            var history = await _messageService.GetChatHistoryAsync(
                userId, otherUserId, courseId, page, pageSize);

            return Ok(new
            {
                message = "Mesaj geçmişi getirildi.",
                page,
                pageSize,
                count = history.Count(),
                hasMore = history.Count() == pageSize,
                data = history
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
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
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("rights/{userId:guid}")]
    public async Task<IActionResult> GetEmergencyRights(Guid userId)
    {
        try
        {
            int remaining = await _messageService.GetRemainingRightsAsync(userId);
            return Ok(new { remaining, maxRights = 3 });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

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

    [HttpPatch("read")]
    public async Task<IActionResult> MarkAsRead(
        [FromQuery] Guid userId,
        [FromQuery] Guid? senderId = null,
        [FromQuery] Guid? courseId = null)
    {
        if (userId == Guid.Empty) return BadRequest(new { message = "userId zorunludur." });
        if (senderId == null && courseId == null)
            return BadRequest(new { message = "senderId veya courseId gereklidir." });
        try
        {
            int updated = await _messageService.MarkMessagesAsReadAsync(userId, senderId, courseId);
            return Ok(new { message = $"{updated} mesaj okundu.", count = updated });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
