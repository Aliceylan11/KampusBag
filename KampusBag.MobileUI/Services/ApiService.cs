using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KampusBag.MobileUI.Models;

namespace KampusBag.MobileUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly EncryptionService _encryption = new();

#if ANDROID
    private const string BaseUrl = "http://10.0.2.2:5178/api/";
#else
    private const string BaseUrl = "http://localhost:5178/api/";
#endif

    public static class Session
    {
        public static Guid UserId { get; set; }
        public static string Email { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static int Role { get; set; }
        public static bool IsLoggedIn { get; set; }
        public static string Token { get; set; } = string.Empty;

        public static void Clear()
        {
            UserId = Guid.Empty; Email = string.Empty; FullName = string.Empty;
            Role = 0; IsLoggedIn = false; Token = string.Empty;
        }
    }

    public ApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(20) };
    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(Session.Token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Session.Token);
    }

    public async Task<(bool success, string message)> LoginAsync(string identifier, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "users/login", new { Identifier = identifier, Password = password });
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
                    Session.Token = parsed.Token ?? string.Empty;
                    SetAuthHeader();
                    return (true, parsed.Message ?? "Giris basarili!");
                }
            }

            var err = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            return (false, err?.Message ?? "Giris basarisiz.");
        }
        catch (Exception ex) { return (false, $"Baglanti hatasi: {ex.Message}"); }
    }

    public async Task<bool> RegisterAsync(object dto)
    {
        try { return (await _httpClient.PostAsJsonAsync("users/register", dto)).IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        try
        {
            var r = await _httpClient.PostAsync($"users/verify?email={Uri.EscapeDataString(email)}&code={code}", null);
            return await r.Content.ReadAsStringAsync();
        }
        catch (Exception ex) { return $"Hata: {ex.Message}"; }
    }

    public async Task<(bool, string)> ForgotPasswordAsync(string email)
    {
        try
        {
            var r = await _httpClient.PostAsJsonAsync("users/forgot-password", new { Email = email });
            var p = JsonSerializer.Deserialize<MessageResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return (r.IsSuccessStatusCode, p?.Message ?? "Tamam.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool, string)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        try
        {
            var r = await _httpClient.PostAsJsonAsync("users/reset-password",
                new { Email = email, Code = code, NewPassword = newPassword });
            var p = JsonSerializer.Deserialize<MessageResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return (r.IsSuccessStatusCode, p?.Message ?? "Tamam.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<ChatListResult> GetChatListAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"messages/chats/{userId}");
            if (!r.IsSuccessStatusCode) return ChatListResult.Fail("Liste alinamadi.");
            var p = JsonSerializer.Deserialize<ChatListResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return new ChatListResult
            {
                Success = true,
                OfficialChannels = DecryptList(p?.OfficialChannels),
                StudyRooms = DecryptList(p?.StudyRooms),
                PrivateMessages = DecryptList(p?.PrivateMessages)
            };
        }
        catch (Exception ex) { return ChatListResult.Fail(ex.Message); }
    }

    public async Task<(bool, List<MessageModel>, string)> GetChatHistoryAsync(
        Guid userId, Guid? otherUserId, Guid? courseId, int page = 1, int pageSize = 50)
    {
        try
        {
            SetAuthHeader();
            string q = courseId.HasValue
                ? $"messages/history?userId={userId}&courseId={courseId}&page={page}&pageSize={pageSize}"
                : $"messages/history?userId={userId}&otherUserId={otherUserId}&page={page}&pageSize={pageSize}";

            var r = await _httpClient.GetAsync(q);
            var c = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode)
            {
                var e = JsonSerializer.Deserialize<ErrorResponse>(c, _jsonOptions);
                return (false, new(), e?.Message ?? "Gecmis alinamadi.");
            }

            var p = JsonSerializer.Deserialize<HistoryResponse>(c, _jsonOptions);
            var msgs = (p?.Data ?? new())
                .Select(m => { m.Content = _encryption.Decrypt(m.Content); return m; })
                .OrderBy(m => m.SentAt)
                .ToList();

            return (true, msgs, string.Empty);
        }
        catch (Exception ex) { return (false, new(), ex.Message); }
    }

    public async Task<SendMessageResult> SendMessageAsync(
        Guid? receiverId, Guid? courseId, string content, bool isEmergency = false)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.PostAsJsonAsync("messages/send", new
            {
                SenderId = Session.UserId,
                ReceiverId = receiverId,
                CourseId = courseId,
                Content = _encryption.Encrypt(content),
                IsEmergency = isEmergency
            });
            var body = await r.Content.ReadAsStringAsync();
            if (r.IsSuccessStatusCode)
            {
                var res = JsonSerializer.Deserialize<SendMessageResponse>(body, _jsonOptions);
                return new SendMessageResult { Success = true, Message = res?.Data, StatusMsg = res?.Message ?? "Gonderildi." };
            }
            if (r.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var e = JsonSerializer.Deserialize<ErrorResponse>(body, _jsonOptions);
                return new SendMessageResult { Success = false, IsRightsDepleted = true, StatusMsg = e?.Message ?? "Hak doldu." };
            }
            var ge = JsonSerializer.Deserialize<ErrorResponse>(body, _jsonOptions);
            return new SendMessageResult { Success = false, StatusMsg = ge?.Message ?? "Gonderilemedi." };
        }
        catch (Exception ex) { return new SendMessageResult { Success = false, StatusMsg = ex.Message }; }
    }

    public async Task<(bool, int)> GetEmergencyRightsAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"messages/rights/{userId}");
            if (!r.IsSuccessStatusCode) return (false, 0);
            var p = JsonSerializer.Deserialize<RightsResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return (true, p?.Remaining ?? 0);
        }
        catch { return (false, 0); }
    }

    public async Task MarkAsReadAsync(Guid? senderId, Guid? courseId)
    {
        try
        {
            SetAuthHeader();
            string q = courseId.HasValue
                ? $"messages/read?userId={Session.UserId}&courseId={courseId}"
                : $"messages/read?userId={Session.UserId}&senderId={senderId}";
            await _httpClient.PatchAsync(q, null);
        }
        catch { }
    }

    public async Task<CourseListResult> GetMyCoursesAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"courses/my/{userId}");
            if (!r.IsSuccessStatusCode) return CourseListResult.Fail("Dersler alinamadi.");
            var p = JsonSerializer.Deserialize<CourseListResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            var courses = (p?.Courses ?? new()).Select(c => new CourseModel
            {
                Id = c.Id,
                Name = c.Name,
                CourseCode = c.CourseCode,
                AcademicName = c.AcademicName,
                MemberCount = c.MemberCount,
                IsRepresentative = c.IsRepresentative,
                IsOfficial = c.IsOfficial
            }).ToList();
            return new CourseListResult { Success = true, Courses = courses };
        }
        catch (Exception ex) { return CourseListResult.Fail(ex.Message); }
    }

    public async Task<(bool, string)> JoinCourseAsync(string code)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.PostAsJsonAsync("courses/join",
                new { CourseCode = code.ToUpper(), UserId = Session.UserId });
            var p = JsonSerializer.Deserialize<MessageResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return (r.IsSuccessStatusCode, p?.Message ?? "Tamam.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool, string, Guid?)> CreateCourseAsync(string name, string code)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.PostAsJsonAsync("courses/create",
                new { Name = name, CourseCode = code.ToUpper(), AcademicId = Session.UserId });
            var c = await r.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(c)) return (r.IsSuccessStatusCode, "Tamam.", null);
            var p = JsonSerializer.Deserialize<CreateCourseResponse>(c, _jsonOptions);
            return (r.IsSuccessStatusCode, p?.Message ?? "Tamam.", p?.CourseId);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool, string)> AssignRepresentativeAsync(Guid courseId, Guid studentId, bool revoke = false)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.PostAsJsonAsync(
                $"courses/{courseId}/assign-representative",
                new { AcademicId = Session.UserId, StudentId = studentId, Revoke = revoke });
            var p = JsonSerializer.Deserialize<MessageResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions);
            return (r.IsSuccessStatusCode, p?.Message ?? "Tamam.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // #1 FIX: MembersResponse class (positional record degil)
    public async Task<(bool, List<CourseMemberModel>, string)> GetCourseMembersAsync(Guid courseId)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"courses/{courseId}/members");
            var c = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode)
            {
                var e = JsonSerializer.Deserialize<ErrorResponse>(c, _jsonOptions);
                return (false, new(), e?.Message ?? "Uyeler alinamadi.");
            }
            var p = JsonSerializer.Deserialize<MembersResponse>(c, _jsonOptions);
            var members = (p?.Members ?? new()).Select(m => new CourseMemberModel
            {
                UserId = m.UserId,
                FullName = m.FullName,
                Email = m.Email,
                Role = m.Role,
                IsRepresentative = m.IsRepresentative
            }).ToList();
            return (true, members, string.Empty);
        }
        catch (Exception ex) { return (false, new(), ex.Message); }
    }

    public async Task<(bool, List<UserSearchModel>, string)> SearchUsersAsync(string query)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"users/search?term={Uri.EscapeDataString(query)}");
            var c = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode)
            {
                var e = JsonSerializer.Deserialize<ErrorResponse>(c, _jsonOptions);
                return (false, new(), e?.Message ?? "Arama basarisiz.");
            }
            var p = JsonSerializer.Deserialize<List<UserApiModel>>(c, _jsonOptions);
            var users = (p ?? new()).Select(u => new UserSearchModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                RegistrationNumber = u.RegistrationNumber,
                Role = u.Role
            }).ToList();
            return (true, users, string.Empty);
        }
        catch (Exception ex) { return (false, new(), ex.Message); }
    }

    public async Task<(bool success, UserProfileModel? user, string error)> GetProfileAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            var r = await _httpClient.GetAsync($"users/profile/{userId}");
            var c = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode)
            {
                var e = JsonSerializer.Deserialize<ErrorResponse>(c, _jsonOptions);
                return (false, null, e?.Message ?? "Profil alinamadi.");
            }
            return (true, JsonSerializer.Deserialize<UserProfileModel>(c, _jsonOptions), string.Empty);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    private List<ChatSummaryModel> DecryptList(List<ChatSummaryApiModel>? list)
    {
        if (list == null) return new();
        return list.Select(m => new ChatSummaryModel
        {
            ChatId = m.ChatId,
            ChatType = m.ChatType,
            DisplayName = m.DisplayName,
            LastMessage = _encryption.Decrypt(m.LastMessage ?? string.Empty),
            LastMessageAt = m.LastMessageAt,
            UnreadCount = m.UnreadCount,
            IsSilentMode = m.IsSilentMode,
            IsLocked = m.IsLocked,
            IsOfficial = m.IsOfficial,
            IsUserRepresentative = m.IsUserRepresentative,
            HasEmergency = m.IsEmergency,
            OtherUserId = m.OtherUserId,
            OtherUserRole = m.OtherUserRole,
            CourseId = m.CourseId,
            AvatarInitial = string.IsNullOrEmpty(m.DisplayName) ? "?" : m.DisplayName[0].ToString().ToUpper(),
            AvatarColor = m.OtherUserRole switch { 2 => "#1B305E", 3 => "#7C3AED", _ => "#059669" }
        }).ToList();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record LoginResponse(string? Message, string? Token, UserInfo? User);
    private record UserInfo(Guid Id, string Email, string FullName, string RegistrationNumber, int Role);
    private record ErrorResponse(string? Message);
    private record MessageResponse(string? Message);
    private record ChatListResponse(List<ChatSummaryApiModel>? OfficialChannels, List<ChatSummaryApiModel>? StudyRooms, List<ChatSummaryApiModel>? PrivateMessages);
    private record HistoryResponse(string? Message, int Count, List<MessageModel>? Data);
    private record SendMessageResponse(string? Message, MessageModel? Data);
    private record RightsResponse(string? Message, int Remaining, int MaxRights);
    private record CourseListResponse(List<CourseApiModel>? Courses);
    private record CreateCourseResponse(string? Message, Guid? CourseId);

    // #1 FIX: class tabanli - record ile karsilastirildiginda daha guvenli deserialization
    private class MembersResponse { public List<MemberApiModel> Members { get; set; } = new(); }
    private class ChatSummaryApiModel
    {
        public string ChatId { get; set; } = string.Empty; public string ChatType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsSilentMode { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsOfficial { get; set; }
        public bool IsUserRepresentative { get; set; }
        public Guid? OtherUserId { get; set; }
        public int? OtherUserRole { get; set; }
        public Guid? CourseId { get; set; }
    }
    private class UserApiModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; public string RegistrationNumber { get; set; } = string.Empty;
        public int Role { get; set; }
    }
    private class CourseApiModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty; public string AcademicName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public bool IsRepresentative { get; set; }
        public bool IsOfficial { get; set; }
    }
    private class MemberApiModel
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; public int Role { get; set; }
        public bool IsRepresentative { get; set; }
    }

    public class CourseListResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public List<CourseModel> Courses { get; set; } = new();
        public static CourseListResult Fail(string e) => new() { Success = false, Error = e };
    }
}

public class SendMessageResult
{
    public bool Success { get; set; }
    public bool IsRightsDepleted { get; set; }
    public string StatusMsg { get; set; } = string.Empty; public MessageModel? Message { get; set; }
}

public class ChatListResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<ChatSummaryModel> OfficialChannels { get; set; } = new();
    public List<ChatSummaryModel> StudyRooms { get; set; } = new();
    public List<ChatSummaryModel> PrivateMessages { get; set; } = new();
    public static ChatListResult Fail(string e) => new() { Success = false, Error = e };
}