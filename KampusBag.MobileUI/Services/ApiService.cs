using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KampusBag.Core.DTOs;
using KampusBag.MobileUI.Models;

namespace KampusBag.MobileUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly EncryptionService _encryption = new();

    // ── Hub URL ───────────────────────────────────────────────────────
#if ANDROID
    public const string BaseUrl = "https://resistant-sacred-exes.ngrok-free.dev/api/";
#else
    // Laptop (Windows) için de ngrok kullanalım ki port karmaşası bitsin
    public const string BaseUrl = "https://resistant-sacred-exes.ngrok-free.dev/api/";
#endif
    // ── Oturum ────────────────────────────────────────────────────────
    public static class Session
    {
        public static Guid UserId { get; set; }
        public static string Email { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static int Role { get; set; }
        public static bool IsLoggedIn { get; set; }

        public static void Clear()
        {
            UserId = Guid.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            Role = 0;
            IsLoggedIn = false;
        }
    }

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            // Ngrok statik URL'nizi buraya tam adres olarak yazıyoruz
            BaseAddress = new Uri("https://resistant-sacred-exes.ngrok-free.dev/api/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Ngrok'un ücretsiz planındaki "browser warning" sayfasını atlamak için:
        _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "69420");
    }

    // ══════════════════════════════════════════════════════════════════
    // AUTH
    // ══════════════════════════════════════════════════════════════════

    public async Task<bool> RegisterAsync(UserRegisterDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("users/register", dto);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        try
        {
            var response = await _httpClient
                .PostAsync($"users/verify?email={Uri.EscapeDataString(email)}&code={code}", null);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex) { return $"Hata: {ex.Message}"; }
    }

    public async Task<(bool success, string message)> LoginAsync(
        string identifier, string password)
    {
        try
        {
            var dto = new UserLoginDto { Identifier = identifier, Password = password };
            var response = await _httpClient.PostAsJsonAsync("users/login", dto);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var parsed = JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
                if (parsed?.User != null)
                {
                    Session.UserId = parsed.User.Id;
                    Session.Email = parsed.User.Email;
                    Session.FullName = parsed.User.FullName;
                    Session.Role = parsed.User.Role;
                    Session.IsLoggedIn = true;
                    return (true, parsed.Message ?? "Giriş başarılı!");
                }
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            return (false, err?.Message ?? "Giriş başarısız.");
        }
        catch (Exception ex) { return (false, $"Bağlantı hatası: {ex.Message}"); }
    }

    public async Task<(bool success, string message)> ForgotPasswordAsync(string email)
    {
        try
        {
            var response = await _httpClient
                .PostAsJsonAsync("users/forgot-password", new { Email = email });
            var result = JsonSerializer.Deserialize<MessageResponse>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "İşlem tamamlandı.");
        }
        catch (Exception ex) { return (false, $"Bağlantı hatası: {ex.Message}"); }
    }

    public async Task<(bool success, string message)> ResetPasswordAsync(
        string email, string code, string newPassword)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "users/reset-password",
                new { Email = email, Code = code, NewPassword = newPassword });
            var result = JsonSerializer.Deserialize<MessageResponse>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            return (response.IsSuccessStatusCode, result?.Message ?? "İşlem tamamlandı.");
        }
        catch (Exception ex) { return (false, $"Bağlantı hatası: {ex.Message}"); }
    }

    // ══════════════════════════════════════════════════════════════════
    // KULLANICI ARAMA
    // ══════════════════════════════════════════════════════════════════

    public async Task<(bool success, List<UserSearchModel> users, string error)>
        SearchUsersAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"users/search?term={Uri.EscapeDataString(query)}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
                return (false, new(), err?.Message ?? "Arama başarısız.");
            }

            var parsed = JsonSerializer.Deserialize<List<UserApiModel>>(content, _jsonOptions);
            var users = (parsed ?? new()).Select(u => new UserSearchModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                RegistrationNumber = u.RegistrationNumber,
                Role = u.Role
            }).ToList();

            return (true, users, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, new(), $"Bağlantı hatası: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PROFİL
    // ══════════════════════════════════════════════════════════════════

    public async Task<(bool success, UserProfileModel? user, string error)>
        GetProfileAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"users/profile/{userId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
                return (false, null, err?.Message ?? "Profil alınamadı.");
            }

            var profile = JsonSerializer.Deserialize<UserProfileModel>(content, _jsonOptions);
            return (true, profile, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, null, $"Bağlantı hatası: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // SOHBET LİSTESİ & GEÇMİŞ
    // ══════════════════════════════════════════════════════════════════

    public async Task<ChatListResult> GetChatListAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"messages/chats/{userId}");
            if (!response.IsSuccessStatusCode)
                return ChatListResult.Fail("Sohbet listesi alınamadı.");

            var content = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<ChatListResponse>(content, _jsonOptions);

            return new ChatListResult
            {
                Success = true,
                OfficialChannels = DecryptList(parsed?.OfficialChannels),
                StudyRooms = DecryptList(parsed?.StudyRooms),
                PrivateMessages = DecryptList(parsed?.PrivateMessages),
            };
        }
        catch (Exception ex)
        {
            return ChatListResult.Fail($"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<(bool success, List<MessageModel> messages, string error)>
        GetChatHistoryAsync(Guid userId, Guid? otherUserId, Guid? courseId)
    {
        try
        {
            string query = otherUserId.HasValue
                ? $"messages/history?userId={userId}&otherUserId={otherUserId}"
                : $"messages/history?userId={userId}&courseId={courseId}";

            var response = await _httpClient.GetAsync(query);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
                return (false, new(), err?.Message ?? "Geçmiş alınamadı.");
            }

            var parsed = JsonSerializer.Deserialize<HistoryResponse>(content, _jsonOptions);
            var messages = (parsed?.Data ?? new())
                .Select(m => { m.Content = _encryption.Decrypt(m.Content); return m; })
                .OrderBy(m => m.SentAt)
                .ToList();

            return (true, messages, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, new(), $"Bağlantı hatası: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // MESAJ GÖNDER
    // ══════════════════════════════════════════════════════════════════

    public async Task<SendMessageResult> SendMessageAsync(
        Guid? receiverId,
        Guid? courseId,
        string content,
        bool isEmergency = false)
    {
        try
        {
            string encrypted = _encryption.Encrypt(content);
            var dto = new
            {
                SenderId = Session.UserId,
                ReceiverId = receiverId,
                CourseId = courseId,
                Content = encrypted,
                IsEmergency = isEmergency
            };

            var response = await _httpClient.PostAsJsonAsync("messages/send", dto);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<SendMessageResponse>(body, _jsonOptions);
                return new SendMessageResult
                {
                    Success = true,
                    Message = result?.Data,
                    StatusMsg = result?.Message ?? "Gönderildi."
                };
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var err = JsonSerializer.Deserialize<ErrorResponse>(body, _jsonOptions);
                return new SendMessageResult
                {
                    Success = false,
                    IsRightsDepleted = true,
                    StatusMsg = err?.Message ?? "Acil mesaj hakkınız doldu."
                };
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return new SendMessageResult
                {
                    Success = false,
                    StatusMsg = "Bu kanala mesaj gönderme yetkiniz yok."
                };

            var genericErr = JsonSerializer.Deserialize<ErrorResponse>(body, _jsonOptions);
            return new SendMessageResult
            {
                Success = false,
                StatusMsg = genericErr?.Message ?? "Mesaj gönderilemedi."
            };
        }
        catch (TaskCanceledException)
        {
            return new SendMessageResult
            {
                Success = false,
                StatusMsg = "Sunucu yanıt vermiyor. Bağlantınızı kontrol edin."
            };
        }
        catch (Exception ex)
        {
            return new SendMessageResult
            {
                Success = false,
                StatusMsg = $"Bağlantı hatası: {ex.Message}"
            };
        }
    }

    public async Task<(bool success, int remaining)> GetEmergencyRightsAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"messages/rights/{userId}");
            if (!response.IsSuccessStatusCode) return (false, 0);
            var parsed = JsonSerializer.Deserialize<RightsResponse>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            return (true, parsed?.Remaining ?? 0);
        }
        catch { return (false, 0); }
    }

    public async Task MarkAsReadAsync(Guid? senderId, Guid? courseId)
    {
        try
        {
            string query = courseId.HasValue
                ? $"messages/read?userId={Session.UserId}&courseId={courseId}"
                : $"messages/read?userId={Session.UserId}&senderId={senderId}";
            await _httpClient.PatchAsync(query, null);
        }
        catch { /* Kritik değil, sessizce geç */ }
    }

    // ══════════════════════════════════════════════════════════════════
    // DERS METODLARI
    // ══════════════════════════════════════════════════════════════════

    public async Task<CourseListResult> GetMyCoursesAsync(Guid userId)
    {
        try
        {
            // DÜZELTME: Önceki kod "courses/my/{userId}" çağırıyordu
            // ancak CoursesController'da bu endpoint eksikti. Artık mevcut.
            var response = await _httpClient.GetAsync($"courses/my/{userId}");
            if (!response.IsSuccessStatusCode)
                return CourseListResult.Fail("Dersler alınamadı.");

            var content = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<CourseListResponse>(content, _jsonOptions);

            var courses = (parsed?.Courses ?? new()).Select(c => new CourseModel
            {
                Id = c.Id,
                Name = c.Name,
                CourseCode = c.CourseCode,
                AcademicName = c.AcademicName,
                MemberCount = c.MemberCount,
                IsRepresentative = c.IsRepresentative,
                IsOfficial = c.IsOfficial       // YENİ: IsOfficial alanı eklendi
            }).ToList();

            return new CourseListResult { Success = true, Courses = courses };
        }
        catch (Exception ex)
        {
            return CourseListResult.Fail($"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> JoinCourseAsync(string courseCode)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "courses/join",
                new { CourseCode = courseCode.ToUpper(), UserId = Session.UserId });
            var content = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<MessageResponse>(content, _jsonOptions);
            return (response.IsSuccessStatusCode, parsed?.Message ?? "İşlem tamamlandı.");
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<(bool success, string message, Guid? courseId)>
        CreateCourseAsync(string name, string courseCode)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("courses/create", new
            {
                Name = name,
                CourseCode = courseCode.ToUpper(),
                AcademicId = Session.UserId
            });

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode
                            ? "Ders başarıyla oluşturuldu."
                            : "Sunucudan boş yanıt geldi.",
                        null);

            var parsed = JsonSerializer.Deserialize<CreateCourseResponse>(content, _jsonOptions);
            return (response.IsSuccessStatusCode,
                    parsed?.Message ?? "İşlem tamamlandı",
                    parsed?.CourseId);
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}", null);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // TEMSİLCİ ATAMA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dersin sahibi hoca, bir öğrenciyi temsilci olarak atar veya
    /// temsilciliğini geri alır.
    /// </summary>
    public async Task<(bool success, string message)> AssignRepresentativeAsync(
        Guid courseId, Guid studentId, bool revoke = false)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"courses/{courseId}/assign-representative",
                new
                {
                    AcademicId = Session.UserId,
                    StudentId = studentId,
                    Revoke = revoke
                });

            var content = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<MessageResponse>(content, _jsonOptions);

            return (response.IsSuccessStatusCode, parsed?.Message ?? "İşlem tamamlandı.");
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Bir dersin üye listesini getirir (temsilci atama ekranı için).
    /// </summary>
    public async Task<(bool success, List<CourseMemberModel> members, string error)>
        GetCourseMembersAsync(Guid courseId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"courses/{courseId}/members");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
                return (false, new(), err?.Message ?? "Üyeler alınamadı.");
            }

            var parsed = JsonSerializer.Deserialize<MembersResponse>(content, _jsonOptions);
            var members = (parsed?.Members ?? new()).Select(m => new CourseMemberModel
            {
                UserId = m.UserId,
                FullName = m.FullName,
                Email = m.Email,
                Role = m.Role,
                IsRepresentative = m.IsRepresentative
            }).ToList();

            return (true, members, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, new(), $"Bağlantı hatası: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // YARDIMCI METODLAR
    // ══════════════════════════════════════════════════════════════════

    private List<ChatSummaryModel> DecryptList(List<ChatSummaryApiModel>? apiModels)
    {
        if (apiModels == null) return new();

        return apiModels.Select(m => new ChatSummaryModel
        {
            ChatId = m.ChatId,
            ChatType = m.ChatType,
            DisplayName = m.DisplayName,
            LastMessage = _encryption.Decrypt(m.LastMessage ?? string.Empty),
            LastMessageAt = m.LastMessageAt,
            UnreadCount = m.UnreadCount,
            IsSilentMode = m.IsSilentMode,
            IsLocked = m.IsLocked,
            IsOfficial = m.IsOfficial,          // YENİ
            IsUserRepresentative = m.IsUserRepresentative, // YENİ
            HasEmergency = m.IsEmergency,
            OtherUserId = m.OtherUserId,
            OtherUserRole = m.OtherUserRole,
            CourseId = m.CourseId,
            AvatarInitial = string.IsNullOrEmpty(m.DisplayName)
                                   ? "?" : m.DisplayName[0].ToString().ToUpper(),
            AvatarColor = m.OtherUserRole switch
            {
                2 => "#1B305E",
                3 => "#7C3AED",
                _ => "#059669"
            }
        }).ToList();
    }

    // ── JSON seçenekleri ──────────────────────────────────────────────
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Private response modelleri ────────────────────────────────────
    private record LoginResponse(string? Message, UserInfo? User);
    private record UserInfo(Guid Id, string Email, string FullName,
                            string RegistrationNumber, int Role);
    private record ErrorResponse(string? Message);
    private record MessageResponse(string? Message);
    private record ChatListResponse(
        List<ChatSummaryApiModel>? OfficialChannels,
        List<ChatSummaryApiModel>? StudyRooms,
        List<ChatSummaryApiModel>? PrivateMessages);
    private record HistoryResponse(string? Message, int Count, List<MessageModel>? Data);
    private record SendMessageResponse(string? Message, MessageModel? Data);
    private record RightsResponse(string? Message, int Remaining, int MaxRights);
    private record CourseListResponse(List<CourseApiModel>? Courses);
    private record CreateCourseResponse(string? Message, Guid? CourseId);
    private record MembersResponse(List<MemberApiModel>? Members);

    private class ChatSummaryApiModel
    {
        public string ChatId { get; set; } = string.Empty;
        public string ChatType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsSilentMode { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsOfficial { get; set; }   // YENİ
        public bool IsUserRepresentative { get; set; }   // YENİ
        public Guid? OtherUserId { get; set; }
        public int? OtherUserRole { get; set; }
        public Guid? CourseId { get; set; }
    }

    private class UserApiModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int Role { get; set; }
    }

    private class CourseApiModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string AcademicName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public bool IsRepresentative { get; set; }
        public bool IsOfficial { get; set; }   // YENİ
    }

    private class MemberApiModel
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Role { get; set; }
        public bool IsRepresentative { get; set; }
    }

    // ── Public result modelleri ───────────────────────────────────────
    public class CourseListResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public List<CourseModel> Courses { get; set; } = new();
        public static CourseListResult Fail(string e)
            => new() { Success = false, Error = e };
    }
}

// ── Dışarı açık sonuç modelleri ───────────────────────────────────────────
public class SendMessageResult
{
    public bool Success { get; set; }
    public bool IsRightsDepleted { get; set; }
    public string StatusMsg { get; set; } = string.Empty;
    public MessageModel? Message { get; set; }
}

public class ChatListResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<ChatSummaryModel> OfficialChannels { get; set; } = new();
    public List<ChatSummaryModel> StudyRooms { get; set; } = new();
    public List<ChatSummaryModel> PrivateMessages { get; set; } = new();

    public static ChatListResult Fail(string error)
        => new() { Success = false, Error = error };
}