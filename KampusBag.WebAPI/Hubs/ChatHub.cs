using Microsoft.AspNetCore.SignalR;

namespace KampusBag.WebAPI.Hubs;

/// <summary>
/// KampusBag gerçek zamanlı mesajlaşma hub'ı.
/// JWT entegre edilene kadar userId query string'den alınır.
///
/// Grup adlandırma:
///   Ders odası  → course_{courseId}
///   Birebir oda → private_{min(a,b)}_{max(a,b)}
///   Kişisel     → user_{userId}  (OnConnect'te otomatik)
/// </summary>
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnDisconnectedAsync(exception);
    }

    // ── Ders odası ────────────────────────────────────────────────────
    public async Task JoinCourseRoom(string courseId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"course_{courseId}");

    public async Task LeaveCourseRoom(string courseId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"course_{courseId}");

    // ── Birebir oda ───────────────────────────────────────────────────
    public async Task JoinPrivateRoom(string userA, string userB)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GetPrivateRoom(userA, userB));

    public async Task LeavePrivateRoom(string userA, string userB)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetPrivateRoom(userA, userB));

    // ── Yardımcı: yönden bağımsız sabit oda adı ─────────────────────
    public static string GetPrivateRoom(string a, string b)
    {
        var sorted = new[] { a, b }.OrderBy(x => x).ToArray();
        return $"private_{sorted[0]}_{sorted[1]}";
    }

    public static string GetPrivateRoom(Guid a, Guid b)
        => GetPrivateRoom(a.ToString(), b.ToString());
}
