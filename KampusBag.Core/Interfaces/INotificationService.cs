namespace KampusBag.Core.Interfaces;

/// <summary>
/// Push notification gönderim soyutlaması. Firebase, OneSignal vb. farklı sağlayıcılar bu interface'i implemente edebilir.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Tek bir kullanıcıya bildirim gönderir. Kullanıcı için kayıtlı device token bulamazsa false döner.
    /// </summary>
    Task<bool> SendAsync(
        Guid receiverId,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Birden fazla kullanıcıya tek seferde bildirim gönderir (multicast).
    /// Başarılı gönderim sayısını döner.
    /// </summary>
    Task<int> SendToGroupAsync(
        List<Guid> userIds,
        string title,
        string body,
        Dictionary<string, string>? data = null);
}