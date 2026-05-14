namespace KampusBag.Core.DTOs;

/// <summary>
/// Mobil uygulamadan FCM token kaydetmek için kullanılan DTO.
/// </summary>
public class DeviceTokenDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android"; // "android" veya "ios"
}