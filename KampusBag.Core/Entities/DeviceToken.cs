namespace KampusBag.Core.Entities;

/// <summary>
/// Kullanıcının FCM cihaz token'ı. Push notification göndermek için kullanılır.
/// UserId üzerinde unique index var — bir kullanıcının yalnızca en son cihaz token'ı tutulur.
/// </summary>
public class DeviceToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>FCM tarafından üretilen cihaz token'ı (genelde 150+ karakter).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>"android" veya "ios"</summary>
    public string Platform { get; set; } = "android";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}